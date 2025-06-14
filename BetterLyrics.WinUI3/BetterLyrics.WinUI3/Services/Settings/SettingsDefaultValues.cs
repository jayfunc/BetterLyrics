namespace BetterLyrics.WinUI3.Services.Settings
{
    public static class SettingsDefaultValues
    {
        public const bool IsFirstRun = true;

        // Theme
        public const int ThemeType = 0; // Follow system

        // Language
        public const int Language = 0; // Default

        // Music
        public const string MusicLibraries = "[]";

        // Backdrop
        public const int BackdropType = 5; // Acrylic Base
        public const bool IsCoverOverlayEnabled = true;
        public const bool IsDynamicCoverOverlayEnabled = true;
        public const int CoverOverlayOpacity = 100; // 1.0
        public const int CoverOverlayBlurAmount = 200;

        // Title bar
        public const int TitleBarType = 0;

        // Album art
        public const int CoverImageRadius = 24;

        // Lyrics
        public const int InAppLyricsAlignmentType = 1; // Center
        public const int InAppLyricsBlurAmount = 0;
        public const int InAppLyricsVerticalEdgeOpacity = 0; // 0 % = 0.0
        public const float InAppLyricsLineSpacingFactor = 0.5f;
        public const int InAppLyricsFontSize = 28;
        public const bool IsInAppLyricsGlowEffectEnabled = false;
        public const bool IsInAppLyricsDynamicGlowEffectEnabled = false;
        public const int InAppLyricsFontColorType = 0; // Default
        public const int InAppLyricsFontSelectedAccentColorIndex = 0;

        public const int DesktopLyricsAlignmentType = 1; // Center
        public const int DesktopLyricsBlurAmount = 0;
        public const int DesktopLyricsVerticalEdgeOpacity = 0; // 0 % = 0.0
        public const float DesktopLyricsLineSpacingFactor = 0.5f;
        public const int DesktopLyricsFontSize = 24;
        public const bool IsDesktopLyricsGlowEffectEnabled = false;
        public const bool IsDesktopLyricsDynamicGlowEffectEnabled = false;
        public const int DesktopLyricsFontColorType = 0; // Default
        public const int DesktopLyricsFontSelectedAccentColorIndex = 0;

        // Notification
        public const bool NeverShowMessage = false;
    }
}
