using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using DevWinUI;
using Microsoft.UI.Dispatching;
using Windows.Media.Control;
using Windows.System;

namespace BetterLyrics.WinUI3.Services.Playback
{
    public partial class PlaybackService : IPlaybackService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private readonly IDatabaseService _databaseService;

        public PlaybackService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            InitMediaManager();
        }

        private async void InitMediaManager()
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
                WeakReferenceMessenger.Default.Send(new PlayingStatusChangedMessage(false));
                return;
            }

            var playbackState = sender.GetPlaybackInfo().PlaybackStatus;
            // _logger.LogDebug(playbackState.ToString());

            switch (playbackState)
            {
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened:
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing:
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                    WeakReferenceMessenger.Default.Send(new PlayingStatusChangedMessage(false));
                    return;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                    WeakReferenceMessenger.Default.Send(new PlayingStatusChangedMessage(true));
                    return;
                default:
                    return;
            }
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
                        WeakReferenceMessenger.Default.Send(new SongInfoChangedMessage(null));
                    else
                    {
                        try
                        {
                            var songInfo = await _databaseService.FindSongInfoAsync(
                                await sender.TryGetMediaPropertiesAsync()
                            );
                            songInfo.SourceAppUserModelId = sender.SourceAppUserModelId;
                            WeakReferenceMessenger.Default.Send(
                                new SongInfoChangedMessage(songInfo)
                            );
                        }
                        catch (Exception) { }
                    }
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
                WeakReferenceMessenger.Default.Send(
                    new PlayingPositionChangedMessage(TimeSpan.Zero)
                );
            }
            else
                WeakReferenceMessenger.Default.Send(
                    new PlayingPositionChangedMessage(sender.GetTimelineProperties().Position)
                );

            // _logger.LogDebug(_currentTime);
        }
    }
}
