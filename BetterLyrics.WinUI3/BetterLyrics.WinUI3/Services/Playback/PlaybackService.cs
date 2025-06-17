using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Windows.Media.Control;

namespace BetterLyrics.WinUI3.Services.Playback
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

        private readonly IDatabaseService _databaseService;

        public PlaybackService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            InitMediaManager().ConfigureAwait(true);
        }

        private async Task<SongInfo> GetSongInfoAsync()
        {
            var songInfo = await _databaseService.FindSongInfoAsync(
                await _currentSession?.TryGetMediaPropertiesAsync()
            );
            songInfo.SourceAppUserModelId = _currentSession?.SourceAppUserModelId;
            return songInfo;
        }

        private async Task InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
        }

        private void ReSendingMessages()
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
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(IsPlaying));
            });
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
            App.DispatcherQueueTimer!.Debounce(
                async () =>
                {
                    // _logger.LogDebug("CurrentSession_MediaPropertiesChanged");
                    if (sender == null)
                        SongInfo = null;
                    else
                    {
                        try
                        {
                            SongInfo = await GetSongInfoAsync();
                        }
                        catch (Exception) { }
                    }
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(SongInfo));
                    });
                },
                TimeSpan.FromMilliseconds(AnimationHelper.DebounceDefaultDuration)
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
            _dispatcherQueue.TryEnqueue(() =>
            {
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
            });
            // _logger.LogDebug(_currentTime);
        }
    }
}
