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

        ElementTheme Theme { get; set; }
        public List<MusicFolder> MusicLibraries { get; set; }
        public Language Language { get; set; }
        public BackdropType BackdropType { get; set; }
        public bool IsCoverOverlayEnabled { get; set; }
        public bool IsDynamicCoverOverlay { get; set; }
        public int CoverOverlayOpacity { get; set; }
        public LyricsAlignmentType LyricsAlignmentType { get; set; }

    }

}
