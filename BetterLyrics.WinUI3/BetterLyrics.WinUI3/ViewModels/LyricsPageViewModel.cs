// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using Lyricify.Lyrics.Providers.Web.Netease;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>,
        IRecipient<PropertyChangedMessage<LyricsDisplayType>>
    {
        public IMediaSessionsService MediaSessionsService { get; private set; }
        private readonly ILiveStatesService _liveStatesService;

        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        [ObservableProperty] public partial double AlbumArtWithSongInfoStackPanelHeight { get; set; } = 0;

        [ObservableProperty] public partial double LastAlbumArtOpacity { get; set; } = 1;
        [ObservableProperty] public partial double AlbumArtOpacity { get; set; } = 1;
        [ObservableProperty] public partial BitmapImage? LastAlbumArtBitmapImage { get; set; }
        [ObservableProperty] public partial BitmapImage? AlbumArtBitmapImage { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsX { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsY { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsWidth { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsOpacity { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Matrix4x4 Lyrics3DMatrix { get; set; } = Matrix4x4.Identity;

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

        [ObservableProperty]
        public partial double SongInfoOpacity { get; set; } = 1;

        public LyricsPageViewModel(IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService)
        {
            _liveStatesService = liveStatesService;
            MediaSessionsService = mediaSessionsService;

            LiveStates = _liveStatesService.LiveStates;

            Volume = SystemVolumeHook.MasterVolume;
            SystemVolumeHook.VolumeNotification += SystemVolumeHelper_VolumeNotification;
        }

        private void SystemVolumeHelper_VolumeNotification(object? sender, int e)
        {
            Volume = e;
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = MediaSessionsService.CurrentLyricsData?.GetLyricsLine(value);
        }

        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
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

        public async void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapImage))
                {
                    LastAlbumArtBitmapImage = AlbumArtBitmapImage;
                    LastAlbumArtOpacity = 1;
                    await Task.Delay(Constants.Time.AnimationDuration);

                    AlbumArtOpacity = 0;
                    await Task.Delay(Constants.Time.AnimationDuration);
                    AlbumArtBitmapImage = message.NewValue;

                    LastAlbumArtOpacity = 0;
                    AlbumArtOpacity = 1;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsLayoutOrientation))
                {
                    //OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsDisplayType))
                {
                    //OnLayoutChanged();
                }
            }
        }

    }
}
