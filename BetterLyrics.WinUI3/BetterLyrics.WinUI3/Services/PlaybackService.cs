using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.WinUI;
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

        private readonly IMusicSearchService _musicSearchService;

        public PlaybackService(
            ISettingsService settingsService,
            IMusicSearchService musicSearchService
        )
        {
            _musicSearchService = musicSearchService;
            InitMediaManager().ConfigureAwait(true);
        }

        private async Task SendSongInfoAsync(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            SongInfo = new SongInfo
            {
                Title = mediaProps?.Title ?? string.Empty,
                Artist = mediaProps?.Artist ?? string.Empty,
                Album = mediaProps?.AlbumTitle ?? string.Empty,
                DurationMs = _currentSession?.GetTimelineProperties().EndTime.TotalMilliseconds,
                SourceAppUserModelId = _currentSession?.SourceAppUserModelId,
            };

            if (mediaProps?.Thumbnail is IRandomAccessStreamReference streamReference)
            {
                SongInfo.AlbumArt = await ImageHelper.ToByteArrayAsync(streamReference);
            }
            else
            {
                SongInfo.AlbumArt = _musicSearchService.SearchAlbumArtAsync(
                    SongInfo.Title,
                    SongInfo.Artist
                );
            }

            SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(SongInfo));
        }

        private async Task InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
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

                CurrentSession_MediaPropertiesChanged(_currentSession, null);
                CurrentSession_PlaybackInfoChanged(_currentSession, null);
                CurrentSession_TimelinePropertiesChanged(_currentSession, null);
            }
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CurrentSession_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            MediaPropertiesChangedEventArgs? args
        )
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;
            if (sender != null)
            {
                mediaProps = await sender.TryGetMediaPropertiesAsync();
            }
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                async () =>
                {
                    if (sender == null)
                    {
                        SongInfo = null;
                        SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(SongInfo));
                    }
                    else
                    {
                        await SendSongInfoAsync(mediaProps);
                    }
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
