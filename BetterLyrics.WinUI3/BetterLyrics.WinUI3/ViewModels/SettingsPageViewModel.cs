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
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;

        public SettingsPageViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService, IPlaybackService playbackService) : base(settingsService)
        {
            _libWatcherService = libWatcherService;
            _playbackService = playbackService;

            LocalLyricsFolders = [.. _settingsService.LocalLyricsFolders];
            LyricsSearchProvidersInfo = [.. _settingsService.LyricsSearchProvidersInfo];

            Language = _settingsService.Language;
            CoverImageRadius = _settingsService.CoverImageRadius;

            AutoStartWindowType = _settingsService.AutoStartWindowType;
            AutoLockOnDesktopMode = _settingsService.AutoLockOnDesktopMode;

            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            SongInfoAlignmentType = _settingsService.SongInfoAlignmentType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            IsFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;
            LyricsFontColorType = _settingsService.LyricsFontColorType;
            LyricsCustomFontColor = _settingsService.LyricsCustomFontColor;

            MediaSourceProvidersInfo = [.. _settingsService.MediaSourceProvidersInfo];
            _playbackService.MediaSourceProvidersInfoChanged += PlaybackService_SessionIdsChanged;

            Task.Run(async () =>
            {
                BuildDate = (await AppInfo.GetBuildDate()).ToString("(yyyy/MM/dd HH:mm:ss)");
            });
        }

        private void PlaybackService_SessionIdsChanged(object? sender, Events.MediaSourceProvidersInfoEventArgs e)
        {
            MediaSourceProvidersInfo = [.. e.MediaSourceProviersInfo];
        }

        [ObservableProperty]
        public partial AutoStartWindowType AutoStartWindowType { get; set; }

        [ObservableProperty]
        public partial bool AutoLockOnDesktopMode { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverImageRadius { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLogEnabled { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDynamicCoverOverlayEnabled { get; set; }

        [ObservableProperty]
        public partial Enums.Language Language { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LocalLyricsFolder> LocalLyricsFolders { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsFanLyricsEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TextAlignmentType LyricsAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TextAlignmentType SongInfoAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LineRenderingType LyricsGlowEffectScope { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; }

        public string Version { get; set; } = Helper.AppInfo.AppVersion;

        public string BuildDate { get; set; } = string.Empty;

        public void OnLyricsSearchProvidersReordered()
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        public void OpenMusicFolder(LocalLyricsFolder folder)
        {
            OpenFolderInFileExplorer(folder.Path);
        }

        public void RemoveFolderAsync(LocalLyricsFolder folder)
        {
            LocalLyricsFolders.Remove(folder);
            _settingsService.LocalLyricsFolders = [.. LocalLyricsFolders];
            _libWatcherService.UpdateWatchers([.. LocalLyricsFolders]);
            Broadcast(LocalLyricsFolders, LocalLyricsFolders, nameof(LocalLyricsFolders));
        }

        public void ToggleLocalLyricsFolder(LocalLyricsFolder folder)
        {
            _settingsService.LocalLyricsFolders = [.. LocalLyricsFolders];
            Broadcast(LocalLyricsFolders, LocalLyricsFolders, nameof(LocalLyricsFolders));
        }

        public void ToggleLyricsSearchProvider(LyricsSearchProviderInfo providerInfo)
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        public void ToggleMediaSourceProvider(MediaSourceProviderInfo providerInfo)
        {
            Broadcast(
                MediaSourceProvidersInfo,
                MediaSourceProvidersInfo,
                nameof(MediaSourceProvidersInfo)
            );
        }

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

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(Helper.AppInfo.GithubUrl));
        }

        [RelayCommand]
        private void OpenCacheFolder()
        {
            OpenFolderInFileExplorer(Helper.AppInfo.CacheFolder);
        }

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

        [RelayCommand]
        private void PlayTestingMusicTask()
        {
            WindowHelper.OpenOrShowWindow<LyricsWindow>();
        }

        [RelayCommand]
        private void RestartApp()
        {
            WindowHelper.RestartApp();
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync(UIElement sender)
        {
            var window = WindowHelper.GetWindowByWindowType<SettingsWindow>();
            if (window == null) return;

            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                AddFolderAsync(folder.Path);
            }
        }

        partial void OnAutoStartWindowTypeChanged(AutoStartWindowType value)
        {
            _settingsService.AutoStartWindowType = value;
        }

        partial void OnAutoLockOnDesktopModeChanged(bool value)
        {
            _settingsService.AutoLockOnDesktopMode = value;
        }

        partial void OnCoverImageRadiusChanged(int value)
        {
            _settingsService.CoverImageRadius = value;
        }

        partial void OnCoverOverlayBlurAmountChanged(int value)
        {
            _settingsService.CoverOverlayBlurAmount = value;
        }

        partial void OnCoverOverlayOpacityChanged(int value)
        {
            _settingsService.CoverOverlayOpacity = value;
        }

        partial void OnIsDynamicCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsDynamicCoverOverlayEnabled = value;
        }

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

        partial void OnIsFanLyricsEnabledChanged(bool value)
        {
            _settingsService.IsFanLyricsEnabled = value;
        }

        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsLyricsGlowEffectEnabled = value;
        }

        partial void OnLyricsAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.LyricsAlignmentType = value;
        }

        partial void OnSongInfoAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.SongInfoAlignmentType = value;
        }

        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.LyricsBlurAmount = value;
        }

        partial void OnLyricsCustomFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomFontColor = value;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFontColorType = value;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.LyricsFontSize = value;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.LyricsFontWeight = value;
        }

        partial void OnLyricsGlowEffectScopeChanged(LineRenderingType value)
        {
            _settingsService.LyricsGlowEffectScope = value;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }

        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }
    }
}
