// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<TimeSpan>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ISettingsService _settingsService;
        private readonly ILiveStatesService _liveStatesService;

        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        public LyricsPageViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _liveStatesService = liveStatesService;

            LiveStates = _liveStatesService.LiveStates;

            IsImmersiveMode = _settingsService.AppSettings.GeneralSettings.IsImmersiveMode;

            OnIsImmersiveModeChanged(IsImmersiveMode);

            //Volume = SystemVolumeHelper.GetMasterVolume();
            //SystemVolumeHelper.VolumeChanged += SystemVolumeHelper_VolumeChanged;

            _mediaSessionsService = mediaSessionsService;
            _mediaSessionsService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _mediaSessionsService.TimelineChanged += PlaybackService_TimelineChanged;

            IsSongPlaying = _mediaSessionsService.IsPlaying;
        }

        private void PlaybackService_TimelineChanged(object? sender, Events.TimelineChangedEventArgs e)
        {
            SongDurationSeconds = (int)e.End.TotalSeconds;
        }

        //private void SystemVolumeHelper_VolumeChanged(int volume)
        //{
        //    Volume = volume;
        //}

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            IsSongPlaying = e.IsPlaying;
        }

        private void PlaybackService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
            SongDurationSeconds = SongInfo?.Duration ?? 0;
        }

        [ObservableProperty]
        public partial LiveStates LiveStates { get; set; }

        [ObservableProperty]
        public partial double TimelinePositionSeconds { get; set; }

        [ObservableProperty]
        public partial int SongDurationSeconds { get; set; }

        [ObservableProperty]
        public partial int Volume { get; set; }

        [ObservableProperty]
        public partial bool IsImmersiveMode { get; set; }

        [ObservableProperty]
        public partial double BottomCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial double BottomCommandFlyoutTriggerOpacity { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        [ObservableProperty]
        public partial bool IsSongPlaying { get; set; }

        [ObservableProperty]
        public partial float TimelineSliderThumbOpacity { get; set; } = 0f;

        [ObservableProperty]
        public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

        [ObservableProperty]
        public partial double TimelineSliderThumbSeconds { get; set; } = 0;

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsImmersiveMode))
                {
                    IsImmersiveMode = message.NewValue;
                }
            }
        }

        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private async Task PlaySongAsync()
        {
            await _mediaSessionsService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await _mediaSessionsService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await _mediaSessionsService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await _mediaSessionsService.NextAsync();
        }

        partial void OnIsImmersiveModeChanged(bool value)
        {
            if (value)
            {
                BottomCommandGridOpacity = 0f;
                BottomCommandFlyoutTriggerOpacity = 0f;
            }
            else
            {
                BottomCommandGridOpacity = 1f;
                BottomCommandFlyoutTriggerOpacity = 1f;
            }
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = _mediaSessionsService.CurrentLyricsData?.GetLyricsLine(value);
        }

        //partial void OnVolumeChanged(int value)
        //{
        //    SystemVolumeHelper.SetMasterVolume(value);
        //}

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.LyricsRendererViewModel.TotalTime))
                {
                    if (_timelineThrottle.CanTrigger())
                    {
                        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            TimelinePositionSeconds = message.NewValue.TotalSeconds;
                        });
                    }
                }
            }
        }
    }
}
