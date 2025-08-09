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

            _mediaSourceProvidersInfo = _settingsService.MediaSourceProvidersInfo;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            _albumArtCornerRadius = _settingsService.CoverImageRadius;
            _albumArtBgOpacity = _settingsService.CoverOverlayOpacity;
            _albumArtBgBlurAmount = _settingsService.CoverOverlayBlurAmount;
            _coverAcrylicEffectAmount = _settingsService.CoverAcrylicEffectAmount;
            _coverOverlaySpeed = _settingsService.CoverOverlaySpeed;

            _lyricsBgFontColorType = _settingsService.LyricsBgFontColorType;
            _lyricsFgFontColorType = _settingsService.LyricsFgFontColorType;

            _lyricsTextFormat.FontWeight = _settingsService.LyricsFontWeight.ToFontWeight();

            _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = _settingsService.LyricsFontFamily;

            _lyricsAlignmentType = _settingsService.LyricsAlignmentType;
            _lyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            _lyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            
            _lyricsStandardFontSize = _settingsService.LyricsStandardFontSize;
            _lyricsDockFontSize = _settingsService.LyricsDockFontSize;
            _lyricsDesktopFontSize = _settingsService.LyricsDesktopFontSize;

            _lyricsBlurAmount = _settingsService.LyricsBlurAmount;
            _isLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            _lyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            _lyricsHighlightScope = _settingsService.LyricsHighlightScope;

            _customBgFontColor = _settingsService.LyricsCustomBgFontColor;
            _customFgFontColor = _settingsService.LyricsCustomFgFontColor;

            _lyricsBgTheme = _settingsService.LyricsBackgroundTheme;

            _isFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;

            // 歌词描边
            _lyricsFontStrokeWidth = _settingsService.LyricsFontStrokeWidth;
            _lyricsStrokeFontColorType = _settingsService.LyricsStrokeFontColorType;
            _customStrokeFontColor = _settingsService.LyricsCustomStrokeFontColor;
            
            _isTranslationEnabled = _settingsService.IsTranslationEnabled;
            _showTranslationOnly = _settingsService.ShowTranslationOnly;
            _isLibreTranslateEnabled = _settingsService.IsLibreTranslateEnabled;
            _targetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;
            _lyricsTranslationSeparator = _settingsService.LyricsTranslationSeparator;

            _dockPlacement = _settingsService.DockPlacement;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.SongInfoAlignmentType.ToCanvasHorizontalAlignment();

            _timelineSyncThreshold = 0;

            _lyricsScrollTopDuration = _settingsService.LyricsScrollTopDuration / 1000.0;
            _lyricsScrollBottomDuration = _settingsService.LyricsScrollBottomDuration / 1000.0;
            _canvasYScrollTransition.SetDuration(_settingsService.LyricsScrollDuration / 1000.0);
            _canvasYScrollTransition.SetEasingType(_settingsService.LyricsScrollEasingType);
            _defaultOpacity = _settingsService.LyricsBgFontOpacity / 100f;

            _isLyricsFloatAnimationEnabled = _settingsService.IsLyricsFloatAnimationEnabled;
            _displayType = _displayTypeReceived = _settingsService.DisplayType;

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
