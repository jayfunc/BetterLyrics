// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    public interface ISettingsService
    {
        // App behavior

        AutoStartWindowType AutoStartWindowType { get; set; }

        int CoverImageRadius { get; set; }

        int CoverOverlayBlurAmount { get; set; }
        int CoverOverlayOpacity { get; set; }
        int CoverOverlaySpeed { get; set; }
        int CoverAcrylicEffectAmount { get; set; }
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

        string LibreTranslateServer { get; set; }
        int SelectedTargetLanguageIndex { get; set; }
        int PositionOffset { get; set; }
        // Lyrics lib

        List<LocalMediaFolder> LocalMediaFolders { get; set; }

        // Lyrics style and effetc

        TextAlignmentType LyricsAlignmentType { get; set; }
        TextAlignmentType SongInfoAlignmentType { get; set; }

        int LyricsBlurAmount { get; set; }

        Color LyricsCustomBgFontColor { get; set; }
        Color LyricsCustomFgFontColor { get; set; }
        Color LyricsCustomStrokeFontColor { get; set; }

        int LyricsBgFontOpacity { get; set; }

        LyricsFontColorType LyricsBgFontColorType { get; set; }
        LyricsFontColorType LyricsFgFontColorType { get; set; }
        LyricsFontColorType LyricsStrokeFontColorType { get; set; }

        int LyricsStandardFontSize { get; set; }
        int LyricsDockFontSize { get; set; }
        int LyricsDesktopFontSize { get; set; }

        ElementTheme LyricsBackgroundTheme { get; set; }

        int LyricsFontStrokeWidth { get; set; }

        LyricsFontWeight LyricsFontWeight { get; set; }

        LineRenderingType LyricsGlowEffectScope { get; set; }
        LineRenderingType LyricsHighlightScope { get; set; }

        bool IsLyricsFloatAnimationEnabled { get; set; }

        double LyricsLineSpacingFactor { get; set; }

        List<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; }

        EasingType LyricsScrollEasingType { get; set; }
        int LyricsScrollDuration { get; set; }
        int LyricsScrollTopDuration { get; set; }
        int LyricsScrollBottomDuration { get; set; }

        int LyricsVerticalEdgeOpacity { get; set; }

        bool IgnoreFullscreenWindow { get; set; }

        bool IsTranslationEnabled { get; set; }
        bool ShowTranslationOnly { get; set; }

        LyricsDisplayType DisplayType { get; set; }

        int LockHotKeyIndex { get; set; }
        bool IsImmersiveMode { get; set; }
        string LXMusicServer { get; set; }
        DockPlacement DockPlacement { get; set; }
        bool HideWindowWhenNotPlaying { get; set; }
        int DockWindowHeight { get; set; }
        int SelectedFontFamilyIndex { get; set; }
        string LyricsFontFamily { get; set; }
        bool IsDragEverywhereEnabled { get; set; }
        PlaybackOrder PlaybackOrder { get; set; }
        bool IsLibreTranslateEnabled { get; set; }
        string DockMonitorDeviceName { get; set; }

        // LastFM
        string LastFMSessionKey { get; set; }

        string LyricsTranslationSeparator { get; set; }

        bool ImportSettings(string importPath);
        void ExportSettings(string exportPath);
    }
}
