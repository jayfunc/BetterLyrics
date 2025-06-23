// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Services
{
    #region Interfaces

    /// <summary>
    /// Defines the <see cref="ISettingsService" />
    /// </summary>
    public interface ISettingsService
    {
        #region Properties

        // App behavior

        /// <summary>
        /// Gets or sets the AutoStartWindowType
        /// </summary>
        AutoStartWindowType AutoStartWindowType { get; set; }

        /// <summary>
        /// Gets or sets the BackdropType
        /// </summary>
        BackdropType BackdropType { get; set; }

        // Album art cover style

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        int CoverImageRadius { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayBlurAmount
        /// </summary>
        int CoverOverlayBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayOpacity
        /// </summary>
        int CoverOverlayOpacity { get; set; }

        // Album art background

        /// <summary>
        /// Gets or sets a value indicating whether IsCoverOverlayEnabled
        /// </summary>
        bool IsCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsDynamicCoverOverlayEnabled
        /// </summary>
        bool IsDynamicCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsFirstRun
        /// </summary>
        bool IsFirstRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsLyricsGlowEffectEnabled
        /// </summary>
        bool IsLyricsGlowEffectEnabled { get; set; }

        /// <summary>
        /// Gets or sets the Language
        /// </summary>
        Language Language { get; set; }

        // Lyrics lib

        /// <summary>
        /// Gets or sets the LocalLyricsFolders
        /// </summary>
        List<LocalLyricsFolder> LocalLyricsFolders { get; set; }

        // Lyrics style and effetc

        /// <summary>
        /// Gets or sets the LyricsAlignmentType
        /// </summary>
        LyricsAlignmentType LyricsAlignmentType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsBlurAmount
        /// </summary>
        int LyricsBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontColorType
        /// </summary>
        LyricsFontColorType LyricsFontColorType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        int LyricsFontSize { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontWeight
        /// </summary>
        LyricsFontWeight LyricsFontWeight { get; set; }

        /// <summary>
        /// Gets or sets the LyricsGlowEffectScope
        /// </summary>
        LyricsGlowEffectScope LyricsGlowEffectScope { get; set; }

        /// <summary>
        /// Gets or sets the LyricsLineSpacingFactor
        /// </summary>
        float LyricsLineSpacingFactor { get; set; }

        /// <summary>
        /// Gets or sets the LyricsSearchProvidersInfo
        /// </summary>
        List<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        /// <summary>
        /// Gets or sets the LyricsVerticalEdgeOpacity
        /// </summary>
        int LyricsVerticalEdgeOpacity { get; set; }

        // App appearance

        /// <summary>
        /// Gets or sets the ThemeType
        /// </summary>
        ElementTheme ThemeType { get; set; }

        /// <summary>
        /// Gets or sets the TitleBarType
        /// </summary>
        TitleBarType TitleBarType { get; set; }

        #endregion
    }

    #endregion
}
