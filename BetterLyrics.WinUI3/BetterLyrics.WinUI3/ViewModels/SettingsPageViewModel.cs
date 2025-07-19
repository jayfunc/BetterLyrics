// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShadowViewer.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using WinRT.Interop;
using MetadataHelper = BetterLyrics.WinUI3.Helper.MetadataHelper;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;
        private readonly ITranslateService _libreTranslateService;

        private readonly string _autoStartupTaskId = "AutoStartup";

        public SettingsPageViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService, IPlaybackService playbackService, ITranslateService libreTranslateService) : base(settingsService)
        {
            _libWatcherService = libWatcherService;
            _playbackService = playbackService;
            _libreTranslateService = libreTranslateService;

            LibreTranslateServer = _settingsService.LibreTranslateServer;
            SelectedTargetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;

            LocalLyricsFolders = [.. _settingsService.LocalLyricsFolders];
            LyricsSearchProvidersInfo = [.. _settingsService.LyricsSearchProvidersInfo];
            AlbumArtSearchProvidersInfo = [.. _settingsService.AlbumArtSearchProvidersInfo];

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
            LyricsHighlightScope = _settingsService.LyricsHighlightScope;
            IsFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;

            LyricsBgFontColorType = _settingsService.LyricsBgFontColorType;
            LyricsFgFontColorType = _settingsService.LyricsFgFontColorType;
            LyricsStrokeFontColorType = _settingsService.LyricsStrokeFontColorType;

            LyricsCustomBgFontColor = _settingsService.LyricsCustomBgFontColor;
            LyricsCustomFgFontColor = _settingsService.LyricsCustomFgFontColor;
            LyricsCustomStrokeFontColor = _settingsService.LyricsCustomStrokeFontColor;

            LyricsFontStrokeWidth = _settingsService.LyricsFontStrokeWidth;
            LyricsBackgroundTheme = _settingsService.LyricsBackgroundTheme;
            MediaSourceProvidersInfo = [.. _settingsService.MediaSourceProvidersInfo];
            IgnoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;

            LyricsScrollEasingType = _settingsService.LyricsScrollEasingType;
            LyricsScrollDuration = _settingsService.LyricsScrollDuration;
            TimelineSyncThreshold = _settingsService.TimelineSyncThreshold;

            IsLyricsFloatAnimationEnabled = _settingsService.IsLyricsFloatAnimationEnabled;
            ResetPositionOffsetOnSongChanged = _settingsService.ResetPositionOffsetOnSongChanged;
            LockHotKeyIndex = _settingsService.LockHotKeyIndex;

            LXMusicServer = _settingsService.LXMusicServer;
            DockPlacement = _settingsService.DockPlacement;
            LyricsBgFontOpacity = _settingsService.LyricsBgFontOpacity;
            HideWindowWhenNotPlaying = _settingsService.HideWindowWhenNotPlaying;
            DockWindowHeight = _settingsService.DockWindowHeight;

            SystemFontNames = [.. FontHelper.SystemFontFamilies];
            SelectedFontFamilyIndex = _settingsService.SelectedFontFamilyIndex;
            LyricsFontFamily = _settingsService.LyricsFontFamily;

            _playbackService.MediaSourceProvidersInfoChanged += PlaybackService_SessionIdsChanged;

            Task.Run(async () =>
            {
                BuildDate = (await Helper.MetadataHelper.GetBuildDate()).ToString("(yyyy/MM/dd HH:mm:ss)");
            });
        }

        private void PlaybackService_SessionIdsChanged(object? sender, Events.MediaSourceProvidersInfoEventArgs e)
        {
            MediaSourceProvidersInfo = [.. e.MediaSourceProviersInfo];
        }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial string LyricsFontFamily { get; set; }


        [ObservableProperty]
        public partial ObservableCollection<string> SystemFontNames { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int SelectedFontFamilyIndex { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial DockPlacement DockPlacement { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LockHotKeyIndex { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme LyricsBackgroundTheme { get; set; }

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
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; }

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
        public partial Color LyricsCustomBgFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomFgFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomStrokeFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBgFontOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsBgFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFgFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsStrokeFontColorType { get; set; }

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
        public partial LineRenderingType LyricsHighlightScope { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool ResetPositionOffsetOnSongChanged { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsFloatAnimationEnabled { get; set; }

        public string Version { get; set; } = MetadataHelper.AppVersion;

        public string BuildDate { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string LibreTranslateServer { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int SelectedTargetLanguageIndex { get; set; } = 0;

        [ObservableProperty]
        public partial bool IsLibreTranslateServerTesting { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontStrokeWidth { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IgnoreFullscreenWindow { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial EasingType LyricsScrollEasingType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsScrollDuration { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int TimelineSyncThreshold { get; set; }

        [ObservableProperty]
        public partial bool IsLXMusicServerTesting { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial string LXMusicServer { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool HideWindowWhenNotPlaying { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int DockWindowHeight { get; set; }

        public void OnLyricsSearchProvidersReordered()
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        public void OnAlbumArtSearchProvidersReordered()
        {
            _settingsService.AlbumArtSearchProvidersInfo = [.. AlbumArtSearchProvidersInfo];
            Broadcast(
                AlbumArtSearchProvidersInfo,
                AlbumArtSearchProvidersInfo,
                nameof(AlbumArtSearchProvidersInfo)
            );
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

        public void ToggleAlbumArtSearchProvider(AlbumArtSearchProviderInfo providerInfo)
        {
            _settingsService.AlbumArtSearchProvidersInfo = [.. AlbumArtSearchProvidersInfo];
            Broadcast(
                AlbumArtSearchProvidersInfo,
                AlbumArtSearchProvidersInfo,
                nameof(AlbumArtSearchProvidersInfo)
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
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (LocalLyricsFolders.Any(x => Path.GetFullPath(x.Path).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathExistedInfo"));
            }
            else if (LocalLyricsFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                // 添加的文件夹是现有文件夹的子文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathBeIncludedInfo"));
            }
            else if (LocalLyricsFolders.Any(item => Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathIncludingOthersInfo"));
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
            await Launcher.LaunchUriAsync(new Uri(MetadataHelper.GithubUrl));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            await Launcher.LaunchFolderPathAsync(PathHelper.CacheFolder);
        }

        [RelayCommand]
        private static void RestartApp()
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

        [RelayCommand]
        private void LibreTranslateServerTest()
        {
            IsLibreTranslateServerTesting = true;
            Task.Run(async () =>
            {
                try
                {
                    string targetLangCode = LanguageHelper.SupportedTargetLanguages[SelectedTargetLanguageIndex].Code;
                    string result = await _libreTranslateService.TranslateTextAsync("Hello, world!", targetLangCode, null);
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
                        IsLibreTranslateServerTesting = false;
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                        IsLibreTranslateServerTesting = false;
                    });
                }
            });
        }

        [RelayCommand]
        private void LXMusicServerTest()
        {
            IsLXMusicServerTesting = true;
            Task.Run(async () =>
            {
                bool testResult = await NetHelper.CheckConnectivity($"{LXMusicServer}/status");
                _dispatcherQueue.TryEnqueue(() =>
                {
                    App.Current.SettingsWindowNotificationPanel?.Notify(
                        App.ResourceLoader!.GetString($"SettingsPageServerTest{(testResult ? "Success" : "Failed")}Info"),
                        testResult ? InfoBarSeverity.Success : InfoBarSeverity.Error);
                    IsLXMusicServerTesting = false;
                });
            });
        }

        public async Task<bool> ToggleAutoStartupAsync(bool target)
        {
            StartupTask startupTask = await StartupTask.GetAsync(_autoStartupTaskId);
            if (target)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }
            return await DetectIsAutoStartupEnabledAsync();
        }

        public async Task<bool> DetectIsAutoStartupEnabledAsync()
        {
            bool result = false;
            var startupTask = await StartupTask.GetAsync(_autoStartupTaskId);
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                case StartupTaskState.DisabledByUser:
                case StartupTaskState.DisabledByPolicy:
                    result = false;
                    break;
                case StartupTaskState.Enabled:
                    result = true;
                    break;
            }
            return result;
        }

        partial void OnDockPlacementChanged(DockPlacement value)
        {
            _settingsService.DockPlacement = value;
        }
        partial void OnLyricsScrollEasingTypeChanged(EasingType value)
        {
            _settingsService.LyricsScrollEasingType = value;
        }
        partial void OnLyricsScrollDurationChanged(int value)
        {
            _settingsService.LyricsScrollDuration = value;
        }
        partial void OnLyricsBackgroundThemeChanged(ElementTheme value)
        {
            _settingsService.LyricsBackgroundTheme = value;
        }
        partial void OnLyricsFontStrokeWidthChanged(int value)
        {
            _settingsService.LyricsFontStrokeWidth = value;
        }
        partial void OnIgnoreFullscreenWindowChanged(bool value)
        {
            _settingsService.IgnoreFullscreenWindow = value;
        }
        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            _settingsService.SelectedTargetLanguageIndex = value;
        }
        partial void OnLibreTranslateServerChanged(string value)
        {
            _settingsService.LibreTranslateServer = value;
        }
        partial void OnLXMusicServerChanged(string value)
        {
            _settingsService.LXMusicServer = value;
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
        partial void OnLyricsCustomBgFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomBgFontColor = value;
        }
        partial void OnLyricsCustomFgFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomFgFontColor = value;
        }
        partial void OnLyricsCustomStrokeFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomStrokeFontColor = value;
        }
        partial void OnLyricsBgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsBgFontColorType = value;
        }
        partial void OnLyricsFgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFgFontColorType = value;
        }
        partial void OnLyricsStrokeFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsStrokeFontColorType = value;
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
        partial void OnLyricsHighlightScopeChanged(LineRenderingType value)
        {
            _settingsService.LyricsHighlightScope = value;
        }
        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }
        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }
        partial void OnTimelineSyncThresholdChanged(int value)
        {
            _settingsService.TimelineSyncThreshold = value;
        }
        partial void OnIsLyricsFloatAnimationEnabledChanged(bool value)
        {
            _settingsService.IsLyricsFloatAnimationEnabled = value;
        }
        partial void OnResetPositionOffsetOnSongChangedChanged(bool value)
        {
            _settingsService.ResetPositionOffsetOnSongChanged = value;
        }
        partial void OnLyricsBgFontOpacityChanged(int value)
        {
            _settingsService.LyricsBgFontOpacity = value;
        }
        partial void OnHideWindowWhenNotPlayingChanged(bool value)
        {
            _settingsService.HideWindowWhenNotPlaying = value;
        }
        partial void OnDockWindowHeightChanged(int value)
        {
            _settingsService.DockWindowHeight = value;
        }
        partial void OnSelectedFontFamilyIndexChanged(int value)
        {
            _settingsService.SelectedFontFamilyIndex = value;
            LyricsFontFamily = SystemFontNames[value];
        }
        partial void OnLyricsFontFamilyChanged(string value)
        {
            _settingsService.LyricsFontFamily = value;
        }
    }
}
