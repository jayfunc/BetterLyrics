// 2025/6/23 by Zhe Fang

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="LyricsPageViewModel" />
    /// </summary>
    public partial class LyricsPageViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<LyricsStatus>>
    {
        #region Fields

        /// <summary>
        /// Defines the _playbackService
        /// </summary>
        private readonly IPlaybackService _playbackService;

        /// <summary>
        /// Defines the _preferredDisplayTypeBeforeSwitchToDockMode
        /// </summary>
        private LyricsDisplayType? _preferredDisplayTypeBeforeSwitchToDockMode;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsPageViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        /// <param name="playbackService">The playbackService<see cref="IPlaybackService"/></param>
        public LyricsPageViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
            LyricsFontSize = _settingsService.LyricsFontSize;
            CoverImageRadius = _settingsService.CoverImageRadius;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += async (_, args) =>
                await UpdateSongInfoUI(args.SongInfo).ConfigureAwait(true);

            IsFirstRun = _settingsService.IsFirstRun;

            UpdateSongInfoUI(_playbackService.SongInfo).ConfigureAwait(true);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether AboutToUpdateUI
        /// </summary>
        [ObservableProperty]
        public partial bool AboutToUpdateUI { get; set; }

        /// <summary>
        /// Gets or sets the CoverImage
        /// </summary>
        [ObservableProperty]
        public partial BitmapImage? CoverImage { get; set; }

        /// <summary>
        /// Gets or sets the CoverImageGridActualHeight
        /// </summary>
        [ObservableProperty]
        public partial double CoverImageGridActualHeight { get; set; }

        /// <summary>
        /// Gets or sets the CoverImageGridCornerRadius
        /// </summary>
        [ObservableProperty]
        public partial CornerRadius CoverImageGridCornerRadius { get; set; }

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        [ObservableProperty]
        public partial int CoverImageRadius { get; set; }

        /// <summary>
        /// Gets or sets the DisplayType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; } =
            LyricsDisplayType.PlaceholderOnly;

        /// <summary>
        /// Gets or sets a value indicating whether IsFirstRun
        /// </summary>
        [ObservableProperty]
        public partial bool IsFirstRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsNotMockMode
        /// </summary>
        [ObservableProperty]
        public partial bool IsNotMockMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether IsWelcomeTeachingTipOpen
        /// </summary>
        [ObservableProperty]
        public partial bool IsWelcomeTeachingTipOpen { get; set; }

        /// <summary>
        /// Gets or sets the LimitedLineWidth
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double MaxLyricsWidth { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        /// <summary>
        /// Gets or sets the LyricsStatus
        /// </summary>
        [ObservableProperty]
        public partial LyricsStatus LyricsStatus { get; set; } = LyricsStatus.Loading;

        /// <summary>
        /// Gets or sets the PreferredDisplayType
        /// </summary>
        [ObservableProperty]
        public partial LyricsDisplayType? PreferredDisplayType { get; set; } =
            LyricsDisplayType.SplitView;

        /// <summary>
        /// Gets or sets the SongInfo
        /// </summary>
        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        #endregion

        #region Methods

        /// <summary>
        /// The OpenMatchedFileFolderInFileExplorer
        /// </summary>
        /// <param name="path">The path<see cref="string"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{bool}"/></param>
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
                else if (message.PropertyName == nameof(HostWindowViewModel.IsDesktopMode))
                {
                    if (message.NewValue) { }
                    else { }
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{int}"/></param>
        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.CoverImageRadius))
                {
                    CoverImageRadius = message.NewValue;
                }
            }
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontSize))
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsStatus}"/></param>
        public void Receive(PropertyChangedMessage<LyricsStatus> message)
        {
            if (message.Sender is LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.LyricsStatus))
                {
                    LyricsStatus = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The UpdateSongInfoUI
        /// </summary>
        /// <param name="songInfo">The songInfo<see cref="SongInfo?"/></param>
        /// <returns>The <see cref="Task"/></returns>
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

        /// <summary>
        /// The OnDisplayTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="object"/></param>
        [RelayCommand]
        private void OnDisplayTypeChanged(object value)
        {
            int index = Convert.ToInt32(value);
            PreferredDisplayType = (LyricsDisplayType)index;
            DisplayType = (LyricsDisplayType)index;
        }

        /// <summary>
        /// The OpenSettingsWindow
        /// </summary>
        [RelayCommand]
        private void OpenSettingsWindow()
        {
            WindowHelper.OpenSettingsWindow();
        }

        /// <summary>
        /// The TrySwitchToPreferredDisplayType
        /// </summary>
        /// <param name="songInfo">The songInfo<see cref="SongInfo?"/></param>
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

        /// <summary>
        /// The OnCoverImageGridActualHeightChanged
        /// </summary>
        /// <param name="value">The value<see cref="double"/></param>
        partial void OnCoverImageGridActualHeightChanged(double value)
        {
            if (double.IsNaN(value))
                return;

            CoverImageGridCornerRadius = new CornerRadius(CoverImageRadius / 100f * value / 2);
        }

        /// <summary>
        /// The OnCoverImageRadiusChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnCoverImageRadiusChanged(int value)
        {
            if (double.IsNaN(CoverImageGridActualHeight))
                return;

            CoverImageGridCornerRadius = new CornerRadius(
                value / 100f * CoverImageGridActualHeight / 2
            );
        }

        /// <summary>
        /// The OnIsFirstRunChanged
        /// </summary>
        /// <param name="value">The value<see cref="bool"/></param>
        partial void OnIsFirstRunChanged(bool value)
        {
            IsWelcomeTeachingTipOpen = value;
            _settingsService.IsFirstRun = false;
        }

        #endregion
    }
}
