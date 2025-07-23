using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        public LyricsRendererViewModel(ISettingsService settingsService, IPlaybackService playbackService, ILyricsSearchService musicSearchService, ILibWatcherService libWatcherService, ITranslateService libreTranslateService) : base(settingsService)
        {
            _lyrcsSearchService = musicSearchService;
            _playbackService = playbackService;
            _libWatcherService = libWatcherService;
            _translateService = libreTranslateService;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            _albumArtCornerRadius = _settingsService.CoverImageRadius;
            _isDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            _albumArtBgOpacity = _settingsService.CoverOverlayOpacity;
            _albumArtBgBlurAmount = _settingsService.CoverOverlayBlurAmount;

            _lyricsBgFontColorType = _settingsService.LyricsBgFontColorType;
            _lyricsFgFontColorType = _settingsService.LyricsFgFontColorType;

            _lyricsTextFormat.FontWeight = _settingsService.LyricsFontWeight.ToFontWeight();

            _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = _settingsService.LyricsFontFamily;

            _lyricsAlignmentType = _settingsService.LyricsAlignmentType;
            _lyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            _lyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            _lyricsFontSize = _settingsService.LyricsFontSize;
            _lyricsBlurAmount = _settingsService.LyricsBlurAmount;
            _isLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            _lyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            _lyricsHighlightScope = _settingsService.LyricsHighlightScope;

            _customBgFontColor = _settingsService.LyricsCustomBgFontColor;
            _customFgFontColor = _settingsService.LyricsCustomFgFontColor;

            _lyricsBgTheme = _settingsService.LyricsBackgroundTheme;

            _isFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;
            _lyricsFontStrokeWidth = _settingsService.LyricsFontStrokeWidth;
            _isTranslationEnabled = _settingsService.IsTranslationEnabled;
            _showTranslationOnly = _settingsService.ShowTranslationOnly;
            _targetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;
            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.SongInfoAlignmentType.ToCanvasHorizontalAlignment();

            _timelineSyncThreshold = _settingsService.TimelineSyncThreshold;

            _canvasYScrollTransition.SetDuration(_settingsService.LyricsScrollDuration / 1000f);
            _canvasYScrollTransition.SetEasingType(_settingsService.LyricsScrollEasingType);
            _defaultOpacity = _settingsService.LyricsBgFontOpacity / 100f;

            _isLyricsFloatAnimationEnabled = _settingsService.IsLyricsFloatAnimationEnabled;
            _displayType = _displayTypeReceived = _settingsService.DisplayType;

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.AlbumArtChangedChanged += PlaybackService_AlbumArtChangedChanged;
            _playbackService.TimelineChanged += PlaybackService_TimelineChanged;

            _isPlaying = _playbackService.IsPlaying;

            UpdateColorConfig();
        }
    }
}
