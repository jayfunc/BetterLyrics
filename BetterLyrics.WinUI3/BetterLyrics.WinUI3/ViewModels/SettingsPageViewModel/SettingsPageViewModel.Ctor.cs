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

            IsLibreTranslateEnabled = _settingsService.IsLibreTranslateEnabled;
            LibreTranslateServer = _settingsService.LibreTranslateServer;
            SelectedTargetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;

            LocalMediaFolders = [.. _settingsService.LocalMediaFolders];
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
            SelectedMediaSourceProvider = MediaSourceProvidersInfo.FirstOrDefault();

            IgnoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;

            LyricsScrollEasingType = _settingsService.LyricsScrollEasingType;
            LyricsScrollDuration = _settingsService.LyricsScrollDuration;

            IsLyricsFloatAnimationEnabled = _settingsService.IsLyricsFloatAnimationEnabled;
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

            LyricsTranslationSeparator = _settingsService.LyricsTranslationSeparator;

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
