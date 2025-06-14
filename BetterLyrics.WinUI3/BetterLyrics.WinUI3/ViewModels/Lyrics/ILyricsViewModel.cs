using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.ViewModels.Lyrics
{
    public interface ILyricsViewModel
    {
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
