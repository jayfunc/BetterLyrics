using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<bool>>
    {
        private LyricsDisplayType? _preferredDisplayTypeBeforeSwitchToDockMode;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double LimitedLineWidth { get; set; } = 0.0;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; } =
            LyricsDisplayType.PlaceholderOnly;

        [ObservableProperty]
        public partial BitmapImage? CoverImage { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        [ObservableProperty]
        public partial LyricsDisplayType? PreferredDisplayType { get; set; } =
            LyricsDisplayType.SplitView;

        [ObservableProperty]
        public partial bool AboutToUpdateUI { get; set; }

        [ObservableProperty]
        public partial double CoverImageGridActualHeight { get; set; }

        [ObservableProperty]
        public partial int CoverImageRadius { get; set; }

        [ObservableProperty]
        public partial CornerRadius CoverImageGridCornerRadius { get; set; }

        [ObservableProperty]
        public partial bool IsWelcomeTeachingTipOpen { get; set; }

        [ObservableProperty]
        public partial bool IsFirstRun { get; set; }

        [ObservableProperty]
        public partial bool IsNotMockMode { get; set; } = true;

        private readonly IPlaybackService _playbackService;

        public LyricsPageViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
            CoverImageRadius = _settingsService.CoverImageRadius;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += async (_, args) =>
                await UpdateSongInfoUI(args.SongInfo).ConfigureAwait(true);

            IsFirstRun = _settingsService.IsFirstRun;

            UpdateSongInfoUI(_playbackService.SongInfo).ConfigureAwait(true);
        }

        partial void OnCoverImageRadiusChanged(int value)
        {
            if (double.IsNaN(CoverImageGridActualHeight))
                return;

            CoverImageGridCornerRadius = new CornerRadius(
                value / 100f * CoverImageGridActualHeight / 2
            );
        }

        partial void OnCoverImageGridActualHeightChanged(double value)
        {
            if (double.IsNaN(value))
                return;

            CoverImageGridCornerRadius = new CornerRadius(CoverImageRadius / 100f * value / 2);
        }

        partial void OnIsFirstRunChanged(bool value)
        {
            IsWelcomeTeachingTipOpen = value;
            _settingsService.IsFirstRun = false;
        }

        [RelayCommand]
        private void OnDisplayTypeChanged(object value)
        {
            int index = Convert.ToInt32(value);
            PreferredDisplayType = (LyricsDisplayType)index;
            DisplayType = (LyricsDisplayType)index;
        }

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            WindowHelper.OpenSettingsWindow();
        }

        public async Task UpdateSongInfoUI(SongInfo? songInfo)
        {
            AboutToUpdateUI = true;
            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

            SongInfo = songInfo;

            CoverImage =
                (songInfo?.AlbumArt == null)
                    ? null
                    : await ImageHelper.GetBitmapImageFromBytesAsync(songInfo.AlbumArt);

            TrySwitchToPreferredDisplayType(songInfo);

            AboutToUpdateUI = false;
        }

        private void TrySwitchToPreferredDisplayType(SongInfo? songInfo)
        {
            LyricsDisplayType displayType;

            if (songInfo == null)
            {
                displayType = LyricsDisplayType.PlaceholderOnly;
            }
            else if (PreferredDisplayType is LyricsDisplayType preferredDisplayType)
            {
                displayType = preferredDisplayType;
            }
            else
            {
                displayType = LyricsDisplayType.SplitView;
            }

            DisplayType = displayType;
        }

        public void OpenMatchedFileFolderInFileExplorer(string path)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                }
            );
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender.GetType() == typeof(SettingsViewModel))
            {
                if (message.PropertyName == nameof(SettingsViewModel.CoverImageRadius))
                {
                    CoverImageRadius = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.IsDockMode))
                {
                    IsNotMockMode = !message.NewValue;
                    if (message.NewValue)
                    {
                        _preferredDisplayTypeBeforeSwitchToDockMode = PreferredDisplayType;
                        PreferredDisplayType = LyricsDisplayType.LyricsOnly;
                    }
                    else
                    {
                        PreferredDisplayType = _preferredDisplayTypeBeforeSwitchToDockMode;
                    }
                    TrySwitchToPreferredDisplayType(SongInfo);
                }
            }
            else if (message.Sender is SettingsViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(SettingsViewModel.IsRebuildingLyricsIndexDatabase)
                )
                {
                    if (!message.NewValue)
                    {
                        _playbackService.ReSendingMessages();
                    }
                }
            }
        }
    }
}
