using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public LyricsRendererViewModel(
            ISettingsService settingsService, 
            IMediaSessionsService mediaSessionsService, 
            ILyricsSearchService musicSearchService, 
            ILibWatcherService libWatcherService, 
            ITranslateService libreTranslateService,
            ILastFMService lastFMService
            ) : base(settingsService)
        {
            _lyrcsSearchService = musicSearchService;
            _mediaSessionsService = mediaSessionsService;
            _libWatcherService = libWatcherService;
            _translateService = libreTranslateService;

            _lastFMService = lastFMService;

            _mediaSourceProvidersInfo = _settingsService.AppSettings.MediaSourceProvidersInfo;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            _albumArtCornerRadius = _settingsService.AppSettings.CoverImageRadius;
            _albumArtBgOpacity = _settingsService.AppSettings.CoverOverlayOpacity;
            _albumArtBgBlurAmount = _settingsService.AppSettings.CoverOverlayBlurAmount;
            _coverAcrylicEffectAmount = _settingsService.AppSettings.CoverAcrylicEffectAmount;
            _coverOverlaySpeed = _settingsService.AppSettings.CoverOverlaySpeed;

            _lyricsBgFontColorType = _settingsService.AppSettings.LyricsBgFontColorType;
            _lyricsFgFontColorType = _settingsService.AppSettings.LyricsFgFontColorType;

            _lyricsTextFormat.FontWeight = _settingsService.AppSettings.LyricsFontWeight.ToFontWeight();

            _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = _settingsService.AppSettings.LyricsFontFamily;

            _lyricsAlignmentType = _settingsService.AppSettings.LyricsAlignmentType;
            _lyricsVerticalEdgeOpacity = _settingsService.AppSettings.LyricsVerticalEdgeOpacity;
            _lyricsLineSpacingFactor = _settingsService.AppSettings.LyricsLineSpacingFactor;
            
            _lyricsStandardFontSize = _settingsService.AppSettings.LyricsStandardFontSize;
            _lyricsDockFontSize = _settingsService.AppSettings.LyricsDockFontSize;
            _lyricsDesktopFontSize = _settingsService.AppSettings.LyricsDesktopFontSize;

            _lyricsBlurAmount = _settingsService.AppSettings.LyricsBlurAmount;
            _isLyricsGlowEffectEnabled = _settingsService.AppSettings.IsLyricsGlowEffectEnabled;
            _lyricsGlowEffectScope = _settingsService.AppSettings.LyricsGlowEffectScope;
            _lyricsHighlightScope = _settingsService.AppSettings.LyricsHighlightScope;

            _customBgFontColor = _settingsService.AppSettings.LyricsCustomBgFontColor;
            _customFgFontColor = _settingsService.AppSettings.LyricsCustomFgFontColor;

            _lyricsBgTheme = _settingsService.AppSettings.LyricsBackgroundTheme;

            _isFanLyricsEnabled = _settingsService.AppSettings.IsFanLyricsEnabled;

            // 歌词描边
            _lyricsFontStrokeWidth = _settingsService.AppSettings.LyricsFontStrokeWidth;
            _lyricsStrokeFontColorType = _settingsService.AppSettings.LyricsStrokeFontColorType;
            _customStrokeFontColor = _settingsService.AppSettings.LyricsCustomStrokeFontColor;
            
            _isTranslationEnabled = _settingsService.AppSettings.IsTranslationEnabled;
            _showTranslationOnly = _settingsService.AppSettings.ShowTranslationOnly;
            _isLibreTranslateEnabled = _settingsService.AppSettings.IsLibreTranslateEnabled;
            _targetLanguageIndex = _settingsService.AppSettings.SelectedTargetLanguageIndex;
            _lyricsTranslationSeparator = _settingsService.AppSettings.LyricsTranslationSeparator;

            _dockPlacement = _settingsService.AppSettings.DockPlacement;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.AppSettings.SongInfoAlignmentType.ToCanvasHorizontalAlignment();

            _timelineSyncThreshold = 0;

            _lyricsScrollTopDuration = _settingsService.AppSettings.LyricsScrollTopDuration / 1000.0;
            _lyricsScrollBottomDuration = _settingsService.AppSettings.LyricsScrollBottomDuration / 1000.0;
            _canvasYScrollTransition.SetDuration(_settingsService.AppSettings.LyricsScrollDuration / 1000.0);
            _canvasYScrollTransition.SetEasingType(_settingsService.AppSettings.LyricsScrollEasingType);
            _defaultOpacity = _settingsService.AppSettings.LyricsBgFontOpacity / 100f;

            _isLyricsFloatAnimationEnabled = _settingsService.AppSettings.IsLyricsFloatAnimationEnabled;
            _displayType = _displayTypeReceived = _settingsService.AppSettings.DisplayType;

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _mediaSessionsService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _mediaSessionsService.AlbumArtChangedChanged += PlaybackService_AlbumArtChangedChanged;
            _mediaSessionsService.TimelineChanged += PlaybackService_TimelineChanged;

            _isPlaying = _mediaSessionsService.IsPlaying;

            UpdateColorConfig();
        }
    }
}
