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
using System.Linq;
using Windows.Storage;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services
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

        private const string IsDynamicCoverOverlayEnabledKey = "IsDynamicCoverOverlayEnabled";
        private const string IsFanLyricsEnabledKey = "IsFanLyricsEnabled";
        private const string IsFirstRunKey = "IsFirstRun";
        private const string IsLyricsGlowEffectEnabledKey = "IsLyricsGlowEffectEnabled";
        private const string LanguageKey = "Language";

        private const string LocalLyricsFoldersKey = "LocalLyricsFolders";
        private const string LyricsAlignmentTypeKey = "TextAlignmentType";
        private const string SongInfoAlignmentTypeKey = "SongInfoAlignmentType";
        private const string LyricsBlurAmountKey = "LyricsBlurAmount";

        private const string LyricsBgFontColorTypeKey = "_lyricsBgFontColorType";
        private const string LyricsFgFontColorTypeKey = "LyricsFgFontColorType";
        private const string LyricsStrokeFontColorTypeKey = "LyricsStrokeFontColorType";

        private const string LyricsFontStrokeWidthKey = "LyricsFontStrokeWidth";

        private const string LyricsFontSizeKey = "LyricsFontSize";
        private const string LyricsFontWeightKey = "LyricsFontWeightKey";
        private const string LyricsGlowEffectScopeKey = "LyricsGlowEffectScope";
        private const string LyricsHighlightSopeKey = "LyricsHighlightSope";
        private const string LyricsLineSpacingFactorKey = "LyricsLineSpacingFactor";
        private const string LyricsSearchProvidersInfoKey = "LyricsSearchProvidersInfo";
        private const string AlbumArtSearchProvidersInfoKey = "AlbumArtSearchProvidersInfo";
        private const string LyricsVerticalEdgeOpacityKey = "LyricsVerticalEdgeOpacity";

        private const string MediaSourceProvidersInfoKey = "MediaSourceProvidersInfo";

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

        public const string TimelineSyncThresholdKey = "TimelineSyncThreshold";

        private const string IsLyricsFloatAnimationEnabledKey = "IsLyricsFloatAnimationEnabled";

        private const string ResetPositionOffsetOnSongChangedKey = "ResetPositionOffsetOnSongChanged";
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

        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            SetDefault(IsFirstRunKey, true);
            // Lyrics lib
            SetDefault(LocalLyricsFoldersKey, "[]");
            SetDefault(
                LyricsSearchProvidersInfoKey,
                System.Text.Json.JsonSerializer.Serialize(
                    Enum.GetValues<LyricsSearchProvider>()
                        .Select(p => new LyricsSearchProviderInfo(p, true))
                        .ToList(),
                    SourceGenerationContext.Default.ListLyricsSearchProviderInfo
                )
            );
            if (LyricsSearchProvidersInfo.Count != Enum.GetValues<LyricsSearchProvider>().Length)
            {
                LyricsSearchProvidersInfo = Enum.GetValues<LyricsSearchProvider>()
                    .Select(p => new LyricsSearchProviderInfo(
                        p,
                        LyricsSearchProvidersInfo
                            .Where(x => x.Provider == p)
                            .FirstOrDefault()
                            ?.IsEnabled ?? true
                    ))
                    .ToList();
            }

            SetDefault(
                AlbumArtSearchProvidersInfoKey,
                System.Text.Json.JsonSerializer.Serialize(
                    Enum.GetValues<AlbumArtSearchProvider>()
                        .Select(p => new AlbumArtSearchProviderInfo(p, true))
                        .ToList(),
                    SourceGenerationContext.Default.ListAlbumArtSearchProviderInfo
                )
            );
            if (AlbumArtSearchProvidersInfo.Count != Enum.GetValues<AlbumArtSearchProvider>().Length)
            {
                AlbumArtSearchProvidersInfo = Enum.GetValues<AlbumArtSearchProvider>()
                    .Select(p => new AlbumArtSearchProviderInfo(
                        p,
                        AlbumArtSearchProvidersInfo
                            .Where(x => x.Provider == p)
                            .FirstOrDefault()
                            ?.IsEnabled ?? true
                    ))
                    .ToList();
            }

            SetDefault(MediaSourceProvidersInfoKey, "[]");

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
            SetDefault(IsDynamicCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlayOpacityKey, 100); // 100 % = 1.0
            SetDefault(CoverOverlayBlurAmountKey, 100);
            SetDefault(CoverImageRadiusKey, 12); // 12 %
            SetDefault(CoverAcrylicEffectAmountKey, 0);
            // Lyrics
            SetDefault(LyricsAlignmentTypeKey, (int)TextAlignmentType.Center);
            SetDefault(SongInfoAlignmentTypeKey, (int)TextAlignmentType.Left);
            SetDefault(LyricsFontWeightKey, (int)LyricsFontWeight.Bold);
            SetDefault(LyricsBlurAmountKey, 5);

            SetDefault(LyricsBackgroundThemeKey, (int)ElementTheme.Default);

            SetDefault(LyricsBgFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);
            SetDefault(LyricsFgFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);
            SetDefault(LyricsStrokeFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);

            SetDefault(LyricsCustomBgFontColorKey, Colors.White.ToInt());
            SetDefault(LyricsCustomFgFontColorKey, Colors.White.ToInt());
            SetDefault(LyricsCustomStrokeFontColorKey, Colors.White.ToInt());

            SetDefault(LyricsFontSizeKey, 28);
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

            SetDefault(LyricsScrollEasingTypeKey, (int)EasingType.EaseInOutQuad);
            SetDefault(LyricsScrollDurationKey, 500); // 500ms
            SetDefault(TimelineSyncThresholdKey, 0); // 0ms

            SetDefault(IsLyricsFloatAnimationEnabledKey, true);

            SetDefault(ResetPositionOffsetOnSongChangedKey, false);
            SetDefault(PositionOffsetKey, 0);
            SetDefault(LockHotKeyIndexKey, 'U' - 'A');
            SetDefault(DockPlacementKey, (int)DockPlacement.Top);
            SetDefault(LyricsBgFontOpacityKey, 30); // 30%
            SetDefault(HideWindowWhenNotPlayingKey, false);
            SetDefault(DockWindowHeightKey, 64); // 64px
            SetDefault(SelectedFontFamilyIndexKey, 0);
            SetDefault(LyricsFontFamilyKey, FontHelper.SystemFontFamilies.ElementAtOrDefault(0));
            SetDefault(IsDragEverywhereEnabledKey, false);
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

        public bool IsDynamicCoverOverlayEnabled
        {
            get => GetValue<bool>(IsDynamicCoverOverlayEnabledKey);
            set => SetValue(IsDynamicCoverOverlayEnabledKey, value);
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
            get =>
                System.Text.Json.JsonSerializer.Deserialize(
                    GetValue<string>(LocalLyricsFoldersKey) ?? "[]",
                    SourceGenerationContext.Default.ListLocalMediaFolder
                )!;
            set =>
                SetValue(
                    LocalLyricsFoldersKey,
                    System.Text.Json.JsonSerializer.Serialize(
                        value,
                        SourceGenerationContext.Default.ListLocalMediaFolder
                    )
                );
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

        public int LyricsFontSize
        {
            get => GetValue<int>(LyricsFontSizeKey);
            set => SetValue(LyricsFontSizeKey, value);
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

        public float LyricsLineSpacingFactor
        {
            get => GetValue<float>(LyricsLineSpacingFactorKey);
            set => SetValue(LyricsLineSpacingFactorKey, value);
        }

        public List<LyricsSearchProviderInfo> LyricsSearchProvidersInfo
        {
            get =>
                System.Text.Json.JsonSerializer.Deserialize(
                    GetValue<string>(LyricsSearchProvidersInfoKey) ?? "[]",
                    SourceGenerationContext.Default.ListLyricsSearchProviderInfo
                )!;
            set =>
                SetValue(
                    LyricsSearchProvidersInfoKey,
                    System.Text.Json.JsonSerializer.Serialize(
                        value,
                        SourceGenerationContext.Default.ListLyricsSearchProviderInfo
                    )
                );
        }

        public List<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo
        {
            get =>
                System.Text.Json.JsonSerializer.Deserialize(
                    GetValue<string>(AlbumArtSearchProvidersInfoKey) ?? "[]",
                    SourceGenerationContext.Default.ListAlbumArtSearchProviderInfo
                )!;
            set =>
                SetValue(
                    AlbumArtSearchProvidersInfoKey,
                    System.Text.Json.JsonSerializer.Serialize(
                        value,
                        SourceGenerationContext.Default.ListAlbumArtSearchProviderInfo
                    )
                );
        }

        public List<MediaSourceProviderInfo> MediaSourceProvidersInfo
        {
            get =>
                System.Text.Json.JsonSerializer.Deserialize(
                    GetValue<string>(MediaSourceProvidersInfoKey) ?? "[]",
                    SourceGenerationContext.Default.ListMediaSourceProviderInfo
                )!;
            set =>
                SetValue(
                    MediaSourceProvidersInfoKey,
                    System.Text.Json.JsonSerializer.Serialize(
                        value,
                        SourceGenerationContext.Default.ListMediaSourceProviderInfo
                    )
                );
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

        public int TimelineSyncThreshold
        {
            get => GetValue<int>(TimelineSyncThresholdKey);
            set => SetValue(TimelineSyncThresholdKey, value);
        }

        public bool IsLyricsFloatAnimationEnabled
        {
            get => GetValue<bool>(IsLyricsFloatAnimationEnabledKey);
            set => SetValue(IsLyricsFloatAnimationEnabledKey, value);
        }

        public bool ResetPositionOffsetOnSongChanged
        {
            get => GetValue<bool>(ResetPositionOffsetOnSongChangedKey);
            set => SetValue(ResetPositionOffsetOnSongChangedKey, value);
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

        private T? GetValue<T>(string key)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)value;
            }
            return default;
        }

        private void SetDefault<T>(string key, T value)
        {
            if (_localSettings.Values.ContainsKey(key) && _localSettings.Values[key] is T)
                return;
            _localSettings.Values[key] = value;
        }

        private void SetValue<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }
    }
}
