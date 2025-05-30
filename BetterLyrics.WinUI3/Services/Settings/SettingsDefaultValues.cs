using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings {
    public static class SettingsDefaultValues {
        public const int ThemeType = 0; // Follow system
        public const int Language = 0; // Default
        public const string MusicLibraries = "[]";
        public const int BackdropType = 5; // Acrylic Base
        public const bool IsCoverOverlayEnabled = true;
        public const bool IsDynamicCoverOverlay = true;
        public const int CoverOverlayOpacity = 50; // 0.5
        public const int LyricsAlignmentType = 0; // Left
    }
}
