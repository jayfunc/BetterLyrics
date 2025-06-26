// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="SettingsViewModel" />
    /// </summary>
    public partial class SettingsViewModel : ObservableRecipient
    {
        #region Fields

        /// <summary>
        /// Defines the _libWatcherService
        /// </summary>
        private readonly ILibWatcherService _libWatcherService;

        /// <summary>
        /// Defines the _mediaPlayer
        /// </summary>
        private readonly MediaPlayer _mediaPlayer = new();

        /// <summary>
        /// Defines the _playbackService
        /// </summary>
        private readonly IPlaybackService _playbackService;

        /// <summary>
        /// Defines the _settingsService
        /// </summary>
        private readonly ISettingsService _settingsService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        /// <param name="libWatcherService">The libWatcherService<see cref="ILibWatcherService"/></param>
        /// <param name="playbackService">The playbackService<see cref="IPlaybackService"/></param>
        public SettingsViewModel(
            ISettingsService settingsService,
            ILibWatcherService libWatcherService,
            IPlaybackService playbackService
        )
        {
            _settingsService = settingsService;
            _libWatcherService = libWatcherService;
            _playbackService = playbackService;

            RootGridMargin = new Thickness(0, _settingsService.TitleBarType.GetHeight(), 0, 0);

            LocalLyricsFolders = [.. _settingsService.LocalLyricsFolders];
            LyricsSearchProvidersInfo = [.. _settingsService.LyricsSearchProvidersInfo];

            Language = _settingsService.Language;
            CoverImageRadius = _settingsService.CoverImageRadius;
            ThemeType = _settingsService.ThemeType;
            BackdropType = _settingsService.BackdropType;
            TitleBarType = _settingsService.TitleBarType;

            AutoStartWindowType = _settingsService.AutoStartWindowType;

            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the AutoStartWindowType
        /// </summary>
        [ObservableProperty]
        public partial AutoStartWindowType AutoStartWindowType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the BackdropType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial BackdropType BackdropType { get; set; }

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverImageRadius { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayBlurAmount
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayOpacity
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayOpacity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsCoverOverlayEnabled
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsDynamicCoverOverlayEnabled
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDynamicCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets the Language
        /// </summary>
        [ObservableProperty]
        public partial Enums.Language Language { get; set; }

        /// <summary>
        /// Gets or sets the LocalLyricsFolders
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<LocalLyricsFolder> LocalLyricsFolders { get; set; }

        /// <summary>
        /// Gets or sets the LyricsSearchProvidersInfo
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        /// <summary>
        /// Gets or sets the NavViewSelectedItemTag
        /// </summary>
        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "LyricsLib";

        /// <summary>
        /// Gets or sets the RootGridMargin
        /// </summary>
        [ObservableProperty]
        public partial Thickness RootGridMargin { get; set; } = new(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the ThemeType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeType { get; set; }

        /// <summary>
        /// Gets or sets the TitleBarType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TitleBarType TitleBarType { get; set; }

        /// <summary>
        /// Gets or sets the Version
        /// </summary>
        public string Version { get; set; } = AppInfo.AppVersion;

        #endregion

        #region Methods

        /// <summary>
        /// The OnLyricsSearchProvidersReordered
        /// </summary>
        public void OnLyricsSearchProvidersReordered()
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        /// <summary>
        /// The OpenMusicFolder
        /// </summary>
        /// <param name="folder">The folder<see cref="LocalLyricsFolder"/></param>
        public void OpenMusicFolder(LocalLyricsFolder folder)
        {
            OpenFolderInFileExplorer(folder.Path);
        }

        /// <summary>
        /// The RemoveFolderAsync
        /// </summary>
        /// <param name="folder">The folder<see cref="LocalLyricsFolder"/></param>
        public void RemoveFolderAsync(LocalLyricsFolder folder)
        {
            LocalLyricsFolders.Remove(folder);
            _settingsService.LocalLyricsFolders = [.. LocalLyricsFolders];
            _libWatcherService.UpdateWatchers([.. LocalLyricsFolders]);
            Broadcast(LocalLyricsFolders, LocalLyricsFolders, nameof(LocalLyricsFolders));
        }

        /// <summary>
        /// The ToggleLocalLyricsFolder
        /// </summary>
        /// <param name="folder">The folder<see cref="LocalLyricsFolder"/></param>
        public void ToggleLocalLyricsFolder(LocalLyricsFolder folder)
        {
            _settingsService.LocalLyricsFolders = [.. LocalLyricsFolders];
            Broadcast(LocalLyricsFolders, LocalLyricsFolders, nameof(LocalLyricsFolders));
        }

        /// <summary>
        /// The ToggleLyricsSearchProvider
        /// </summary>
        /// <param name="providerInfo">The providerInfo<see cref="LyricsSearchProviderInfo"/></param>
        public void ToggleLyricsSearchProvider(LyricsSearchProviderInfo providerInfo)
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        /// <summary>
        /// The AddFolderAsync
        /// </summary>
        /// <param name="path">The path<see cref="string"/></param>
        private void AddFolderAsync(string path)
        {
            var normalizedPath =
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (
                LocalLyricsFolders.Any(x =>
                    Path.GetFullPath(x.Path)
                        .TrimEnd(Path.DirectorySeparatorChar)
                        .Equals(
                            normalizedPath.TrimEnd(Path.DirectorySeparatorChar),
                            StringComparison.OrdinalIgnoreCase
                        )
                )
            )
            {
                WeakReferenceMessenger.Default.Send(
                    new ShowNotificatonMessage(
                        new Notification(
                            App.ResourceLoader!.GetString("SettingsPagePathExistedInfo")
                        )
                    )
                );
            }
            else if (
                LocalLyricsFolders.Any(item =>
                    normalizedPath.StartsWith(
                        Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar)
                            + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                // 添加的文件夹是现有文件夹的子文件夹
                WeakReferenceMessenger.Default.Send(
                    new ShowNotificatonMessage(
                        new Notification(
                            App.ResourceLoader!.GetString("SettingsPagePathBeIncludedInfo")
                        )
                    )
                );
            }
            else if (
                LocalLyricsFolders.Any(item =>
                    Path.GetFullPath(item.Path)
                        .TrimEnd(Path.DirectorySeparatorChar)
                        .StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                WeakReferenceMessenger.Default.Send(
                    new ShowNotificatonMessage(
                        new Notification(
                            App.ResourceLoader!.GetString("SettingsPagePathIncludingOthersInfo")
                        )
                    )
                );
            }
            else
            {
                LocalLyricsFolders.Add(new LocalLyricsFolder(path, true));
                _settingsService.LocalLyricsFolders = [.. LocalLyricsFolders];
                _libWatcherService.UpdateWatchers([.. LocalLyricsFolders]);
                Broadcast(LocalLyricsFolders, LocalLyricsFolders, nameof(LocalLyricsFolders));
            }
        }

        /// <summary>
        /// The LaunchProjectGitHubPageAsync
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(AppInfo.GithubUrl));
        }

        /// <summary>
        /// The OpenFolderInFileExplorer
        /// </summary>
        /// <param name="path">The path<see cref="string"/></param>
        private void OpenFolderInFileExplorer(string path)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true,
                }
            );
        }

        /// <summary>
        /// The OpenLogFolder
        /// </summary>
        [RelayCommand]
        private void OpenCacheFolder()
        {
            OpenFolderInFileExplorer(AppInfo.CacheFolder);
        }

        /// <summary>
        /// The PlayTestingMusicTask
        /// </summary>
        [RelayCommand]
        private void PlayTestingMusicTask()
        {
            AddFolderAsync(AppInfo.AssetsFolder);
            _mediaPlayer.SetUriSource(new Uri(AppInfo.TestMusicPath));
            _mediaPlayer.Play();
        }

        /// <summary>
        /// The RestartApp
        /// </summary>
        [RelayCommand]
        private void RestartApp()
        {
            // The restart will be executed immediately.
            AppRestartFailureReason failureReason =
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");

            // If the restart fails, handle it here.
            switch (failureReason)
            {
                case AppRestartFailureReason.RestartPending:
                    break;
                case AppRestartFailureReason.NotInForeground:
                    break;
                case AppRestartFailureReason.InvalidUser:
                    break;
                default: //AppRestartFailureReason.Other
                    break;
            }
        }

        /// <summary>
        /// The SelectAndAddFolderAsync
        /// </summary>
        /// <param name="sender">The sender<see cref="UIElement"/></param>
        /// <returns>The <see cref="Task"/></returns>
        [RelayCommand]
        private async Task SelectAndAddFolderAsync(UIElement sender)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(WindowHelper.GetWindowForElement(sender));
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                AddFolderAsync(folder.Path);
            }
        }

        /// <summary>
        /// The OnAutoStartWindowTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="AutoStartWindowType"/></param>
        partial void OnAutoStartWindowTypeChanged(AutoStartWindowType value)
        {
            _settingsService.AutoStartWindowType = value;
        }

        /// <summary>
        /// The OnBackdropTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="BackdropType"/></param>
        partial void OnBackdropTypeChanged(BackdropType value)
        {
            _settingsService.BackdropType = value;
        }

        /// <summary>
        /// The OnCoverImageRadiusChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnCoverImageRadiusChanged(int value)
        {
            _settingsService.CoverImageRadius = value;
        }

        /// <summary>
        /// The OnCoverOverlayBlurAmountChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnCoverOverlayBlurAmountChanged(int value)
        {
            _settingsService.CoverOverlayBlurAmount = value;
        }

        /// <summary>
        /// The OnCoverOverlayOpacityChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnCoverOverlayOpacityChanged(int value)
        {
            _settingsService.CoverOverlayOpacity = value;
        }

        /// <summary>
        /// The OnIsCoverOverlayEnabledChanged
        /// </summary>
        /// <param name="value">The value<see cref="bool"/></param>
        partial void OnIsCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsCoverOverlayEnabled = value;
        }

        /// <summary>
        /// The OnIsDynamicCoverOverlayEnabledChanged
        /// </summary>
        /// <param name="value">The value<see cref="bool"/></param>
        partial void OnIsDynamicCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsDynamicCoverOverlayEnabled = value;
        }

        /// <summary>
        /// The OnLanguageChanged
        /// </summary>
        /// <param name="value">The value<see cref="Enums.Language"/></param>
        partial void OnLanguageChanged(Enums.Language value)
        {
            switch (value)
            {
                case Enums.Language.FollowSystem:
                    ApplicationLanguages.PrimaryLanguageOverride = "";
                    break;
                case Enums.Language.English:
                    ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                    break;
                case Enums.Language.SimplifiedChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
                    break;
                case Enums.Language.TraditionalChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-TW";
                    break;
                case Enums.Language.Japanese:
                    ApplicationLanguages.PrimaryLanguageOverride = "ja-JP";
                    break;
                case Enums.Language.Korean:
                    ApplicationLanguages.PrimaryLanguageOverride = "ko-KR";
                    break;
                default:
                    break;
            }
            _settingsService.Language = Language;
        }

        /// <summary>
        /// The OnThemeTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="ElementTheme"/></param>
        partial void OnThemeTypeChanged(ElementTheme value)
        {
            _settingsService.ThemeType = value;
        }

        /// <summary>
        /// The OnTitleBarTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="TitleBarType"/></param>
        partial void OnTitleBarTypeChanged(TitleBarType value)
        {
            _settingsService.TitleBarType = value;
            RootGridMargin = new Thickness(0, value.GetHeight(), 0, 0);
        }

        #endregion
    }
}
