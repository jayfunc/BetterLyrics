using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.ViewModels.Lyrics
{
    public interface ILyricsViewModel
    {
        ObservableCollection<Windows.UI.Color> CoverImageDominantColors { get; set; }
        LyricsAlignmentType LyricsAlignmentType { get; set; }
        int LyricsBlurAmount { get; set; }
        int LyricsVerticalEdgeOpacity { get; set; }
        float LyricsLineSpacingFactor { get; set; }
        int LyricsFontSize { get; set; }
        bool IsLyricsGlowEffectEnabled { get; set; }
        bool IsLyricsDynamicGlowEffectEnabled { get; set; }
        LyricsFontColorType LyricsFontColorType { get; set; }
        int LyricsFontSelectedAccentColorIndex { get; set; }
    }
}
