// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Services
{
    public interface ISettingsService
    {
        // App behavior

        AutoStartWindowType AutoStartWindowType { get; set; }

        int CoverImageRadius { get; set; }

        int CoverOverlayBlurAmount { get; set; }
        int CoverOverlayOpacity { get; set; }
        bool IsDynamicCoverOverlayEnabled { get; set; }
        bool IsFanLyricsEnabled { get; set; }
        bool IsFirstRun { get; set; }
        bool IsLyricsGlowEffectEnabled { get; set; }
        Language Language { get; set; }
        int DesktopWindowLeft { get; set; }
        int DesktopWindowTop { get; set; }
        int DesktopWindowWidth { get; set; }
        int DesktopWindowHeight { get; set; }

        int StandardWindowWidth { get; set; }
        int StandardWindowHeight { get; set; }
        int StandardWindowLeft { get; set; }
        int StandardWindowTop { get; set; }

        bool AutoLockOnDesktopMode { get; set; }

        // Lyrics lib

        List<LocalLyricsFolder> LocalLyricsFolders { get; set; }

        // Lyrics style and effetc

        TextAlignmentType LyricsAlignmentType { get; set; }
        TextAlignmentType SongInfoAlignmentType { get; set; }

        int LyricsBlurAmount { get; set; }

        Color LyricsCustomFontColor { get; set; }

        LyricsFontColorType LyricsFontColorType { get; set; }

        int LyricsFontSize { get; set; }

        LyricsFontWeight LyricsFontWeight { get; set; }

        LineRenderingType LyricsGlowEffectScope { get; set; }

        float LyricsLineSpacingFactor { get; set; }

        List<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        List<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; }

        int LyricsVerticalEdgeOpacity { get; set; }
    }
}
