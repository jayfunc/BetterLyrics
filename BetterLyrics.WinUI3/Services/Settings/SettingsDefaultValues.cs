using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings {
    public static class SettingsDefaultValues {
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
        public const int CoverOverlayOpacity = 50; // 0.5
        public const int CoverOverlayBlurAmount = 100;
        // Lyrics
        public const int LyricsAlignmentType = 0; // Left
        public const int LyricsBlurAmount = 0;
    }
}
