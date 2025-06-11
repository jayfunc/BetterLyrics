using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;

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
        public const bool IsDynamicCoverOverlay = true;
        public const int CoverOverlayOpacity = 100; // 1.0
        public const int CoverOverlayBlurAmount = 200;

        // Title bar
        public const int TitleBarType = 0;

        // Album art
        public const int CoverImageRadius = 24;

        // Lyrics
        public const int LyricsAlignmentType = 1; // Center
        public const int LyricsBlurAmount = 0;
        public const int LyricsVerticalEdgeOpacity = 0; // 0.0
        public const float LyricsLineSpacingFactor = 0.5f;
        public const int LyricsFontSize = 28;
        public const bool IsLyricsGlowEffectEnabled = false;
        public const bool IsLyricsDynamicGlowEffectEnabled = false;
        public const int LyricsFontColorType = 0; // Default
        public const int LyricsFontSelectedAccentColorIndex = 0;

        // Notification
        public const bool NeverShowMessage = false;
    }
}
