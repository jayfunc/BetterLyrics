using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
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
    public partial class InAppLyricsPageViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<int>>
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double LimitedLineWidth { get; set; } = 0.0;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial DisplayType DisplayType { get; set; } = DisplayType.PlaceholderOnly;

        [ObservableProperty]
        public partial BitmapImage? CoverImage { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        [ObservableProperty]
        public partial DisplayType? PreferredDisplayType { get; set; } = DisplayType.SplitView;

        [ObservableProperty]
        public partial bool AboutToUpdateUI { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsImmersiveMode { get; set; }

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

        private readonly IPlaybackService _playbackService;

        public InAppLyricsPageViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
            CoverImageRadius = _settingsService.CoverImageRadius;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += async (_, args) =>
                await UpdateSongInfoUI(args.SongInfo);

            IsFirstRun = _settingsService.IsFirstRun;
            Task.Run(async () =>
            {
                await UpdateSongInfoUI(_playbackService.SongInfo);
            });
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
        }

        [RelayCommand]
        private void OnDisplayTypeChanged(object value)
        {
            int index = Convert.ToInt32(value);
            PreferredDisplayType = (DisplayType)index;
            DisplayType = (DisplayType)index;
        }

        public async Task UpdateSongInfoUI(SongInfo? songInfo)
        {
            AboutToUpdateUI = true;
            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

            SongInfo = songInfo;

            await Task.Delay(1);

            CoverImage =
                (songInfo?.AlbumArt == null)
                    ? null
                    : await ImageHelper.GetBitmapImageFromBytesAsync(songInfo.AlbumArt);

            DisplayType displayType;

            if (songInfo == null)
            {
                displayType = DisplayType.PlaceholderOnly;
            }
            else if (PreferredDisplayType is DisplayType preferredDisplayType)
            {
                displayType = preferredDisplayType;
            }
            else
            {
                displayType = DisplayType.SplitView;
            }

            DisplayType = displayType;

            AboutToUpdateUI = false;
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
    }
}
