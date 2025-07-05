// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services
{
    public class SettingsService : ISettingsService
    {
        public const string LyricsCustomFontColorKey = "LyricsCustomFontColor";

        // App behavior

        private const string AutoStartWindowTypeKey = "AutoStartWindowType";

        private const string CoverImageRadiusKey = "AlbumArtCornerRadius";
        private const string CoverOverlayBlurAmountKey = "CoverOverlayBlurAmount";
        private const string CoverOverlayOpacityKey = "CoverOverlayOpacity";
        private const string IsCoverOverlayEnabledKey = "IsCoverOverlayEnabled";

        private const string DesktopWindowLeftKey = "DesktopWindowLeft";
        private const string DesktopWindowTopKey = "DesktopWindowTop";
        private const string DesktopWindowWidthKey = "DesktopWindowWidth";
        private const string DesktopWindowHeightKey = "DesktopWindowHeight";

        private const string StandardWindowLeftKey = "StandardWindowLeft";
        private const string StandardWindowTopKey = "StandardWindowTop";
        private const string StandardWindowWidthKey = "StandardWindowWidth";
        private const string StandardWindowHeightKey = "StandardWindowHeight";

        private const string AutoLockOnDesktopModeKey = "AutoLockOnDesktopMode";

        private const string IsDynamicCoverOverlayEnabledKey = "IsDynamicCoverOverlayEnabled";
        private const string IsFanLyricsEnabledKey = "IsFanLyricsEnabled";
        private const string IsFirstRunKey = "IsFirstRun";
        private const string IsLyricsGlowEffectEnabledKey = "IsLyricsGlowEffectEnabled";
        private const string LanguageKey = "Language";

        private const string LocalLyricsFoldersKey = "LocalLyricsFolders";
        private const string LyricsAlignmentTypeKey = "TextAlignmentType";
        private const string SongInfoAlignmentTypeKey = "SongInfoAlignmentType";
        private const string LyricsBlurAmountKey = "LyricsBlurAmount";
        private const string LyricsFontColorTypeKey = "LyricsFontColorType";
        private const string LyricsFontSizeKey = "LyricsFontSize";
        private const string LyricsFontWeightKey = "LyricsFontWeightKey";
        private const string LyricsGlowEffectScopeKey = "LyricsGlowEffectScope";
        private const string LyricsLineSpacingFactorKey = "LyricsLineSpacingFactor";
        private const string LyricsSearchProvidersInfoKey = "LyricsSearchProvidersInfo";
        private const string LyricsVerticalEdgeOpacityKey = "LyricsVerticalEdgeOpacity";

        private const string MediaSourceProvidersInfoKey = "MediaSourceProvidersInfo";

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
            SetDefault(MediaSourceProvidersInfoKey, "[]");
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
            // App appearance
            SetDefault(LanguageKey, (int)Language.FollowSystem);

            SetDefault(DesktopWindowHeightKey, 500);
            SetDefault(DesktopWindowLeftKey, 200);
            SetDefault(DesktopWindowTopKey, 200);
            SetDefault(DesktopWindowWidthKey, 800);

            SetDefault(StandardWindowHeightKey, 800);
            SetDefault(StandardWindowLeftKey, 200);
            SetDefault(StandardWindowTopKey, 200);
            SetDefault(StandardWindowWidthKey, 1000);

            SetDefault(AutoLockOnDesktopModeKey, false);
            // App behavior
            SetDefault(AutoStartWindowTypeKey, (int)AutoStartWindowType.StandardMode);
            // Album art
            SetDefault(IsCoverOverlayEnabledKey, true);
            SetDefault(IsDynamicCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlayOpacityKey, 75); // 100 % = 1.0
            SetDefault(CoverOverlayBlurAmountKey, 200);
            SetDefault(CoverImageRadiusKey, 24); // 24 %
            // Lyrics
            SetDefault(LyricsAlignmentTypeKey, (int)TextAlignmentType.Center);
            SetDefault(SongInfoAlignmentTypeKey, (int)TextAlignmentType.Left);
            SetDefault(LyricsFontWeightKey, (int)LyricsFontWeight.Bold);
            SetDefault(LyricsBlurAmountKey, 5);
            SetDefault(LyricsFontColorTypeKey, (int)LyricsFontColorType.AdaptiveGrayed);
            SetDefault(LyricsCustomFontColorKey, Colors.White.ToInt());
            SetDefault(LyricsFontSizeKey, 28);
            SetDefault(LyricsLineSpacingFactorKey, 0.5f);
            SetDefault(LyricsVerticalEdgeOpacityKey, 0);
            SetDefault(IsLyricsGlowEffectEnabledKey, true);
            SetDefault(LyricsGlowEffectScopeKey, (int)LineRenderingType.CurrentCharOnly);
            SetDefault(IsFanLyricsEnabledKey, false);
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

        public List<LocalLyricsFolder> LocalLyricsFolders
        {
            get =>
                System.Text.Json.JsonSerializer.Deserialize(
                    GetValue<string>(LocalLyricsFoldersKey) ?? "[]",
                    SourceGenerationContext.Default.ListLocalLyricsFolder
                )!;
            set =>
                SetValue(
                    LocalLyricsFoldersKey,
                    System.Text.Json.JsonSerializer.Serialize(
                        value,
                        SourceGenerationContext.Default.ListLocalLyricsFolder
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

        public Color LyricsCustomFontColor
        {
            get => GetValue<int>(LyricsCustomFontColorKey)!.ToColor();
            set => SetValue(LyricsCustomFontColorKey, value.ToInt());
        }

        public LyricsFontColorType LyricsFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsFontColorTypeKey);
            set => SetValue(LyricsFontColorTypeKey, (int)value);
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
