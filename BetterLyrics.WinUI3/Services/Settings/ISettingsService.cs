using BetterLyrics.WinUI3.Models;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings {
    public interface ISettingsService {

        // Theme
        ElementTheme Theme { get; set; }
        
        // Music
        public List<MusicFolder> MusicLibraries { get; set; }
        
        // Language
        public Language Language { get; set; }
        
        // Backdrop
        public BackdropType BackdropType { get; set; }
        public bool IsCoverOverlayEnabled { get; set; }
        public bool IsDynamicCoverOverlay { get; set; }
        public int CoverOverlayOpacity { get; set; }
        public int CoverOverlayBlurAmount { get; set; }

        // Lyrics
        public LyricsAlignmentType LyricsAlignmentType { get; set; }
        public int LyricsBlurAmount { get; set; }

    }

}
