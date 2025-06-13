using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public static class SettingsKeys
    {
        public const string IsFirstRun = "IsFirstRun";

        // Theme
        public const string ThemeType = "ThemeType";

        // Language
        public const string Language = "Language";

        // Music
        public const string MusicLibraries = "MusicLibraries";

        // Backdrop
        public const string BackdropType = "BackdropType";
        public const string IsCoverOverlayEnabled = "IsCoverOverlayEnabled";
        public const string IsDynamicCoverOverlay = "IsDynamicCoverOverlay";
        public const string CoverOverlayOpacity = "CoverOverlayOpacity";
        public const string CoverOverlayBlurAmount = "CoverOverlayBlurAmount";

        // Title bar
        public const string TitleBarType = "TitleBarType";

        // Album art
        public const string CoverImageRadius = "CoverImageRadius";

        // Lyrics
        public const string InAppLyricsAlignmentType = "InAppLyricsAlignmentType";
        public const string InAppLyricsBlurAmount = "InAppLyricsBlurAmount";
        public const string InAppLyricsVerticalEdgeOpacity = "InAppLyricsVerticalEdgeOpacity";
        public const string InAppLyricsLineSpacingFactor = "InAppLyricsLineSpacingFactor";
        public const string InAppLyricsFontSize = "InAppLyricsFontSize";
        public const string IsInAppLyricsGlowEffectEnabled = "IsInAppLyricsGlowEffectEnabled";
        public const string IsInAppLyricsDynamicGlowEffectEnabled =
            "IsInAppLyricsDynamicGlowEffectEnabled";
        public const string InAppLyricsFontColorType = "InAppLyricsFontColorType";
        public const string InAppLyricsFontSelectedAccentColorIndex =
            "InAppLyricsFontSelectedAccentColorIndex";

        public const string DesktopLyricsAlignmentType = "DesktopLyricsAlignmentType";
        public const string DesktopLyricsBlurAmount = "DesktopLyricsBlurAmount";
        public const string DesktopLyricsVerticalEdgeOpacity = "DesktopLyricsVerticalEdgeOpacity";
        public const string DesktopLyricsLineSpacingFactor = "DesktopLyricsLineSpacingFactor";
        public const string DesktopLyricsFontSize = "DesktopLyricsFontSize";
        public const string IsDesktopLyricsGlowEffectEnabled = "IsDesktopLyricsGlowEffectEnabled";
        public const string IsDesktopLyricsDynamicGlowEffectEnabled =
            "IsDesktopLyricsDynamicGlowEffectEnabled";
        public const string DesktopLyricsFontColorType = "DesktopLyricsFontColorType";
        public const string DesktopLyricsFontSelectedAccentColorIndex =
            "DesktopLyricsFontSelectedAccentColorIndex";

        // Notification
        public const string NeverShowEnterFullScreenMessage = "NeverShowEnterFullScreenMessage";
        public const string NeverShowEnterImmersiveModeMessage =
            "NeverShowEnterImmersiveModeMessage";
    }
}
