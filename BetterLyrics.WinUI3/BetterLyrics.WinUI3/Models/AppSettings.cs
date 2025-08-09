using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public partial class AppSettings : ObservableObject
    {
        [ObservableProperty] public partial AutoStartWindowType AutoStartWindowType { get; set; } = AutoStartWindowType.StandardMode;
        [ObservableProperty] public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size
        [ObservableProperty] public partial int CoverOverlayBlurAmount { get; set; } = 100; // 100 % of the cover image size
        [ObservableProperty] public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0
        [ObservableProperty] public partial int CoverOverlaySpeed { get; set; } = 50; // 50 % of the base rotate speed
        [ObservableProperty] public partial int CoverAcrylicEffectAmount { get; set; } = 0;
        [ObservableProperty] public partial bool IsFanLyricsEnabled { get; set; } = false;
        [ObservableProperty] public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;
        [ObservableProperty] public partial Language Language { get; set; } = Language.FollowSystem;
        [ObservableProperty] public partial int DesktopWindowLeft { get; set; } = 100;
        [ObservableProperty] public partial int DesktopWindowTop { get; set; } = 100;
        [ObservableProperty] public partial int DesktopWindowWidth { get; set; } = 400;
        [ObservableProperty] public partial int DesktopWindowHeight { get; set; } = 200;
        [ObservableProperty] public partial int StandardWindowWidth { get; set; } = 1000;
        [ObservableProperty] public partial int StandardWindowHeight { get; set; } = 600;
        [ObservableProperty] public partial int StandardWindowLeft { get; set; } = 100;
        [ObservableProperty] public partial int StandardWindowTop { get; set; } = 100;
        [ObservableProperty] public partial bool AutoLockOnDesktopMode { get; set; } = false;
        [ObservableProperty] public partial string LibreTranslateServer { get; set; } = string.Empty;
        [ObservableProperty] public partial int SelectedTargetLanguageIndex { get; set; } = LanguageHelper.GetDefaultTargetLanguageIndex();
        [ObservableProperty] public partial List<LocalMediaFolder> LocalMediaFolders { get; set; } = [];
        [ObservableProperty] public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty] public partial TextAlignmentType SongInfoAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty] public partial int LyricsBlurAmount { get; set; } = 5;
        [ObservableProperty] public partial Color LyricsCustomBgFontColor { get; set; } = Colors.White;
        [ObservableProperty] public partial Color LyricsCustomFgFontColor { get; set; } = Colors.White;
        [ObservableProperty] public partial Color LyricsCustomStrokeFontColor { get; set; } = Colors.White;
        [ObservableProperty] public partial int LyricsBgFontOpacity { get; set; } = 30; // 30% opacity
        [ObservableProperty] public partial LyricsFontColorType LyricsBgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty] public partial LyricsFontColorType LyricsFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty] public partial LyricsFontColorType LyricsStrokeFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty] public partial int LyricsStandardFontSize { get; set; } = 32;
        [ObservableProperty] public partial int LyricsDockFontSize { get; set; } = 16;
        [ObservableProperty] public partial int LyricsDesktopFontSize { get; set; } = 28;
        [ObservableProperty] public partial ElementTheme LyricsBackgroundTheme { get; set; } = ElementTheme.Dark;
        [ObservableProperty] public partial int LyricsFontStrokeWidth { get; set; } = 2;
        [ObservableProperty] public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;
        [ObservableProperty] public partial LineRenderingType LyricsGlowEffectScope { get; set; } = LineRenderingType.CurrentChar;
        [ObservableProperty] public partial LineRenderingType LyricsHighlightScope { get; set; } = LineRenderingType.LineStartToCurrentChar;
        [ObservableProperty] public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;
        [ObservableProperty] public partial double LyricsLineSpacingFactor { get; set; } = 0.5;
        [ObservableProperty] public partial List<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; } = [];
        [ObservableProperty] public partial EasingType LyricsScrollEasingType { get; set; } = EasingType.EaseInOutSine;
        [ObservableProperty] public partial int LyricsScrollDuration { get; set; } = 500; // 500 ms
        [ObservableProperty] public partial int LyricsScrollTopDuration { get; set; } = 100; // 100 ms
        [ObservableProperty] public partial int LyricsScrollBottomDuration { get; set; } = 1000; //  1000 ms
        [ObservableProperty] public partial int LyricsVerticalEdgeOpacity { get; set; } = 0; // 0% opacity
        [ObservableProperty] public partial bool IgnoreFullscreenWindow { get; set; } = false;
        [ObservableProperty] public partial bool IsTranslationEnabled { get; set; } = true;
        [ObservableProperty] public partial bool ShowTranslationOnly { get; set; } = false;
        [ObservableProperty] public partial LyricsDisplayType DisplayType { get; set; } = LyricsDisplayType.SplitView;
        [ObservableProperty] public partial int LockHotKeyIndex { get; set; } = 'U' - 'A'; // Default to 'U' key
        [ObservableProperty] public partial bool IsImmersiveMode { get; set; } = false;
        [ObservableProperty] public partial string LXMusicServer { get; set; } = string.Empty;
        [ObservableProperty] public partial DockPlacement DockPlacement { get; set; } = DockPlacement.Top;
        [ObservableProperty] public partial bool HideWindowWhenNotPlaying { get; set; } = true;
        [ObservableProperty] public partial int DockWindowHeight { get; set; } = 64;
        [ObservableProperty] public partial int SelectedFontFamilyIndex { get; set; } = 0;
        [ObservableProperty] public partial string LyricsFontFamily { get; set; } = FontHelper.SystemFontFamilies.FirstOrDefault() ?? "";
        [ObservableProperty] public partial bool IsDragEverywhereEnabled { get; set; } = false;
        [ObservableProperty] public partial PlaybackOrder PlaybackOrder { get; set; } = PlaybackOrder.RepeatAll;
        [ObservableProperty] public partial bool IsLibreTranslateEnabled { get; set; } = false;
        [ObservableProperty] public partial string DockMonitorDeviceName { get; set; } = MonitorHelper.GetPrimaryMonitorDeviceName();
        [ObservableProperty] public partial string LastFMSessionKey { get; set; } = string.Empty;
        [ObservableProperty] public partial string LyricsTranslationSeparator { get; set; } = StringHelper.NewLine;
    }
}
