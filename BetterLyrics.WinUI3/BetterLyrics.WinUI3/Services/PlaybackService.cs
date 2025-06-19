using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Dispatching;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services
{
    public partial class PlaybackService : IPlaybackService
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        public event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        public SongInfo? SongInfo { get; private set; }
        public bool IsPlaying { get; private set; }
        public TimeSpan Position { get; private set; }

        private readonly ISettingsService _settingsService;
        private readonly ILrcLibService _lrcLibService;

        public PlaybackService(ISettingsService settingsService, ILrcLibService lrcLibService)
        {
            _settingsService = settingsService;
            _lrcLibService = lrcLibService;
            InitMediaManager().ConfigureAwait(true);
        }

        private async Task RefreshSongInfoAsync()
        {
            var mediaProps = await _currentSession?.TryGetMediaPropertiesAsync();
            SongInfo = new SongInfo
            {
                Title = mediaProps?.Title ?? string.Empty,
                Artist = mediaProps?.Artist ?? string.Empty,
                Album = mediaProps?.AlbumTitle ?? string.Empty,
                Duration = (int?)_currentSession?.GetTimelineProperties().EndTime.TotalSeconds,
                SourceAppUserModelId = _currentSession?.SourceAppUserModelId,
                LyricsStatus = LyricsStatus.Loading,
            };

            string? lyricsRaw = null;
            byte[]? albumArt = null;

            if (mediaProps?.Thumbnail is IRandomAccessStreamReference streamReference)
            {
                // Local search for lyrics only
                SongInfo.AlbumArt = await ImageHelper.ToByteArrayAsync(streamReference);
                (lyricsRaw, _) = await Search(SongInfo.Title, SongInfo.Artist);
            }
            else
            {
                // Local search for lyrics and album art
                (lyricsRaw, albumArt) = await Search(
                    SongInfo.Title,
                    SongInfo.Artist,
                    targetProps: LocalSearchTargetProps.LyricsAndAlbumArt
                );
                SongInfo.AlbumArt = albumArt;
            }
            SongInfo.ParseLyrics(lyricsRaw);

            // If no lyrics found locally, search online
            if (_settingsService.IsOnlineLyricsEnabled && SongInfo.LyricsLines == null)
            {
                SongInfo.ParseLyrics(
                    await _lrcLibService.SearchLyricsAsync(
                        SongInfo.Title,
                        SongInfo.Artist,
                        SongInfo.Album,
                        SongInfo.Duration ?? 0
                    )
                );
            }

            if (SongInfo.LyricsLines == null || SongInfo.LyricsLines.Count == 0)
            {
                SongInfo.LyricsStatus = LyricsStatus.NotFound;
            }
            else
            {
                SongInfo.LyricsStatus = LyricsStatus.Found;
            }
        }

        private async Task<(string?, byte[]?)> Search(
            string title,
            string artist,
            LocalSearchTargetProps targetProps = LocalSearchTargetProps.LyricsOnly
        )
        {
            string? lyricsRaw = null;
            byte[]? albumArt = null;

            foreach (var path in _settingsService.MusicLibraries)
            {
                if (Directory.Exists(path))
                {
                    foreach (
                        var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    )
                    {
                        if (file.Contains(title) && file.Contains(artist))
                        {
                            Track track = new(file);
                            albumArt ??= track.EmbeddedPictures.FirstOrDefault()?.PictureData;

                            if (file.EndsWith(".lrc"))
                            {
                                lyricsRaw ??= await File.ReadAllTextAsync(
                                    file,
                                    FileHelper.GetEncoding(file)
                                );
                            }
                            else if (file.EndsWith("qm.qrc"))
                            {
                                lyricsRaw ??= Lyricify.Lyrics.Decrypter.Qrc.Decrypter.DecryptLyrics(
                                    await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8)
                                );
                            }
                            else if (track.Lyrics.SynchronizedLyrics.Count > 0)
                            {
                                lyricsRaw ??= track.Lyrics.FormatSynchToLRC();
                            }

                            if (
                                lyricsRaw != null
                                && (
                                    (
                                        targetProps == LocalSearchTargetProps.LyricsAndAlbumArt
                                        && albumArt != null
                                    )
                                    || targetProps == LocalSearchTargetProps.LyricsOnly
                                )
                            )
                            {
                                return (lyricsRaw, albumArt);
                            }
                        }
                    }
                }
            }

            return (lyricsRaw, albumArt);
        }

        private async Task InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
        }

        public void ReSendingMessages()
        {
            // Re-send messages to update UI
            CurrentSession_MediaPropertiesChanged(_currentSession, null);
            CurrentSession_PlaybackInfoChanged(_currentSession, null);
            CurrentSession_TimelinePropertiesChanged(_currentSession, null);
        }

        /// <summary>
        /// Note: Non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_PlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            PlaybackInfoChangedEventArgs? args
        )
        {
            if (sender == null)
            {
                IsPlaying = false;
            }
            else
            {
                var playbackState = sender.GetPlaybackInfo().PlaybackStatus;
                // _logger.LogDebug(playbackState.ToString());

                switch (playbackState)
                {
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                        IsPlaying = false;
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                        IsPlaying = true;
                        break;
                    default:
                        break;
                }
            }
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                () =>
                {
                    IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(IsPlaying));
                }
            );
        }

        private void SessionManager_CurrentSessionChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            CurrentSessionChangedEventArgs? args
        )
        {
            // _logger.LogDebug("SessionManager_CurrentSessionChanged");
            // Unregister events associated with the previous session
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -=
                    CurrentSession_TimelinePropertiesChanged;
            }

            // Record and register events for current session
            _currentSession = sender.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged +=
                    CurrentSession_TimelinePropertiesChanged;
            }

            ReSendingMessages();
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            MediaPropertiesChangedEventArgs? args
        )
        {
            // _logger.LogDebug("CurrentSession_MediaPropertiesChanged");
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                async () =>
                {
                    if (sender == null)
                        SongInfo = null;
                    else
                    {
                        await RefreshSongInfoAsync();
                    }
                    SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(SongInfo));
                }
            );
        }

        private void CurrentSession_TimelinePropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            TimelinePropertiesChangedEventArgs? args
        )
        {
            if (sender == null)
            {
                Position = TimeSpan.Zero;
            }
            else
            {
                Position = sender.GetTimelineProperties().Position;
            }
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                () =>
                {
                    PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
                }
            );
            // _logger.LogDebug(_currentTime);
        }
    }
}
