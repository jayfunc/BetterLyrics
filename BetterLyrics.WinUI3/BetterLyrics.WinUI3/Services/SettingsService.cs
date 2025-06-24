// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services
{
    /// <summary>
    /// Defines the <see cref="SettingsService" />
    /// </summary>
    public class SettingsService : ISettingsService
    {
        #region Constants

        // App behavior

        /// <summary>
        /// Defines the AutoStartWindowTypeKey
        /// </summary>
        private const string AutoStartWindowTypeKey = "AutoStartWindowType";

        /// <summary>
        /// Defines the BackdropTypeKey
        /// </summary>
        private const string BackdropTypeKey = "BackdropType";

        /// <summary>
        /// Defines the CoverImageRadiusKey
        /// </summary>
        private const string CoverImageRadiusKey = "CoverImageRadius";

        /// <summary>
        /// Defines the CoverOverlayBlurAmountKey
        /// </summary>
        private const string CoverOverlayBlurAmountKey = "CoverOverlayBlurAmount";

        /// <summary>
        /// Defines the CoverOverlayOpacityKey
        /// </summary>
        private const string CoverOverlayOpacityKey = "CoverOverlayOpacity";

        // Album art

        /// <summary>
        /// Defines the IsCoverOverlayEnabledKey
        /// </summary>
        private const string IsCoverOverlayEnabledKey = "IsCoverOverlayEnabled";

        /// <summary>
        /// Defines the IsDynamicCoverOverlayEnabledKey
        /// </summary>
        private const string IsDynamicCoverOverlayEnabledKey = "IsDynamicCoverOverlayEnabled";

        /// <summary>
        /// Defines the IsFirstRunKey
        /// </summary>
        private const string IsFirstRunKey = "IsFirstRun";

        /// <summary>
        /// Defines the IsLyricsGlowEffectEnabledKey
        /// </summary>
        private const string IsLyricsGlowEffectEnabledKey = "IsLyricsGlowEffectEnabled";

        /// <summary>
        /// Defines the LanguageKey
        /// </summary>
        private const string LanguageKey = "Language";

        // Lyrics lib

        /// <summary>
        /// Defines the LocalLyricsFoldersKey
        /// </summary>
        private const string LocalLyricsFoldersKey = "LocalLyricsFolders";

        /// <summary>
        /// Defines the LyricsAlignmentTypeKey
        /// </summary>
        private const string LyricsAlignmentTypeKey = "LyricsAlignmentType";

        /// <summary>
        /// Defines the LyricsBlurAmountKey
        /// </summary>
        private const string LyricsBlurAmountKey = "LyricsBlurAmount";

        /// <summary>
        /// Defines the LyricsFontColorTypeKey
        /// </summary>
        private const string LyricsFontColorTypeKey = "LyricsFontColorType";

        /// <summary>
        /// Defines the LyricsFontSizeKey
        /// </summary>
        private const string LyricsFontSizeKey = "LyricsFontSize";

        /// <summary>
        /// Defines the LyricsFontWeightKey
        /// </summary>
        private const string LyricsFontWeightKey = "LyricsFontWeightKey";

        /// <summary>
        /// Defines the LyricsGlowEffectScopeKey
        /// </summary>
        private const string LyricsGlowEffectScopeKey = "LyricsGlowEffectScope";

        /// <summary>
        /// Defines the LyricsLineSpacingFactorKey
        /// </summary>
        private const string LyricsLineSpacingFactorKey = "LyricsLineSpacingFactor";

        /// <summary>
        /// Defines the LyricsSearchProvidersInfoKey
        /// </summary>
        private const string LyricsSearchProvidersInfoKey = "LyricsSearchProvidersInfo";

        /// <summary>
        /// Defines the LyricsVerticalEdgeOpacityKey
        /// </summary>
        private const string LyricsVerticalEdgeOpacityKey = "LyricsVerticalEdgeOpacity";

        // App appearance

        /// <summary>
        /// Defines the ThemeTypeKey
        /// </summary>
        private const string ThemeTypeKey = "ThemeType";

        /// <summary>
        /// Defines the TitleBarTypeKey
        /// </summary>
        private const string TitleBarTypeKey = "TitleBarType";

        #endregion

        #region Fields

        /// <summary>
        /// Defines the _localSettings
        /// </summary>
        private readonly ApplicationDataContainer _localSettings;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsService"/> class.
        /// </summary>
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
            // App appearance
            SetDefault(ThemeTypeKey, (int)ElementTheme.Default);
            SetDefault(LanguageKey, (int)Language.FollowSystem);
            SetDefault(BackdropTypeKey, (int)BackdropType.DesktopAcrylic);
            // App behavior
            SetDefault(AutoStartWindowTypeKey, (int)AutoStartWindowType.StandardMode);
            // Album art
            SetDefault(IsCoverOverlayEnabledKey, true);
            SetDefault(IsDynamicCoverOverlayEnabledKey, true);
            SetDefault(CoverOverlayOpacityKey, 75); // 100 % = 1.0
            SetDefault(CoverOverlayBlurAmountKey, 200);
            SetDefault(TitleBarTypeKey, (int)TitleBarType.Compact);
            SetDefault(CoverImageRadiusKey, 24); // 24 %
            // Lyrics
            SetDefault(LyricsAlignmentTypeKey, (int)LyricsAlignmentType.Center);
            SetDefault(LyricsFontWeightKey, (int)LyricsFontWeight.Bold);
            SetDefault(LyricsBlurAmountKey, 5);
            SetDefault(LyricsFontColorTypeKey, (int)LyricsFontColorType.Default);
            SetDefault(LyricsFontSizeKey, 28);
            SetDefault(LyricsLineSpacingFactorKey, 0.5f);
            SetDefault(LyricsVerticalEdgeOpacityKey, 0);
            SetDefault(IsLyricsGlowEffectEnabledKey, true);
            SetDefault(LyricsGlowEffectScopeKey, (int)LineRenderingType.CurrentCharOnly);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the AutoStartWindowType
        /// </summary>
        public AutoStartWindowType AutoStartWindowType
        {
            get => (AutoStartWindowType)GetValue<int>(AutoStartWindowTypeKey);
            set => SetValue(AutoStartWindowTypeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the BackdropType
        /// </summary>
        public BackdropType BackdropType
        {
            get => (BackdropType)GetValue<int>(BackdropTypeKey);
            set => SetValue(BackdropTypeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        public int CoverImageRadius
        {
            get => GetValue<int>(CoverImageRadiusKey);
            set => SetValue(CoverImageRadiusKey, value);
        }

        /// <summary>
        /// Gets or sets the CoverOverlayBlurAmount
        /// </summary>
        public int CoverOverlayBlurAmount
        {
            get => GetValue<int>(CoverOverlayBlurAmountKey);
            set => SetValue(CoverOverlayBlurAmountKey, value);
        }

        /// <summary>
        /// Gets or sets the CoverOverlayOpacity
        /// </summary>
        public int CoverOverlayOpacity
        {
            get => GetValue<int>(CoverOverlayOpacityKey);
            set => SetValue(CoverOverlayOpacityKey, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsCoverOverlayEnabled
        /// </summary>
        public bool IsCoverOverlayEnabled
        {
            get => GetValue<bool>(IsCoverOverlayEnabledKey);
            set => SetValue(IsCoverOverlayEnabledKey, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsDynamicCoverOverlayEnabled
        /// </summary>
        public bool IsDynamicCoverOverlayEnabled
        {
            get => GetValue<bool>(IsDynamicCoverOverlayEnabledKey);
            set => SetValue(IsDynamicCoverOverlayEnabledKey, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsFirstRun
        /// </summary>
        public bool IsFirstRun
        {
            get => GetValue<bool>(IsFirstRunKey);
            set => SetValue(IsFirstRunKey, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsLyricsGlowEffectEnabled
        /// </summary>
        public bool IsLyricsGlowEffectEnabled
        {
            get => GetValue<bool>(IsLyricsGlowEffectEnabledKey);
            set => SetValue(IsLyricsGlowEffectEnabledKey, value);
        }

        /// <summary>
        /// Gets or sets the Language
        /// </summary>
        public Language Language
        {
            get => (Language)GetValue<int>(LanguageKey);
            set => SetValue(LanguageKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the LocalLyricsFolders
        /// </summary>
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

        /// <summary>
        /// Gets or sets the LyricsAlignmentType
        /// </summary>
        public LyricsAlignmentType LyricsAlignmentType
        {
            get => (LyricsAlignmentType)GetValue<int>(LyricsAlignmentTypeKey);
            set => SetValue(LyricsAlignmentTypeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the LyricsBlurAmount
        /// </summary>
        public int LyricsBlurAmount
        {
            get => GetValue<int>(LyricsBlurAmountKey);
            set => SetValue(LyricsBlurAmountKey, value);
        }

        /// <summary>
        /// Gets or sets the LyricsFontColorType
        /// </summary>
        public LyricsFontColorType LyricsFontColorType
        {
            get => (LyricsFontColorType)GetValue<int>(LyricsFontColorTypeKey);
            set => SetValue(LyricsFontColorTypeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        public int LyricsFontSize
        {
            get => GetValue<int>(LyricsFontSizeKey);
            set => SetValue(LyricsFontSizeKey, value);
        }

        /// <summary>
        /// Gets or sets the LyricsFontWeight
        /// </summary>
        public LyricsFontWeight LyricsFontWeight
        {
            get => (LyricsFontWeight)GetValue<int>(LyricsFontWeightKey);
            set => SetValue(LyricsFontWeightKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the LyricsGlowEffectScope
        /// </summary>
        public LineRenderingType LyricsGlowEffectScope
        {
            get => (LineRenderingType)GetValue<int>(LyricsGlowEffectScopeKey);
            set => SetValue(LyricsGlowEffectScopeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the LyricsLineSpacingFactor
        /// </summary>
        public float LyricsLineSpacingFactor
        {
            get => GetValue<float>(LyricsLineSpacingFactorKey);
            set => SetValue(LyricsLineSpacingFactorKey, value);
        }

        /// <summary>
        /// Gets or sets the LyricsSearchProvidersInfo
        /// </summary>
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

        /// <summary>
        /// Gets or sets the LyricsVerticalEdgeOpacity
        /// </summary>
        public int LyricsVerticalEdgeOpacity
        {
            get => GetValue<int>(LyricsVerticalEdgeOpacityKey);
            set => SetValue(LyricsVerticalEdgeOpacityKey, value);
        }

        /// <summary>
        /// Gets or sets the ThemeType
        /// </summary>
        public ElementTheme ThemeType
        {
            get => (ElementTheme)GetValue<int>(ThemeTypeKey);
            set => SetValue(ThemeTypeKey, (int)value);
        }

        /// <summary>
        /// Gets or sets the TitleBarType
        /// </summary>
        public TitleBarType TitleBarType
        {
            get => (TitleBarType)GetValue<int>(TitleBarTypeKey);
            set => SetValue(TitleBarTypeKey, (int)value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The GetValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <returns>The <see cref="T?"/></returns>
        private T? GetValue<T>(string key)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)value;
            }
            return default;
        }

        /// <summary>
        /// The SetDefault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="value">The value<see cref="T"/></param>
        private void SetDefault<T>(string key, T value)
        {
            if (_localSettings.Values.ContainsKey(key) && _localSettings.Values[key] is T)
                return;
            _localSettings.Values[key] = value;
        }

        /// <summary>
        /// The SetValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="value">The value<see cref="T"/></param>
        private void SetValue<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }

        #endregion
    }
}
