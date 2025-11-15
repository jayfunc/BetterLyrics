// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<TimeSpan>>
    {
        public IMediaSessionsService MediaSessionsService { get; private set; }
        private readonly ILiveStatesService _liveStatesService;

        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        public LyricsPageViewModel(IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService)
        {
            _liveStatesService = liveStatesService;
            MediaSessionsService = mediaSessionsService;

            LiveStates = _liveStatesService.LiveStates;

            Volume = SystemVolumeHelper.MasterVolume;
            SystemVolumeHelper.VolumeNotification += SystemVolumeHelper_VolumeNotification;
        }

        private void SystemVolumeHelper_VolumeNotification(object? sender, int e)
        {
            Volume = e;
        }

        [ObservableProperty]
        public partial LiveStates LiveStates { get; set; }

        [ObservableProperty]
        public partial double TimelinePositionSeconds { get; set; }

        [ObservableProperty]
        public partial int Volume { get; set; }

        [ObservableProperty]
        public partial double BottomCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial double BottomCommandFlyoutTriggerOpacity { get; set; }

        [ObservableProperty]
        public partial float TimelineSliderThumbOpacity { get; set; } = 0f;

        [ObservableProperty]
        public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

        [ObservableProperty]
        public partial double TimelineSliderThumbSeconds { get; set; } = 0;


        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private async Task PlaySongAsync()
        {
            await MediaSessionsService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await MediaSessionsService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await MediaSessionsService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await MediaSessionsService.NextAsync();
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = MediaSessionsService.CurrentLyricsData?.GetLyricsLine(value);
        }

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
