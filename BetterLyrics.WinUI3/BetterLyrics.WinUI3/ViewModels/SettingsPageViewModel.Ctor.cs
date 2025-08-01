using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel
    {
        public SettingsPageViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService, IPlaybackService playbackService, ITranslateService libreTranslateService) : base(settingsService)
        {
            _libWatcherService = libWatcherService;
            _playbackService = playbackService;
            _libreTranslateService = libreTranslateService;

            IsLibreTranslateEnabled = _settingsService.IsLibreTranslateEnabled;
            LibreTranslateServer = _settingsService.LibreTranslateServer;
            SelectedTargetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;

            LocalMediaFolders = [.. _settingsService.LocalMediaFolders];
            LyricsSearchProvidersInfo = [.. _settingsService.LyricsSearchProvidersInfo];
            AlbumArtSearchProvidersInfo = [.. _settingsService.AlbumArtSearchProvidersInfo];

            Language = _settingsService.Language;
            CoverImageRadius = _settingsService.CoverImageRadius;

            AutoStartWindowType = _settingsService.AutoStartWindowType;
            AutoLockOnDesktopMode = _settingsService.AutoLockOnDesktopMode;

            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            CoverAcrylicEffectAmount = _settingsService.CoverAcrylicEffectAmount;

            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            SongInfoAlignmentType = _settingsService.SongInfoAlignmentType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;

            // Font size
            LyricsStandardFontSize = _settingsService.LyricsStandardFontSize;
            LyricsDockFontSize = _settingsService.LyricsDockFontSize;
            LyricsDesktopFontSize = _settingsService.LyricsDesktopFontSize;

            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            LyricsHighlightScope = _settingsService.LyricsHighlightScope;
            IsFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;

            LyricsBgFontColorType = _settingsService.LyricsBgFontColorType;
            LyricsFgFontColorType = _settingsService.LyricsFgFontColorType;
            LyricsStrokeFontColorType = _settingsService.LyricsStrokeFontColorType;

            LyricsCustomBgFontColor = _settingsService.LyricsCustomBgFontColor;
            LyricsCustomFgFontColor = _settingsService.LyricsCustomFgFontColor;
            LyricsCustomStrokeFontColor = _settingsService.LyricsCustomStrokeFontColor;

            LyricsFontStrokeWidth = _settingsService.LyricsFontStrokeWidth;
            LyricsBackgroundTheme = _settingsService.LyricsBackgroundTheme;
            MediaSourceProvidersInfo = [.. _settingsService.MediaSourceProvidersInfo];
            IgnoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;

            LyricsScrollEasingType = _settingsService.LyricsScrollEasingType;
            LyricsScrollDuration = _settingsService.LyricsScrollDuration;
            TimelineSyncThreshold = _settingsService.TimelineSyncThreshold;

            IsLyricsFloatAnimationEnabled = _settingsService.IsLyricsFloatAnimationEnabled;
            ResetPositionOffsetOnSongChanged = _settingsService.ResetPositionOffsetOnSongChanged;
            LockHotKeyIndex = _settingsService.LockHotKeyIndex;

            LXMusicServer = _settingsService.LXMusicServer;
            DockPlacement = _settingsService.DockPlacement;
            LyricsBgFontOpacity = _settingsService.LyricsBgFontOpacity;
            HideWindowWhenNotPlaying = _settingsService.HideWindowWhenNotPlaying;
            DockWindowHeight = _settingsService.DockWindowHeight;

            SystemFontNames = [.. FontHelper.SystemFontFamilies];
            SelectedFontFamilyIndex = _settingsService.SelectedFontFamilyIndex;
            LyricsFontFamily = _settingsService.LyricsFontFamily;
            IsDragEverywhereEnabled = _settingsService.IsDragEverywhereEnabled;

            MonitorDeviceNames = [.. MonitorHelper.GetAllMonitorDeviceNames()];
            SelectedDockMonitorDeviceName = _settingsService.DockMonitorDeviceName;

            _playbackService.MediaSourceProvidersInfoChanged += PlaybackService_SessionIdsChanged;
        }
    }
}
