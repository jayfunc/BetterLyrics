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
        IRecipient<PropertyChangedMessage<TimeSpan>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<SongInfo?>>,
        IRecipient<PropertyChangedMessage<BitmapImage?>>
    {
        public IMediaSessionsService MediaSessionsService { get; private set; }
        private readonly ILiveStatesService _liveStatesService;

        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        [ObservableProperty]
        public partial Vector3 AlbumArtTranslation { get; set; } = new();

        [ObservableProperty]
        public partial double LastAlbumArtOpacity { get; set; } = 1;

        [ObservableProperty]
        public partial double AlbumArtOpacity { get; set; } = 1;

        [ObservableProperty]
        public partial BitmapImage? LastAlbumArtBitmapImage { get; set; }

        [ObservableProperty]
        public partial BitmapImage? AlbumArtBitmapImage { get; set; }

        public LyricsRendererViewModel.LyricsRendererViewModel LyricsRendererViewModel { get; private set; }

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
        public partial double RootWidth { get; set; } = 0;

        [ObservableProperty]
        public partial double RootHeight { get; set; } = 0;

        public TextBlock? TitleTextBlock { get; set; }
        public TextBlock? ArtistsTextBlock { get; set; }
        public TextBlock? AlbumTextBlock { get; set; }

        [ObservableProperty]
        public partial double SongInfoOpacity { get; set; } = 1;

        public LyricsPageViewModel(IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService, LyricsRendererViewModel.LyricsRendererViewModel lyricsRendererViewModel)
        {
            _liveStatesService = liveStatesService;
            MediaSessionsService = mediaSessionsService;
            LyricsRendererViewModel = lyricsRendererViewModel;

            LiveStates = _liveStatesService.LiveStates;

            Volume = SystemVolumeHook.MasterVolume;
            SystemVolumeHook.VolumeNotification += SystemVolumeHelper_VolumeNotification;
        }

        private void SystemVolumeHelper_VolumeNotification(object? sender, int e)
        {
            Volume = e;
        }

        private void RenderTextBlock(TextBlock? sender, string? text, int fontSize)
        {
            if (sender == null || string.IsNullOrEmpty(text)) return;

            var lyricsStyleSettings = LiveStates.LyricsWindowStatus.LyricsStyleSettings;
            sender.Inlines.Clear();
            foreach (char c in text)
            {
                var fontFamilyName = LanguageHelper.IsCJK(c) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;
                var run = new Run
                {
                    Text = c.ToString(),
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fontFamilyName),
                    FontSize = fontSize,
                };

                sender.Inlines.Add(run);
            }
        }

        private int GetTitleFontSize()
        {
            var albumArtLayoutSettings = LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings;
            if (albumArtLayoutSettings.IsAutoSongInfoFontSize)
            {
                return (int)Math.Clamp(Math.Min(RootHeight, RootWidth) / 20, 8, 72);
            }
            else
            {
                return albumArtLayoutSettings.SongInfoFontSize;
            }
        }

        private int GetArtistsAlbumFontSize()
        {
            return (int)(GetTitleFontSize() * 0.8);
        }

        private void RenderSongInfo()
        {
            RenderTextBlock(TitleTextBlock, MediaSessionsService.CurrentSongInfo?.Title, GetTitleFontSize());
            RenderTextBlock(ArtistsTextBlock, MediaSessionsService.CurrentSongInfo?.DisplayArtists, GetArtistsAlbumFontSize());
            RenderTextBlock(AlbumTextBlock, MediaSessionsService.CurrentSongInfo?.Album, GetArtistsAlbumFontSize());
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = MediaSessionsService.CurrentLyricsData?.GetLyricsLine(value);
        }

        partial void OnRootHeightChanged(double value)
        {
            RenderSongInfo();
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

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.TotalTime))
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

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.AlbumArtX))
                {
                    AlbumArtTranslation = AlbumArtTranslation.WithElement(0, (float)message.NewValue - 32);
                }
                else if (message.PropertyName == nameof(LyricsRendererViewModel.AlbumArtY))
                {
                    AlbumArtTranslation = AlbumArtTranslation.WithElement(1, (float)message.NewValue - 32);
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.SongInfoFontSize))
                {
                    RenderSongInfo();
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.IsAutoSongInfoFontSize))
                {
                    RenderSongInfo();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    RenderSongInfo();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    RenderSongInfo();
                }
            }
        }

        public async void Receive(PropertyChangedMessage<SongInfo?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentSongInfo))
                {
                    SongInfoOpacity = 0;
                    await Task.Delay(Constants.Time.AnimationDuration);
                    RenderSongInfo();
                    SongInfoOpacity = 1;
                }
            }
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
    }
}
