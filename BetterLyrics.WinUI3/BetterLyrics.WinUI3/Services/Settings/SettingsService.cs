using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Windows.Media;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDataContainer _localSettings;

        private const string IsFirstRunKey = "IsFirstRun";

        // App appearance
        private const string ThemeTypeKey = "ThemeType";
        private const string LanguageKey = "Language";
        private const string MusicLibrariesKey = "MusicLibraries";
        private const string BackdropTypeKey = "BackdropType";

        // App behavior
        private const string AutoStartWindowTypeKey = "AutoStartWindowType";

        // Album art
        private const string IsCoverOverlayEnabledKey = "IsCoverOverlayEnabled";
        private const string IsDynamicCoverOverlayEnabledKey = "IsDynamicCoverOverlayEnabled";
        private const string CoverOverlayOpacityKey = "CoverOverlayOpacity";
        private const string CoverOverlayBlurAmountKey = "CoverOverlayBlurAmount";
        private const string TitleBarTypeKey = "TitleBarType";
        private const string CoverImageRadiusKey = "CoverImageRadius";

        private const string LyricsAlignmentTypeKey = "LyricsAlignmentType";
        private const string LyricsFontWeightKey = "LyricsFontWeightKey";
        private const string LyricsBlurAmountKey = "LyricsBlurAmount";
        private const string LyricsVerticalEdgeOpacityKey = "LyricsVerticalEdgeOpacity";
        private const string LyricsLineSpacingFactorKey = "LyricsLineSpacingFactor";
        private const string LyricsFontSizeKey = "LyricsFontSize";
        private const string IsLyricsGlowEffectEnabledKey = "IsLyricsGlowEffectEnabled";
        private const string LyricsFontColorTypeKey = "LyricsFontColorType";
        private const string LyricsGlowEffectScopeKey = "LyricsGlowEffectScope";

        public bool IsFirstRun
        {
            get => GetValue<bool>(IsFirstRunKey);
            set => SetValue(IsFirstRunKey, value);
        }

        public ElementTheme ThemeType
        {
            get => (ElementTheme)GetValue<int>(ThemeTypeKey);
            set => SetValue(ThemeTypeKey, (int)value);
        }

        public Language Language
        {
            get => (Language)GetValue<int>(LanguageKey);
            set => SetValue(LanguageKey, (int)value);
        }

        public BackdropType BackdropType
        {
            get => (BackdropType)GetValue<int>(BackdropTypeKey);
            set => SetValue(BackdropTypeKey, (int)value);
        }

        public AutoStartWindowType AutoStartWindowType
        {
            get => (AutoStartWindowType)GetValue<int>(AutoStartWindowTypeKey);
            set => SetValue(AutoStartWindowTypeKey, (int)value);
        }

        public List<string> MusicLibraries
        {
            get =>
                JsonConvert.DeserializeObject<List<string>>(
                    GetValue<string>(MusicLibrariesKey) ?? "[]"
                )!;
            set => SetValue(MusicLibrariesKey, JsonConvert.SerializeObject(value));
        }

        public bool IsCoverOverlayEnabled
        {
            get => GetValue<bool>(IsCoverOverlayEnabledKey);
            set => SetValue(IsCoverOverlayEnabledKey, value);
        }

        public bool IsDynamicCoverOverlayEnabled
        {
            get => GetValue<bool>(IsDynamicCoverOverlayEnabledKey);
            set => SetValue(IsDynamicCoverOverlayEnabledKey, value);
        }

        public int CoverOverlayOpacity
        {
            get => GetValue<int>(CoverOverlayOpacityKey);
            set => SetValue(CoverOverlayOpacityKey, value);
        }

        public int CoverOverlayBlurAmount
        {
            get => GetValue<int>(CoverOverlayBlurAmountKey);
            set => SetValue(CoverOverlayBlurAmountKey, value);
        }

        public TitleBarType TitleBarType
        {
            get => (TitleBarType)GetValue<int>(TitleBarTypeKey);
            set => SetValue(TitleBarTypeKey, (int)value);
        }

        public int CoverImageRadius
        {
            get => GetValue<int>(CoverImageRadiusKey);
            set => SetValue(CoverImageRadiusKey, value);
        }

        public LyricsAlignmentType LyricsAlignmentType
        {
            get => (LyricsAlignmentType)GetValue<int>(LyricsAlignmentTypeKey);
            set => SetValue(LyricsAlignmentTypeKey, (int)value);
        }

        public LyricsFontWeight LyricsFontWeight
        {
            get => (LyricsFontWeight)GetValue<int>(LyricsFontWeightKey);
            set => SetValue(LyricsFontWeightKey, (int)value);
        }

        public int LyricsBlurAmount
        {
            get => GetValue<int>(LyricsBlurAmountKey);
            set => SetValue(LyricsBlurAmountKey, value);
        }

        public int LyricsVerticalEdgeOpacity
        {
            get => GetValue<int>(LyricsVerticalEdgeOpacityKey);
            set => SetValue(LyricsVerticalEdgeOpacityKey, value);
        }

        public float LyricsLineSpacingFactor
        {
            get => GetValue<float>(LyricsLineSpacingFactorKey);
            set => SetValue(LyricsLineSpacingFactorKey, value);
        }

        public int LyricsFontSize
        {
            get => GetValue<int>(LyricsFontSizeKey);
            set => SetValue(LyricsFontSizeKey, value);
        }

        public bool IsLyricsGlowEffectEnabled
        {
            get => GetValue<bool>(IsLyricsGlowEffectEnabledKey);
            set => SetValue(IsLyricsGlowEffectEnabledKey, value);
        }

        public LyricsGlowEffectScope LyricsGlowEffectScope
        {
            get => (LyricsGlowEffectScope)GetValue<int>(LyricsGlowEffectScopeKey);
            set => SetValue(LyricsGlowEffectScopeKey, (int)value);
        }

        public LyricsFontColorType LyricsFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsFontColorTypeKey);
            set => SetValue(LyricsFontColorTypeKey, (int)value);
        }

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            SetDefault(IsFirstRunKey, true);
            // App appearance
            SetDefault(ThemeTypeKey, (int)ElementTheme.Default);
            SetDefault(LanguageKey, (int)Language.FollowSystem);
            SetDefault(MusicLibrariesKey, "[]");
            SetDefault(BackdropTypeKey, (int)BackdropType.DesktopAcrylic);
            // App behavior
            SetDefault(AutoStartWindowTypeKey, (int)AutoStartWindowType.StandardMode);
            // Album art
            SetDefault(IsCoverOverlayEnabledKey, true);
            SetDefault(IsDynamicCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlayOpacityKey, 100); // 100 % = 1.1
            SetDefault(CoverOverlayBlurAmountKey, 200);
            SetDefault(TitleBarTypeKey, (int)TitleBarType.Compact);
            SetDefault(CoverImageRadiusKey, 24); // 24 %
            // Lyrics
            SetDefault(LyricsAlignmentTypeKey, (int)LyricsAlignmentType.Center);
            SetDefault(LyricsFontWeightKey, (int)LyricsFontWeight.Bold);
            SetDefault(LyricsBlurAmountKey, 0);
            SetDefault(LyricsFontColorTypeKey, (int)LyricsFontColorType.Default);
            SetDefault(LyricsFontSizeKey, 28);
            SetDefault(LyricsLineSpacingFactorKey, 0.5f);
            SetDefault(LyricsVerticalEdgeOpacityKey, 0);
            SetDefault(IsLyricsGlowEffectEnabledKey, true);
            SetDefault(LyricsGlowEffectScopeKey, (int)LyricsGlowEffectScope.CurrentChar);
        }

        private T? GetValue<T>(string key)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)value;
            }
            return default;
        }

        private void SetValue<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }

        private void SetDefault<T>(string key, T value)
        {
            if (_localSettings.Values.ContainsKey(key) && _localSettings.Values[key] is T)
                return;
            _localSettings.Values[key] = value;
        }
    }
}
