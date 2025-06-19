using System.Collections.Generic;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Services
{
    public interface ISettingsService
    {
        bool IsFirstRun { get; set; }

        // Lyrics lib
        List<string> MusicLibraries { get; set; }
        bool IsOnlineLyricsEnabled { get; set; }

        // App appearance
        ElementTheme ThemeType { get; set; }
        BackdropType BackdropType { get; set; }
        TitleBarType TitleBarType { get; set; }
        Language Language { get; set; }

        // App behavior
        AutoStartWindowType AutoStartWindowType { get; set; }

        // Album art background

        bool IsCoverOverlayEnabled { get; set; }
        bool IsDynamicCoverOverlayEnabled { get; set; }
        int CoverOverlayOpacity { get; set; }
        int CoverOverlayBlurAmount { get; set; }

        // Album art cover style
        int CoverImageRadius { get; set; }

        // Lyrics style and effetc
        LyricsAlignmentType LyricsAlignmentType { get; set; }
        LyricsFontWeight LyricsFontWeight { get; set; }
        int LyricsBlurAmount { get; set; }
        int LyricsVerticalEdgeOpacity { get; set; }
        float LyricsLineSpacingFactor { get; set; }
        int LyricsFontSize { get; set; }
        bool IsLyricsGlowEffectEnabled { get; set; }
        LyricsGlowEffectScope LyricsGlowEffectScope { get; set; }
        LyricsFontColorType LyricsFontColorType { get; set; }
    }
}
