using System.Collections.Generic;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public interface ISettingsService
    {
        bool IsFirstRun { get; set; }
        ElementTheme ThemeType { get; set; }
        Language Language { get; set; }
        List<string> MusicLibraries { get; set; }
        BackdropType BackdropType { get; set; }
        bool IsCoverOverlayEnabled { get; set; }
        bool IsDynamicCoverOverlayEnabled { get; set; }
        int CoverOverlayOpacity { get; set; }
        int CoverOverlayBlurAmount { get; set; }
        TitleBarType TitleBarType { get; set; }
        int CoverImageRadius { get; set; }
        LyricsAlignmentType InAppLyricsAlignmentType { get; set; }
        int InAppLyricsBlurAmount { get; set; }
        int InAppLyricsVerticalEdgeOpacity { get; set; }
        float InAppLyricsLineSpacingFactor { get; set; }
        int InAppLyricsFontSize { get; set; }
        bool IsInAppLyricsGlowEffectEnabled { get; set; }
        bool IsInAppLyricsDynamicGlowEffectEnabled { get; set; }
        LyricsFontColorType InAppLyricsFontColorType { get; set; }
        int InAppLyricsFontSelectedAccentColorIndex { get; set; }
        int DesktopLyricsBlurAmount { get; set; }
        int DesktopLyricsVerticalEdgeOpacity { get; set; }
        float DesktopLyricsLineSpacingFactor { get; set; }
        int DesktopLyricsFontSize { get; set; }
        bool IsDesktopLyricsGlowEffectEnabled { get; set; }
        bool IsDesktopLyricsDynamicGlowEffectEnabled { get; set; }
        LyricsAlignmentType DesktopLyricsAlignmentType { get; set; }
        LyricsFontColorType DesktopLyricsFontColorType { get; set; }
        int DesktopLyricsFontSelectedAccentColorIndex { get; set; }
    }
}
