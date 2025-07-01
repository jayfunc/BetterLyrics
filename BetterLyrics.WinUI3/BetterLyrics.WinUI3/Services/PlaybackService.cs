// 2025/6/23 by Zhe Fang

using System;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services
{
    public partial class PlaybackService : IPlaybackService
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private readonly IMusicSearchService _musicSearchService;

        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;

        public PlaybackService(ISettingsService settingsService, IMusicSearchService musicSearchService)
        {
            _musicSearchService = musicSearchService;
            InitMediaManager().ConfigureAwait(true);
        }

        public event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;

        public event EventHandler<PositionChangedEventArgs>? PositionChanged;

        public event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;

        public bool IsPlaying { get; private set; }

        public TimeSpan Position { get; private set; }

        public SongInfo? SongInfo { get; private set; }

        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession? sender, MediaPropertiesChangedEventArgs? args)
        {
            App.DispatcherQueueTimer!.Debounce(
                async () =>
                {
                    GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;
                    if (sender == null)
                    {
                        SongInfo = null;
                    }
                    else
                    {
                        try
                        {
                            mediaProps = await sender.TryGetMediaPropertiesAsync();
                        }
                        catch (Exception) { }

                        if (mediaProps == null)
                        {
                            SongInfo = null;
                        }
                        else
                        {
                            SongInfo = new SongInfo
                            {
                                Title = mediaProps.Title,
                                Artist = mediaProps.Artist,
                                Album = mediaProps?.AlbumTitle ?? string.Empty,
                                DurationMs = _currentSession
                                    ?.GetTimelineProperties()
                                    .EndTime.TotalMilliseconds,
                                SourceAppUserModelId = _currentSession?.SourceAppUserModelId,
                            };

                            if (mediaProps?.Thumbnail is IRandomAccessStreamReference streamReference)
                            {
                                SongInfo.AlbumArt = await ImageHelper.ToByteArrayAsync(
                                    streamReference
                                );
                            }
                            else
                            {
                                SongInfo.AlbumArt = _musicSearchService.SearchAlbumArtAsync(
                                    SongInfo.Title,
                                    SongInfo.Artist
                                );

                                if (SongInfo.AlbumArt == null)
                                {
                                    SongInfo.AlbumArt =
                                        await ImageHelper.CreateTextPlaceholderBytesAsync(
                                            $"{SongInfo.Artist} - {SongInfo.Title}",
                                            400,
                                            400
                                        );
                                }
                            }
                        }
                    }
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                        () =>
                        {
                            SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(SongInfo));
                        }
                    );
                },
                TimeSpan.FromMilliseconds(1000)
            );
        }

        private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession? sender, PlaybackInfoChangedEventArgs? args)
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
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                () =>
                {
                    IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(IsPlaying));
                }
            );
        }

        private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession? sender, TimelinePropertiesChangedEventArgs? args)
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
        }

        private async Task InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
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

            CurrentSession_MediaPropertiesChanged(_currentSession, null);
            CurrentSession_PlaybackInfoChanged(_currentSession, null);
            CurrentSession_TimelinePropertiesChanged(_currentSession, null);
        }
    }
}
