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
        public const string LyricsAlignmentType = "LyricsAlignmentType";
        public const string LyricsBlurAmount = "LyricsBlurAmount";
        public const string LyricsVerticalEdgeOpacity = "LyricsVerticalEdgeOpacity";
        public const string LyricsLineSpacingFactor = "LyricsLineSpacingFactor";
        public const string LyricsFontSize = "LyricsFontSize";
        public const string IsLyricsGlowEffectEnabled = "IsLyricsGlowEffectEnabled";
        public const string IsLyricsDynamicGlowEffectEnabled = "IsLyricsDynamicGlowEffectEnabled";
        public const string LyricsFontColorType = "LyricsFontColorType";
        public const string LyricsFontSelectedAccentColorIndex =
            "LyricsFontSelectedAccentColorIndex";
    }
}
