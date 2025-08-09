using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel
{
    public partial class SettingsPageViewModel
    {
        public SettingsPageViewModel(
            ISettingsService settingsService,
            ILibWatcherService libWatcherService,
            IMediaSessionsService mediaSessionsService,
            ITranslateService libreTranslateService,
            ILastFMService lastFMService) : base(settingsService)
        {
            _libWatcherService = libWatcherService;
            _mediaSessionsService = mediaSessionsService;
            _libreTranslateService = libreTranslateService;

            // LastFM
            _lastFMService = lastFMService;
            _lastFMService.UserChanged += LastFMService_UserChanged;
            _lastFMService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;
            IsLastFMAuthenticated = _lastFMService.IsAuthenticated;
            LastFMUser = _lastFMService.User;

            IsLibreTranslateEnabled = _settingsService.AppSettings.IsLibreTranslateEnabled;
            LibreTranslateServer = _settingsService.AppSettings.LibreTranslateServer;
            SelectedTargetLanguageIndex = _settingsService.AppSettings.SelectedTargetLanguageIndex;

            LocalMediaFolders = [.. _settingsService.AppSettings.LocalMediaFolders];

            Language = _settingsService.AppSettings.Language;
            CoverImageRadius = _settingsService.AppSettings.CoverImageRadius;

            AutoStartWindowType = _settingsService.AppSettings.AutoStartWindowType;
            AutoLockOnDesktopMode = _settingsService.AppSettings.AutoLockOnDesktopMode;

            CoverOverlayOpacity = _settingsService.AppSettings.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.AppSettings.CoverOverlayBlurAmount;
            CoverOverlaySpeed = _settingsService.AppSettings.CoverOverlaySpeed;

            CoverAcrylicEffectAmount = _settingsService.AppSettings.CoverAcrylicEffectAmount;

            LyricsAlignmentType = _settingsService.AppSettings.LyricsAlignmentType;
            SongInfoAlignmentType = _settingsService.AppSettings.SongInfoAlignmentType;
            LyricsFontWeight = _settingsService.AppSettings.LyricsFontWeight;
            LyricsBlurAmount = _settingsService.AppSettings.LyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.AppSettings.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.AppSettings.LyricsLineSpacingFactor;

            // Font size
            LyricsStandardFontSize = _settingsService.AppSettings.LyricsStandardFontSize;
            LyricsDockFontSize = _settingsService.AppSettings.LyricsDockFontSize;
            LyricsDesktopFontSize = _settingsService.AppSettings.LyricsDesktopFontSize;

            IsLyricsGlowEffectEnabled = _settingsService.AppSettings.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.AppSettings.LyricsGlowEffectScope;
            LyricsHighlightScope = _settingsService.AppSettings.LyricsHighlightScope;
            IsFanLyricsEnabled = _settingsService.AppSettings.IsFanLyricsEnabled;

            LyricsBgFontColorType = _settingsService.AppSettings.LyricsBgFontColorType;
            LyricsFgFontColorType = _settingsService.AppSettings.LyricsFgFontColorType;
            LyricsStrokeFontColorType = _settingsService.AppSettings.LyricsStrokeFontColorType;

            LyricsCustomBgFontColor = _settingsService.AppSettings.LyricsCustomBgFontColor;
            LyricsCustomFgFontColor = _settingsService.AppSettings.LyricsCustomFgFontColor;
            LyricsCustomStrokeFontColor = _settingsService.AppSettings.LyricsCustomStrokeFontColor;

            LyricsFontStrokeWidth = _settingsService.AppSettings.LyricsFontStrokeWidth;
            LyricsBackgroundTheme = _settingsService.AppSettings.LyricsBackgroundTheme;
            MediaSourceProvidersInfo = [.. _settingsService.AppSettings.MediaSourceProvidersInfo];
            SelectedMediaSourceProvider = MediaSourceProvidersInfo.FirstOrDefault();

            IgnoreFullscreenWindow = _settingsService.AppSettings.IgnoreFullscreenWindow;

            LyricsScrollEasingType = _settingsService.AppSettings.LyricsScrollEasingType;
            LyricsScrollDuration = _settingsService.AppSettings.LyricsScrollDuration;
            LyricsScrollTopDuration = _settingsService.AppSettings.LyricsScrollTopDuration;
            LyricsScrollBottomDuration = _settingsService.AppSettings.LyricsScrollBottomDuration;

            IsLyricsFloatAnimationEnabled = _settingsService.AppSettings.IsLyricsFloatAnimationEnabled;
            LockHotKeyIndex = _settingsService.AppSettings.LockHotKeyIndex;

            LXMusicServer = _settingsService.AppSettings.LXMusicServer;
            DockPlacement = _settingsService.AppSettings.DockPlacement;
            LyricsBgFontOpacity = _settingsService.AppSettings.LyricsBgFontOpacity;
            HideWindowWhenNotPlaying = _settingsService.AppSettings.HideWindowWhenNotPlaying;
            DockWindowHeight = _settingsService.AppSettings.DockWindowHeight;

            SystemFontNames = [.. FontHelper.SystemFontFamilies];
            SelectedFontFamilyIndex = _settingsService.AppSettings.SelectedFontFamilyIndex;
            LyricsFontFamily = _settingsService.AppSettings.LyricsFontFamily;
            IsDragEverywhereEnabled = _settingsService.AppSettings.IsDragEverywhereEnabled;

            MonitorDeviceNames = [.. MonitorHelper.GetAllMonitorDeviceNames()];
            SelectedDockMonitorDeviceName = _settingsService.AppSettings.DockMonitorDeviceName;

            LyricsTranslationSeparator = _settingsService.AppSettings.LyricsTranslationSeparator;

            _mediaSessionsService.MediaSourceProvidersInfoChanged += MediaSessionsService_SessionIdsChanged;
            _mediaSessionsService.SongInfoChanged += MediaSessionsService_SongInfoChanged;
        }

        private void MediaSessionsService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            var current = MediaSourceProvidersInfo.Where(x => x.Provider == e.SongInfo?.SourceAppUserModelId)?.FirstOrDefault();
            if (_mediaSessionsService.Position.TotalSeconds <= 1 && current?.ResetPositionOffsetOnSongChanged == true)
            {
                current.PositionOffset = 0;
            }
        }

        private void LastFMService_IsAuthenticatedChanged(object? sender, Events.LastFMIsAuthenticatedChangedEventArgs e)
        {
            IsLastFMAuthenticated = e.IsAuthenticated;
        }

        private void LastFMService_UserChanged(object? sender, Events.LastFMUserChangedEventArgs e)
        {
            LastFMUser = e.User;
        }
    }
}
