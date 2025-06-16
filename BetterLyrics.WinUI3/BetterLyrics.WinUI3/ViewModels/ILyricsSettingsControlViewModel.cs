using System.Collections.ObjectModel;
using BetterLyrics.WinUI3.Models;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public interface ILyricsSettingsControlViewModel
    {
        ObservableCollection<Color> CoverImageDominantColors { get; set; }
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
