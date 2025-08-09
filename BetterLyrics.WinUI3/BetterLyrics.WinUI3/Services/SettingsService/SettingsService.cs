// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    public class SettingsService : ISettingsService
    {
        public const string LyricsCustomBgFontColorKey = "LyricsCustomBgFontColor";
        public const string LyricsCustomFgFontColorKey = "LyricsCustomFgFontColor";
        public const string LyricsCustomStrokeFontColorKey = "LyricsCustomStrokeFontColor";

        // App behavior

        private const string AutoStartWindowTypeKey = "AutoStartWindowType";

        private const string CoverImageRadiusKey = "AlbumArtCornerRadius";
        private const string CoverOverlayBlurAmountKey = "CoverOverlayBlurAmount";
        private const string CoverOverlayOpacityKey = "CoverOverlayOpacity";
        private const string CoverOverlaySpeedKey = "CoverOverlaySpeed";
        private const string IsCoverOverlayEnabledKey = "IsCoverOverlayEnabled";

        private const string CoverAcrylicEffectAmountKey = "CoverAcrylicEffectAmount";

        private const string DesktopWindowLeftKey = "DesktopWindowLeft";
        private const string DesktopWindowTopKey = "DesktopWindowTop";
        private const string DesktopWindowWidthKey = "DesktopWindowWidth";
        private const string DesktopWindowHeightKey = "DesktopWindowHeight";

        private const string StandardWindowLeftKey = "StandardWindowLeft";
        private const string StandardWindowTopKey = "StandardWindowTop";
        private const string StandardWindowWidthKey = "StandardWindowWidth";
        private const string StandardWindowHeightKey = "StandardWindowHeight";

        private const string AutoLockOnDesktopModeKey = "AutoLockOnDesktopMode";
        private const string IsImmersiveModeKey = "IsImmersiveMode";

        private const string IsFanLyricsEnabledKey = "IsFanLyricsEnabled";
        private const string IsFirstRunKey = "IsFirstRun";
        private const string IsLyricsGlowEffectEnabledKey = "IsLyricsGlowEffectEnabled";
        private const string LanguageKey = "Language";

        private const string LocalMediaFoldersKey = "LocalLyricsFolders.json";
        private const string LyricsAlignmentTypeKey = "TextAlignmentType";
        private const string SongInfoAlignmentTypeKey = "SongInfoAlignmentType";
        private const string LyricsBlurAmountKey = "LyricsBlurAmount";

        private const string LyricsBgFontColorTypeKey = "_lyricsBgFontColorType";
        private const string LyricsFgFontColorTypeKey = "LyricsFgFontColorType";
        private const string LyricsStrokeFontColorTypeKey = "LyricsStrokeFontColorType";

        private const string LyricsFontStrokeWidthKey = "LyricsFontStrokeWidth";

        // Lyrics font size
        private const string LyricsStandardFontSizeKey = "LyricsStandardFontSize";
        private const string LyricsDockFontSizeKey = "LyricsDockFontSize";
        private const string LyricsDesktopFontSizeKey = "LyricsDesktopFontSize";

        private const string LyricsFontWeightKey = "LyricsFontWeightKey";
        private const string LyricsGlowEffectScopeKey = "LyricsGlowEffectScope";
        private const string LyricsHighlightSopeKey = "LyricsHighlightSope";
        private const string LyricsLineSpacingFactorKey = "LyricsLineSpacingFactor";
        private const string LyricsVerticalEdgeOpacityKey = "LyricsVerticalEdgeOpacity";

        private const string MediaSourceProvidersInfoKey = "MediaSourceProvidersInfo.json";

        // Translation
        private const string IsTranslationEnabledKey = "IsTranslationEnabled";
        private const string ShowTranslationOnlyKey = "ShowTranslationOnly";
        private const string IsLibreTranslateEnabledKey = "IsLibreTranslateEnabled";
        private const string LibreTranslateServerKey = "LibreTranslateServer";
        private const string SelectedTargetLanguageIndexKey = "SelectedTargetLanguageIndex";

        // LX Music
        private const string LXMusicServerKey = "LXMusicServer";

        private const string LyricsBackgroundThemeKey = "LyricsBackgroundTheme";
        private const string IgnoreFullscreenWindowKey = "IgnoreFullscreenWindow";
        private const string PreferredDisplayTypeKey = "PreferredDisplayTypeKey";

        private const string LyricsScrollEasingTypeKey = "LyricsScrollEasingType";
        private const string LyricsScrollDurationKey = "LyricsScrollDuration";
        private const string LyricsScrollTopDurationKey = "LyricsScrollTopDuration";
        private const string LyricsScrollBottomDurationKey = "LyricsScrollBottomDuration";

        private const string IsLyricsFloatAnimationEnabledKey = "IsLyricsFloatAnimationEnabled";

        private const string PlaybackOrderKey = "PlaybackOrder";

        private const string PositionOffsetKey = "PositionOffset";

        private const string LockHotKeyIndexKey = "LockHotKeyIndex";
        private const string DockPlacementKey = "DockPlacement";
        private const string LyricsBgFontOpacityKey = "LyricsBgFontOpacity";
        private const string HideWindowWhenNotPlayingKey = "HideWindowWhenNotPlaying";
        private const string DockWindowHeightKey = "DockWindowHeight";

        private const string SelectedFontFamilyIndexKey = "SelectedFontFamilyIndex";
        private const string LyricsFontFamilyKey = "LyricsFontFamily";
        private const string IsDragEverywhereEnabledKey = "IsDragEverywhereEnabled";

        private const string DockMonitorDeviceNameKey = "DockMonitorDeviceName";

        // LastFM
        private const string LastFMSessionKeyKey = "LastFMSessionKey";

        private const string LyricsTranslationSeparatorKey = "LyricsTranslationSeparator";

        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            SetDefault(IsFirstRunKey, true);

            SetDefault(LocalMediaFoldersKey, [], SourceGenerationContext.Default.ListLocalMediaFolder);

            SetDefault(MediaSourceProvidersInfoKey, [], SourceGenerationContext.Default.ListMediaSourceProviderInfo);
            MediaSourceProvidersInfo = MediaSourceProvidersInfo.Select(x => new MediaSourceProviderInfo()
            {
                IsEnabled = x.IsEnabled,
                Provider = x.Provider,
                IsLastFMTrackEnabled = x.IsLastFMTrackEnabled,
                TimelineSyncThreshold = x.TimelineSyncThreshold,
                ResetPositionOffsetOnSongChanged = x.ResetPositionOffsetOnSongChanged,
                PositionOffset = x.PositionOffset,
                LyricsSearchProvidersInfo = [..Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(
                    p,
                    x.LyricsSearchProvidersInfo.Where(x => x.Provider == p).FirstOrDefault()?.IsEnabled ?? true
                ))],
                AlbumArtSearchProvidersInfo = [..Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(
                    p,
                    x.AlbumArtSearchProvidersInfo.Where(x => x.Provider == p).FirstOrDefault()?.IsEnabled ?? true
                ))],
            }).ToList();

            // App appearance
            SetDefault(LanguageKey, (int)Language.FollowSystem);

            SetDefault(DesktopWindowHeightKey, 600);
            SetDefault(DesktopWindowLeftKey, 200);
            SetDefault(DesktopWindowTopKey, 200);
            SetDefault(DesktopWindowWidthKey, 1200);

            SetDefault(StandardWindowHeightKey, 800);
            SetDefault(StandardWindowLeftKey, 200);
            SetDefault(StandardWindowTopKey, 200);
            SetDefault(StandardWindowWidthKey, 1600);

            SetDefault(AutoLockOnDesktopModeKey, false);
            SetDefault(IsImmersiveModeKey, false);
            // App behavior
            SetDefault(AutoStartWindowTypeKey, (int)AutoStartWindowType.StandardMode);
            // Album art
            SetDefault(IsCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlaySpeedKey, 100); // 100 %
            SetDefault(CoverOverlayOpacityKey, 100); // 100 % = 1.0
            SetDefault(CoverOverlayBlurAmountKey, 100);
            SetDefault(CoverImageRadiusKey, 12); // 12 %
            SetDefault(CoverAcrylicEffectAmountKey, 0);
            // Lyrics
            SetDefault(LyricsAlignmentTypeKey, (int)TextAlignmentType.Left);
            SetDefault(SongInfoAlignmentTypeKey, (int)TextAlignmentType.Left);
            SetDefault(LyricsFontWeightKey, (int)LyricsFontWeight.Bold);
            SetDefault(LyricsBlurAmountKey, 5);

            SetDefault(LyricsBackgroundThemeKey, (int)ElementTheme.Dark);

            SetDefault(LyricsBgFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);
            SetDefault(LyricsFgFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);
            SetDefault(LyricsStrokeFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);

            SetDefault(LyricsCustomBgFontColorKey, Colors.White.ToInt());
            SetDefault(LyricsCustomFgFontColorKey, Colors.White.ToInt());
            SetDefault(LyricsCustomStrokeFontColorKey, Colors.White.ToInt());

            SetDefault(LyricsStandardFontSizeKey, 32);
            SetDefault(LyricsDockFontSizeKey, 16);
            SetDefault(LyricsDesktopFontSizeKey, 28);

            SetDefault(LyricsLineSpacingFactorKey, 0.5f);
            SetDefault(LyricsVerticalEdgeOpacityKey, 0);
            SetDefault(IsLyricsGlowEffectEnabledKey, true);
            SetDefault(LyricsGlowEffectScopeKey, (int)LineRenderingType.CurrentChar);
            SetDefault(LyricsHighlightSopeKey, (int)LineRenderingType.LineStartToCurrentChar);
            SetDefault(IsFanLyricsEnabledKey, false);

            SetDefault(LibreTranslateServerKey, "");
            SetDefault(IsLibreTranslateEnabledKey, false);
            SetDefault(IsTranslationEnabledKey, true);
            SetDefault(ShowTranslationOnlyKey, false);
            SetDefault(SelectedTargetLanguageIndexKey, LanguageHelper.GetDefaultTargetLanguageIndex());

            SetDefault(LXMusicServerKey, "");

            SetDefault(LyricsFontStrokeWidthKey, 3);
            SetDefault(IgnoreFullscreenWindowKey, false);
            SetDefault(PreferredDisplayTypeKey, (int)LyricsDisplayType.SplitView);

            SetDefault(LyricsScrollEasingTypeKey, (int)EasingType.EaseInOutSine);
            SetDefault(LyricsScrollTopDurationKey, 100); // ms
            SetDefault(LyricsScrollDurationKey, 500); // ms
            SetDefault(LyricsScrollBottomDurationKey, 1000); // ms

            SetDefault(IsLyricsFloatAnimationEnabledKey, true);

            SetDefault(PositionOffsetKey, 0);
            SetDefault(LockHotKeyIndexKey, 'U' - 'A');
            SetDefault(DockPlacementKey, (int)DockPlacement.Top);
            SetDefault(LyricsBgFontOpacityKey, 30); // 30%
            SetDefault(HideWindowWhenNotPlayingKey, false);
            SetDefault(DockWindowHeightKey, 64); // 64px
            SetDefault(SelectedFontFamilyIndexKey, 0);
            SetDefault(LyricsFontFamilyKey, FontHelper.SystemFontFamilies.ElementAtOrDefault(0));
            SetDefault(IsDragEverywhereEnabledKey, false);
            SetDefault(DockMonitorDeviceNameKey, MonitorHelper.GetPrimaryMonitorDeviceName());

            SetDefault(LastFMSessionKeyKey, "");

            SetDefault(LyricsTranslationSeparatorKey, StringHelper.NewLine);
        }

        public bool IsDragEverywhereEnabled
        {
            get => GetValue<bool>(IsDragEverywhereEnabledKey);
            set => SetValue(IsDragEverywhereEnabledKey, value);
        }

        public string LyricsFontFamily
        {
            get => GetValue<string>(LyricsFontFamilyKey)!;
            set => SetValue(LyricsFontFamilyKey, value);
        }

        public int SelectedFontFamilyIndex
        {
            get => GetValue<int>(SelectedFontFamilyIndexKey);
            set => SetValue(SelectedFontFamilyIndexKey, value);
        }

        public bool HideWindowWhenNotPlaying
        {
            get => GetValue<bool>(HideWindowWhenNotPlayingKey);
            set => SetValue(HideWindowWhenNotPlayingKey, value);
        }

        public int DockWindowHeight
        {
            get => GetValue<int>(DockWindowHeightKey);
            set => SetValue(DockWindowHeightKey, value);
        }

        public int LyricsBgFontOpacity
        {
            get => GetValue<int>(LyricsBgFontOpacityKey);
            set => SetValue(LyricsBgFontOpacityKey, value);
        }

        public bool ShowTranslationOnly
        {
            get => GetValue<bool>(ShowTranslationOnlyKey);
            set => SetValue(ShowTranslationOnlyKey, value);
        }

        public DockPlacement DockPlacement
        {
            get => (DockPlacement)GetValue<int>(DockPlacementKey);
            set => SetValue(DockPlacementKey, (int)value);
        }

        public int LockHotKeyIndex
        {
            get => GetValue<int>(LockHotKeyIndexKey);
            set => SetValue(LockHotKeyIndexKey, value);
        }

        public EasingType LyricsScrollEasingType
        {
            get => (EasingType)GetValue<int>(LyricsScrollEasingTypeKey);
            set => SetValue(LyricsScrollEasingTypeKey, (int)value);
        }

        public int LyricsScrollDuration
        {
            get => GetValue<int>(LyricsScrollDurationKey);
            set => SetValue(LyricsScrollDurationKey, value);
        }

        public int LyricsScrollTopDuration
        {
            get => GetValue<int>(LyricsScrollTopDurationKey);
            set => SetValue(LyricsScrollTopDurationKey, value);
        }

        public int LyricsScrollBottomDuration
        {
            get => GetValue<int>(LyricsScrollBottomDurationKey);
            set => SetValue(LyricsScrollBottomDurationKey, value);
        }

        public LyricsDisplayType DisplayType
        {
            get => (LyricsDisplayType)GetValue<int>(PreferredDisplayTypeKey);
            set => SetValue(PreferredDisplayTypeKey, (int)value);
        }

        public ElementTheme LyricsBackgroundTheme
        {
            get => (ElementTheme)GetValue<int>(LyricsBackgroundThemeKey);
            set => SetValue(LyricsBackgroundThemeKey, (int)value);
        }

        public AutoStartWindowType AutoStartWindowType
        {
            get => (AutoStartWindowType)GetValue<int>(AutoStartWindowTypeKey);
            set => SetValue(AutoStartWindowTypeKey, (int)value);
        }

        public int DesktopWindowLeft
        {
            get => GetValue<int>(DesktopWindowLeftKey);
            set => SetValue(DesktopWindowLeftKey, value);
        }

        public int DesktopWindowTop
        {
            get => GetValue<int>(DesktopWindowTopKey);
            set => SetValue(DesktopWindowTopKey, value);
        }

        public int DesktopWindowWidth
        {
            get => GetValue<int>(DesktopWindowWidthKey);
            set => SetValue(DesktopWindowWidthKey, value);
        }

        public int DesktopWindowHeight
        {
            get => GetValue<int>(DesktopWindowHeightKey);
            set => SetValue(DesktopWindowHeightKey, value);
        }

        public int StandardWindowLeft
        {
            get => GetValue<int>(StandardWindowLeftKey);
            set => SetValue(StandardWindowLeftKey, value);
        }

        public int StandardWindowTop
        {
            get => GetValue<int>(StandardWindowTopKey);
            set => SetValue(StandardWindowTopKey, value);
        }

        public int StandardWindowWidth
        {
            get => GetValue<int>(StandardWindowWidthKey);
            set => SetValue(StandardWindowWidthKey, value);
        }

        public int StandardWindowHeight
        {
            get => GetValue<int>(StandardWindowHeightKey);
            set => SetValue(StandardWindowHeightKey, value);
        }

        public bool AutoLockOnDesktopMode
        {
            get => GetValue<bool>(AutoLockOnDesktopModeKey);
            set => SetValue(AutoLockOnDesktopModeKey, value);
        }

        public int CoverImageRadius
        {
            get => GetValue<int>(CoverImageRadiusKey);
            set => SetValue(CoverImageRadiusKey, value);
        }

        public int CoverOverlayBlurAmount
        {
            get => GetValue<int>(CoverOverlayBlurAmountKey);
            set => SetValue(CoverOverlayBlurAmountKey, value);
        }

        public int CoverOverlayOpacity
        {
            get => GetValue<int>(CoverOverlayOpacityKey);
            set => SetValue(CoverOverlayOpacityKey, value);
        }

        public int CoverOverlaySpeed
        {
            get => GetValue<int>(CoverOverlaySpeedKey);
            set => SetValue(CoverOverlaySpeedKey, value);
        }

        public int CoverAcrylicEffectAmount
        {
            get => GetValue<int>(CoverAcrylicEffectAmountKey);
            set => SetValue(CoverAcrylicEffectAmountKey, value);
        }

        public bool IsFanLyricsEnabled
        {
            get => GetValue<bool>(IsFanLyricsEnabledKey);
            set => SetValue(IsFanLyricsEnabledKey, value);
        }

        public bool IsFirstRun
        {
            get => GetValue<bool>(IsFirstRunKey);
            set => SetValue(IsFirstRunKey, value);
        }

        public bool IsLyricsGlowEffectEnabled
        {
            get => GetValue<bool>(IsLyricsGlowEffectEnabledKey);
            set => SetValue(IsLyricsGlowEffectEnabledKey, value);
        }

        public Language Language
        {
            get => (Language)GetValue<int>(LanguageKey);
            set => SetValue(LanguageKey, (int)value);
        }

        public List<LocalMediaFolder> LocalMediaFolders
        {
            get => GetValue(LocalMediaFoldersKey, SourceGenerationContext.Default.ListLocalMediaFolder)!;
            set => SetValue(LocalMediaFoldersKey, value, SourceGenerationContext.Default.ListLocalMediaFolder);
        }

        public TextAlignmentType LyricsAlignmentType
        {
            get => (TextAlignmentType)GetValue<int>(LyricsAlignmentTypeKey);
            set => SetValue(LyricsAlignmentTypeKey, (int)value);
        }

        public TextAlignmentType SongInfoAlignmentType
        {
            get => (TextAlignmentType)GetValue<int>(SongInfoAlignmentTypeKey);
            set => SetValue(SongInfoAlignmentTypeKey, (int)value);
        }

        public int LyricsBlurAmount
        {
            get => GetValue<int>(LyricsBlurAmountKey);
            set => SetValue(LyricsBlurAmountKey, value);
        }

        public Color LyricsCustomBgFontColor
        {
            get => GetValue<int>(LyricsCustomBgFontColorKey)!.ToColor();
            set => SetValue(LyricsCustomBgFontColorKey, value.ToInt());
        }

        public Color LyricsCustomFgFontColor
        {
            get => GetValue<int>(LyricsCustomFgFontColorKey)!.ToColor();
            set => SetValue(LyricsCustomFgFontColorKey, value.ToInt());
        }

        public Color LyricsCustomStrokeFontColor
        {
            get => GetValue<int>(LyricsCustomStrokeFontColorKey)!.ToColor();
            set => SetValue(LyricsCustomStrokeFontColorKey, value.ToInt());
        }

        public LyricsFontColorType LyricsBgFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsBgFontColorTypeKey);
            set => SetValue(LyricsBgFontColorTypeKey, (int)value);
        }

        public LyricsFontColorType LyricsFgFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsFgFontColorTypeKey);
            set => SetValue(LyricsFgFontColorTypeKey, (int)value);
        }

        public LyricsFontColorType LyricsStrokeFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsStrokeFontColorTypeKey);
            set => SetValue(LyricsStrokeFontColorTypeKey, (int)value);
        }

        public int LyricsFontStrokeWidth
        {
            get => GetValue<int>(LyricsFontStrokeWidthKey);
            set => SetValue(LyricsFontStrokeWidthKey, value);
        }

        public int LyricsStandardFontSize
        {
            get => GetValue<int>(LyricsStandardFontSizeKey);
            set => SetValue(LyricsStandardFontSizeKey, value);
        }

        public int LyricsDockFontSize
        {
            get => GetValue<int>(LyricsDockFontSizeKey);
            set => SetValue(LyricsDockFontSizeKey, value);
        }

        public int LyricsDesktopFontSize
        {
            get => GetValue<int>(LyricsDesktopFontSizeKey);
            set => SetValue(LyricsDesktopFontSizeKey, value);
        }

        public LyricsFontWeight LyricsFontWeight
        {
            get => (LyricsFontWeight)GetValue<int>(LyricsFontWeightKey);
            set => SetValue(LyricsFontWeightKey, (int)value);
        }

        public LineRenderingType LyricsGlowEffectScope
        {
            get => (LineRenderingType)GetValue<int>(LyricsGlowEffectScopeKey);
            set => SetValue(LyricsGlowEffectScopeKey, (int)value);
        }

        public LineRenderingType LyricsHighlightScope
        {
            get => (LineRenderingType)GetValue<int>(LyricsHighlightSopeKey);
            set => SetValue(LyricsHighlightSopeKey, (int)value);
        }

        public double LyricsLineSpacingFactor
        {
            get => GetValue<double>(LyricsLineSpacingFactorKey);
            set => SetValue(LyricsLineSpacingFactorKey, value);
        }

        public List<MediaSourceProviderInfo> MediaSourceProvidersInfo
        {
            get => GetValue(MediaSourceProvidersInfoKey, SourceGenerationContext.Default.ListMediaSourceProviderInfo)!;
            set => SetValue(MediaSourceProvidersInfoKey, value, SourceGenerationContext.Default.ListMediaSourceProviderInfo);
        }

        public int LyricsVerticalEdgeOpacity
        {
            get => GetValue<int>(LyricsVerticalEdgeOpacityKey);
            set => SetValue(LyricsVerticalEdgeOpacityKey, value);
        }

        public string LibreTranslateServer
        {
            get => GetValue<string>(LibreTranslateServerKey)!;
            set => SetValue(LibreTranslateServerKey, value);
        }

        public bool IsTranslationEnabled
        {
            get => GetValue<bool>(IsTranslationEnabledKey);
            set => SetValue(IsTranslationEnabledKey, value);
        }

        public bool IsLibreTranslateEnabled
        {
            get => GetValue<bool>(IsLibreTranslateEnabledKey);
            set => SetValue(IsLibreTranslateEnabledKey, value);
        }

        public int SelectedTargetLanguageIndex
        {
            get => GetValue<int>(SelectedTargetLanguageIndexKey);
            set => SetValue(SelectedTargetLanguageIndexKey, value);
        }

        public string LXMusicServer
        {
            get => GetValue<string>(LXMusicServerKey)!;
            set => SetValue(LXMusicServerKey, value);
        }

        public bool IgnoreFullscreenWindow
        {
            get => GetValue<bool>(IgnoreFullscreenWindowKey);
            set => SetValue(IgnoreFullscreenWindowKey, value);
        }

        public bool IsLyricsFloatAnimationEnabled
        {
            get => GetValue<bool>(IsLyricsFloatAnimationEnabledKey);
            set => SetValue(IsLyricsFloatAnimationEnabledKey, value);
        }

        public PlaybackOrder PlaybackOrder
        {
            get => (PlaybackOrder)GetValue<int>(PlaybackOrderKey);
            set => SetValue(PlaybackOrderKey, (int)value);
        }

        public int PositionOffset
        {
            get => GetValue<int>(PositionOffsetKey);
            set => SetValue(PositionOffsetKey, value);
        }

        public bool IsImmersiveMode
        {
            get => GetValue<bool>(IsImmersiveModeKey);
            set => SetValue(IsImmersiveModeKey, value);
        }

        public string DockMonitorDeviceName
        {
            get => GetValue<string>(DockMonitorDeviceNameKey)!;
            set => SetValue(DockMonitorDeviceNameKey, value);
        }

        // LastFM

        public string LastFMSessionKey
        {
            get => GetValue<string>(LastFMSessionKeyKey)!;
            set => SetValue(LastFMSessionKeyKey, value);
        }

        public string LyricsTranslationSeparator
        {
            get => GetValue<string>(LyricsTranslationSeparatorKey)!;
            set => SetValue(LyricsTranslationSeparatorKey, value);
        }

        /// <summary>
        /// Container 方式取出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private T? GetValue<T>(string key)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                if (value is float)
                {
                    value = Convert.ToDouble(value);
                }
                return (T)value;
            }
            return default;
        }

        /// <summary>
        /// JSON 方式取出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="jsonTypeInfo"></param>
        /// <returns></returns>
        private T? GetValue<T>(string key, JsonTypeInfo<T> jsonTypeInfo)
        {
            string targetPath = Path.Combine(PathHelper.SettingsDirectory, key);
            if (File.Exists(targetPath))
            {
                string json = File.ReadAllText(targetPath);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, jsonTypeInfo);
            }
            return default;
        }

        private void SetDefault<T>(string key, T value)
        {
            if (_localSettings.Values.ContainsKey(key) && _localSettings.Values[key] is T)
                return;
            SetValue(key, value);
        }

        private void SetDefault<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo)
        {
            string targetPath = Path.Combine(PathHelper.SettingsDirectory, key);
            if (File.Exists(targetPath))
                return;
            SetValue(key, value, jsonTypeInfo);
        }

        private void SetValue<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }

        private void SetValue<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo)
        {
            string targetPath = Path.Combine(PathHelper.SettingsDirectory, key);
            string json = System.Text.Json.JsonSerializer.Serialize(value, jsonTypeInfo);
            File.WriteAllText(targetPath, json);
        }

        /// <summary>
        /// Export settings to specific folder
        /// </summary>
        /// <param name="exportPath">Target folder path (not file path)</param>
        public void ExportSettings(string exportPath)
        {
            // 收集 LocalSettings
            var exportData = new Dictionary<string, object>();
            foreach (var kvp in _localSettings.Values)
            {
                exportData[kvp.Key] = kvp.Value;
            }

            // 收集 SettingsDirectory 下的 JSON 文件
            var settingsFiles = Directory.GetFiles(PathHelper.SettingsDirectory, "*.json");
            foreach (var file in settingsFiles)
            {
                var key = Path.GetFileName(file);
                var json = File.ReadAllText(file);
                exportData[key] = json;
            }

            // 导出到文件
            var exportJson = System.Text.Json.JsonSerializer.Serialize(exportData, SourceGenerationContext.Default.DictionaryStringObject);
            File.WriteAllText(Path.Combine(exportPath, $"BetterLyrics_Settings_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json"), exportJson);
        }

        /// <summary>
        /// Indicate a value whether import action is successfullt done
        /// </summary>
        /// <param name="importPath"></param>
        /// <returns></returns>
        public bool ImportSettings(string importPath)
        {
            // TODO 导入有问题
            if (!File.Exists(importPath))
                return false;

            var importJson = File.ReadAllText(importPath);
            var importData = System.Text.Json.JsonSerializer.Deserialize(importJson, SourceGenerationContext.Default.DictionaryStringObject);

            if (importData == null)
                return false;

            foreach (var kvp in importData)
            {
                // 如果是 JSON 文件，则写入 SettingsDirectory
                if (kvp.Key.EndsWith(".json"))
                {
                    var filePath = Path.Combine(PathHelper.SettingsDirectory, kvp.Key);
                    File.WriteAllText(filePath, kvp.Value?.ToString() ?? "");
                }
                else
                {
                    // 其他设置写入 LocalSettings
                    SetValue(kvp.Key, kvp.Value);
                }
                return true;
            }
            return true;
        }

    }
}
