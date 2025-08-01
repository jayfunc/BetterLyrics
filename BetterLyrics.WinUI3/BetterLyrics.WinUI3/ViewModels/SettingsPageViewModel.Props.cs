using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel
    {
        public string Version { get; set; } = MetadataHelper.AppVersion;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLibreTranslateEnabled { get; set; }

        [ObservableProperty]
        public partial bool IsDragEverywhereEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial string LyricsFontFamily { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> SystemFontNames { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int SelectedFontFamilyIndex { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial DockPlacement DockPlacement { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LockHotKeyIndex { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme LyricsBackgroundTheme { get; set; }

        [ObservableProperty]
        public partial AutoStartWindowType AutoStartWindowType { get; set; }

        [ObservableProperty]
        public partial bool AutoLockOnDesktopMode { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverImageRadius { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLogEnabled { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDynamicCoverOverlayEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverAcrylicEffectAmount { get; set; }

        [ObservableProperty]
        public partial Enums.Language Language { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LocalMediaFolder> LocalMediaFolders { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsFanLyricsEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TextAlignmentType LyricsAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TextAlignmentType SongInfoAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomBgFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomFgFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomStrokeFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBgFontOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsBgFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFgFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsStrokeFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsStandardFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsDockFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsDesktopFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LineRenderingType LyricsGlowEffectScope { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LineRenderingType LyricsHighlightScope { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "App";

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool ResetPositionOffsetOnSongChanged { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsFloatAnimationEnabled { get; set; }

        [ObservableProperty]
        public partial string LibreTranslateServer { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int SelectedTargetLanguageIndex { get; set; } = 0;

        [ObservableProperty]
        public partial bool IsLibreTranslateServerTesting { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontStrokeWidth { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IgnoreFullscreenWindow { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial EasingType LyricsScrollEasingType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsScrollDuration { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int TimelineSyncThreshold { get; set; }

        [ObservableProperty]
        public partial bool IsLXMusicServerTesting { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial string LXMusicServer { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool HideWindowWhenNotPlaying { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int DockWindowHeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial string SelectedDockMonitorDeviceName { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> MonitorDeviceNames { get; set; }
    }
}
