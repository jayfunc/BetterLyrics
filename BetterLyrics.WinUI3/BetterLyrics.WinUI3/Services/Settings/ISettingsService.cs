using System.Collections.Generic;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public interface ISettingsService
    {
        bool IsFirstRun { get; set; }

        // Lyrics lib
        List<string> MusicLibraries { get; set; }

        // App appearance
        ElementTheme ThemeType { get; set; }
        BackdropType BackdropType { get; set; }
        TitleBarType TitleBarType { get; set; }
        Language Language { get; set; }

        // App behavior
        AutoStartWindowType AutoStartWindowType { get; set; }

        // Album art background

        bool IsCoverOverlayEnabled { get; set; }
        bool IsDynamicCoverOverlayEnabled { get; set; }
        int CoverOverlayOpacity { get; set; }
        int CoverOverlayBlurAmount { get; set; }

        // Album art cover style
        int CoverImageRadius { get; set; }

        // Lyrics style and effetc (In-app lyics)
        LyricsAlignmentType InAppLyricsAlignmentType { get; set; }
        int InAppLyricsBlurAmount { get; set; }
        int InAppLyricsVerticalEdgeOpacity { get; set; }
        float InAppLyricsLineSpacingFactor { get; set; }
        int InAppLyricsFontSize { get; set; }
        bool IsInAppLyricsGlowEffectEnabled { get; set; }
        bool IsInAppLyricsDynamicGlowEffectEnabled { get; set; }
        LyricsFontColorType InAppLyricsFontColorType { get; set; }

        // Lyrics style and effect (Desktop lyrisc)
        int DesktopLyricsBlurAmount { get; set; }
        int DesktopLyricsVerticalEdgeOpacity { get; set; }
        float DesktopLyricsLineSpacingFactor { get; set; }
        int DesktopLyricsFontSize { get; set; }
        bool IsDesktopLyricsGlowEffectEnabled { get; set; }
        bool IsDesktopLyricsDynamicGlowEffectEnabled { get; set; }
        LyricsAlignmentType DesktopLyricsAlignmentType { get; set; }
        LyricsFontColorType DesktopLyricsFontColorType { get; set; }
    }
}
