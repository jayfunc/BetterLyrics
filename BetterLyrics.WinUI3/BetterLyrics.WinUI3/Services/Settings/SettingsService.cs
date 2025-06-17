using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BetterLyrics.WinUI3.Models;
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

        // In-app lyrics
        private const string InAppLyricsAlignmentTypeKey = "InAppLyricsAlignmentType";
        private const string InAppLyricsBlurAmountKey = "InAppLyricsBlurAmount";
        private const string InAppLyricsVerticalEdgeOpacityKey = "InAppLyricsVerticalEdgeOpacity";
        private const string InAppLyricsLineSpacingFactorKey = "InAppLyricsLineSpacingFactor";
        private const string InAppLyricsFontSizeKey = "InAppLyricsFontSize";
        private const string IsInAppLyricsGlowEffectEnabledKey = "IsInAppLyricsGlowEffectEnabled";
        private const string IsInAppLyricsDynamicGlowEffectEnabledKey =
            "IsInAppLyricsDynamicGlowEffectEnabled";
        private const string InAppLyricsFontColorTypeKey = "InAppLyricsFontColorType";

        // Desktop lyrics
        private const string DesktopLyricsAlignmentTypeKey = "DesktopLyricsAlignmentType";
        private const string DesktopLyricsBlurAmountKey = "DesktopLyricsBlurAmount";
        private const string DesktopLyricsVerticalEdgeOpacityKey =
            "DesktopLyricsVerticalEdgeOpacity";
        private const string DesktopLyricsLineSpacingFactorKey = "DesktopLyricsLineSpacingFactor";
        private const string DesktopLyricsFontSizeKey = "DesktopLyricsFontSize";
        private const string IsDesktopLyricsGlowEffectEnabledKey =
            "IsDesktopLyricsGlowEffectEnabled";
        private const string IsDesktopLyricsDynamicGlowEffectEnabledKey =
            "IsDesktopLyricsDynamicGlowEffectEnabled";
        private const string DesktopLyricsFontColorTypeKey = "DesktopLyricsFontColorType";

        // Notification
        private const string NeverShowEnterFullScreenMessageKey = "NeverShowEnterFullScreenMessage";
        private const string NeverShowEnterImmersiveModeMessageKey =
            "NeverShowEnterImmersiveModeMessage";

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

        public LyricsAlignmentType InAppLyricsAlignmentType
        {
            get => (LyricsAlignmentType)GetValue<int>(InAppLyricsAlignmentTypeKey);
            set => SetValue(InAppLyricsAlignmentTypeKey, (int)value);
        }

        public LyricsAlignmentType DesktopLyricsAlignmentType
        {
            get => (LyricsAlignmentType)GetValue<int>(DesktopLyricsAlignmentTypeKey);
            set => SetValue(DesktopLyricsAlignmentTypeKey, (int)value);
        }

        public int InAppLyricsBlurAmount
        {
            get => GetValue<int>(InAppLyricsBlurAmountKey);
            set => SetValue(InAppLyricsBlurAmountKey, value);
        }

        public int DesktopLyricsBlurAmount
        {
            get => GetValue<int>(DesktopLyricsBlurAmountKey);
            set => SetValue(DesktopLyricsBlurAmountKey, value);
        }

        public int InAppLyricsVerticalEdgeOpacity
        {
            get => GetValue<int>(InAppLyricsVerticalEdgeOpacityKey);
            set => SetValue(InAppLyricsVerticalEdgeOpacityKey, value);
        }

        public int DesktopLyricsVerticalEdgeOpacity
        {
            get => GetValue<int>(DesktopLyricsVerticalEdgeOpacityKey);
            set => SetValue(DesktopLyricsVerticalEdgeOpacityKey, value);
        }

        public float InAppLyricsLineSpacingFactor
        {
            get => GetValue<float>(InAppLyricsLineSpacingFactorKey);
            set => SetValue(InAppLyricsLineSpacingFactorKey, value);
        }

        public float DesktopLyricsLineSpacingFactor
        {
            get => GetValue<float>(DesktopLyricsLineSpacingFactorKey);
            set => SetValue(DesktopLyricsLineSpacingFactorKey, value);
        }

        public int InAppLyricsFontSize
        {
            get => GetValue<int>(InAppLyricsFontSizeKey);
            set => SetValue(InAppLyricsFontSizeKey, value);
        }

        public int DesktopLyricsFontSize
        {
            get => GetValue<int>(DesktopLyricsFontSizeKey);
            set => SetValue(DesktopLyricsFontSizeKey, value);
        }

        public bool IsInAppLyricsGlowEffectEnabled
        {
            get => GetValue<bool>(IsInAppLyricsGlowEffectEnabledKey);
            set => SetValue(IsInAppLyricsGlowEffectEnabledKey, value);
        }

        public bool IsDesktopLyricsGlowEffectEnabled
        {
            get => GetValue<bool>(IsDesktopLyricsGlowEffectEnabledKey);
            set => SetValue(IsDesktopLyricsGlowEffectEnabledKey, value);
        }

        public bool IsInAppLyricsDynamicGlowEffectEnabled
        {
            get => GetValue<bool>(IsInAppLyricsDynamicGlowEffectEnabledKey);
            set => SetValue(IsInAppLyricsDynamicGlowEffectEnabledKey, value);
        }

        public bool IsDesktopLyricsDynamicGlowEffectEnabled
        {
            get => GetValue<bool>(IsDesktopLyricsDynamicGlowEffectEnabledKey);
            set => SetValue(IsDesktopLyricsDynamicGlowEffectEnabledKey, value);
        }

        public LyricsFontColorType InAppLyricsFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(InAppLyricsFontColorTypeKey);
            set => SetValue(InAppLyricsFontColorTypeKey, (int)value);
        }

        public LyricsFontColorType DesktopLyricsFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(DesktopLyricsFontColorTypeKey);
            set => SetValue(DesktopLyricsFontColorTypeKey, (int)value);
        }

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            SetDefault(IsFirstRunKey, true);
            // App appearance
            SetDefault(ThemeTypeKey, (int)ElementTheme.Default);
            SetDefault(LanguageKey, (int)Language.FollowSystem);
            SetDefault(MusicLibrariesKey, "[]");
            SetDefault(BackdropTypeKey, (int)Models.BackdropType.DesktopAcrylic);
            // App behavior
            SetDefault(AutoStartWindowTypeKey, (int)AutoStartWindowType.InAppLyrics);
            // Album art
            SetDefault(IsCoverOverlayEnabledKey, true);
            SetDefault(IsDynamicCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlayOpacityKey, 100); // 100 % = 1.1
            SetDefault(CoverOverlayBlurAmountKey, 200);
            SetDefault(TitleBarTypeKey, (int)TitleBarType.Compact);
            SetDefault(CoverImageRadiusKey, 24); // 24 %
            // Lyrics
            SetDefault(InAppLyricsAlignmentTypeKey, (int)LyricsAlignmentType.Center);
            SetDefault(DesktopLyricsAlignmentTypeKey, (int)LyricsAlignmentType.Center);
            SetDefault(InAppLyricsBlurAmountKey, 0);
            SetDefault(DesktopLyricsBlurAmountKey, 0);
            SetDefault(InAppLyricsFontColorTypeKey, (int)LyricsFontColorType.Default);
            SetDefault(DesktopLyricsFontColorTypeKey, (int)LyricsFontColorType.Default);
            SetDefault(InAppLyricsFontSizeKey, 28);
            SetDefault(DesktopLyricsFontSizeKey, 16);
            SetDefault(InAppLyricsLineSpacingFactorKey, 0.5f);
            SetDefault(DesktopLyricsLineSpacingFactorKey, 0.5f);
            SetDefault(InAppLyricsVerticalEdgeOpacityKey, 0);
            SetDefault(DesktopLyricsVerticalEdgeOpacityKey, 100);
            SetDefault(IsInAppLyricsDynamicGlowEffectEnabledKey, false);
            SetDefault(IsDesktopLyricsDynamicGlowEffectEnabledKey, false);
            SetDefault(IsInAppLyricsGlowEffectEnabledKey, false);
            SetDefault(IsDesktopLyricsGlowEffectEnabledKey, false);
            // Notification
            SetDefault(NeverShowEnterFullScreenMessageKey, false);
            SetDefault(NeverShowEnterImmersiveModeMessageKey, false);
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
