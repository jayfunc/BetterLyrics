// 2025/6/23 by Zhe Fang

using ABI.Microsoft.UI.Xaml;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel : BaseViewModel
    {
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private TimeSpan _totalTime = TimeSpan.Zero;
        private TimeSpan _positionOffset = TimeSpan.Zero;

        private SoftwareBitmap? _lastAlbumArtSwBitmap = null;
        private SoftwareBitmap? _albumArtSwBitmap = null;

        private CanvasBitmap? _lastAlbumArtCanvasBitmap = null;
        private CanvasBitmap? _albumArtCanvasBitmap = null;

        private float _albumArtSize = 0f;
        private int _albumArtCornerRadius = 0;

        private float _albumArtY = 0f;

        private string? _lastSongTitle;
        private string? _songTitle;

        private float _titleY = 0f;

        private string? _lastSongArtist;
        private string? _songArtist;

        private float _canvasWidth = 0f;
        private float _canvasHeight = 0f;

        private readonly float _defaultOpacity = 0.3f;
        private readonly float _highlightedOpacity = 1.0f;

        private readonly float _defaultScale = 0.75f;
        private readonly float _highlightedScale = 1.0f;

        private readonly float _coverRotateSpeed = 0.003f;
        private float _rotateAngle = 0f;

        private readonly float _lyricsGlowEffectAmount = 8f;

        private float _maxLyricsWidth = 0f;

        private readonly IMusicSearchService _musicSearchService;
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;
        private readonly ILibreTranslateService _libreTranslateService;
        private readonly ILogger _logger;

        private readonly float _leftMargin = 36f;
        private readonly float _middleMargin = 36f;
        private readonly float _rightMargin = 36f;
        private readonly float _topMargin = 36f;
        private readonly float _bottomMargin = 36f;

        private Color _adaptiveGrayedFontColor = Colors.Transparent;
        private Color? _adaptiveColoredFontColor = null;

        private Color? _albumArtAccentColor = null;
        private Color _environmentalColor = Colors.Transparent;

        private Color _lightColor = Colors.White;
        private Color _darkColor = Colors.Black;

        private Color _bgFontColor;
        private Color _fgFontColor;
        private Color _strokeFontColor;

        private Color? _customBgFontColor;
        private Color? _customFgFontColor;
        private Color? _customStrokeFontColor;

        private LyricsFontColorType _lyricsBgFontColorType;
        private LyricsFontColorType _lyricsFgFontColorType;
        private LyricsFontColorType _lyricsStrokeFontColorType;

        private ElementTheme _lyricsBgTheme;

        private int _lyricsFontStrokeWidth;

        private int _playingLineIndex = -1;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _isDebugOverlayEnabled = false;
        private bool _isDesktopMode = false;
        private bool _isDockMode = false;
        private bool _isFanLyricsEnabled = false;

        private bool _isPlaying = true;

        private bool _isLayoutChanged = true;

        private int _langIndex = 0;

        private List<List<LyricsLine>> _multiLangLyrics = [];
        private List<string> _translations = [];
        private bool _isTranslationEnabled = false;
        private int _targetLanguageIndex = 6;

        private CanvasTextFormat _lyricsTextFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };
        private CanvasTextFormat _titleTextFormat = new()
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingSign = CanvasTrimmingSign.Ellipsis,
            TrimmingGranularity = CanvasTextTrimmingGranularity.Character,
        };
        private CanvasTextFormat _artistTextFormat = new()
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingSign = CanvasTrimmingSign.Ellipsis,
            TrimmingGranularity = CanvasTextTrimmingGranularity.Character,
        };

        private Task? _refreshLyricsTask;
        private CancellationTokenSource? _refreshLyricsCts;

        private Task? _showTranslationsTask;
        private CancellationTokenSource? _showTranslationsCts;

        public LyricsRendererViewModel(ISettingsService settingsService, IPlaybackService playbackService, IMusicSearchService musicSearchService, ILibWatcherService libWatcherService, ILibreTranslateService libreTranslateService) : base(settingsService)
        {
            _musicSearchService = musicSearchService;
            _playbackService = playbackService;
            _libWatcherService = libWatcherService;
            _libreTranslateService = libreTranslateService;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            _albumArtCornerRadius = _settingsService.CoverImageRadius;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            _lyricsBgFontColorType = _settingsService.LyricsBgFontColorType;
            _lyricsFgFontColorType = _settingsService.LyricsFgFontColorType;

            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;

            _customBgFontColor = _settingsService.LyricsCustomBgFontColor;
            _customFgFontColor = _settingsService.LyricsCustomFgFontColor;

            _lyricsBgTheme = _settingsService.LyricsBackgroundTheme;

            _isFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;

            _lyricsFontStrokeWidth = _settingsService.LyricsFontStrokeWidth;

            _isTranslationEnabled = _settingsService.IsTranslationEnabled;
            _targetLanguageIndex = _settingsService.SelectedTargetLanguageIndex;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.SongInfoAlignmentType.ToCanvasHorizontalAlignment();

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.PositionChanged += PlaybackService_PositionChanged;

            UpdateFontColor();
        }

        [ObservableProperty]
        public partial bool IsTranslating { get; set; } = false;

        public int CoverOverlayBlurAmount { get; set; }

        public int CoverOverlayOpacity { get; set; }

        private LyricsDisplayType _displayTypeReceived = LyricsDisplayType.PlaceholderOnly;
        private LyricsDisplayType _displayType = LyricsDisplayType.PlaceholderOnly;

        public bool IsDynamicCoverOverlayEnabled { get; set; }

        public bool IsLyricsGlowEffectEnabled { get; set; }

        public TextAlignmentType LyricsAlignmentType { get; set; }

        public int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        public LineRenderingType LyricsGlowEffectScope { get; set; }

        [ObservableProperty]
        public partial float LyricsLineSpacingFactor { get; set; }

        public int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeTypeSent { get; set; }

        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < _multiLangLyrics.SafeGet(_langIndex)?.Count; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?[i];
                if (line == null)
                {
                    continue;
                }
                if (
                    line.StartMs <= _totalTime.TotalMilliseconds + _positionOffset.TotalMilliseconds
                    && _totalTime.TotalMilliseconds + _positionOffset.TotalMilliseconds <= line.EndMs
                )
                {
                    return i;
                }
            }

            return -1;
        }

        private void GetLinePlayingProgress(LyricsLine line, out int charStartIndex, out int charLength, out float charProgress)
        {
            charStartIndex = 0;
            charLength = 0;
            charProgress = 0f;

            float now = (float)_totalTime.TotalMilliseconds + (float)_positionOffset.TotalMilliseconds;

            // 1. 还没到本句
            if (now < line.StartMs)
            {
                return;
            }

            // 2. 已经超过本句
            if (now > line.EndMs)
            {
                return;
            }

            // 3. 有逐字时间轴
            if (line.CharTimings != null && line.CharTimings.Count > 0)
            {
                int charTimingsCount = line.CharTimings.Count;
                for (int i = 0; i < charTimingsCount; i++)
                {
                    var timing = line.CharTimings[i];

                    // 当前时间在某个字的高亮区间
                    if (now >= timing.StartMs && now <= timing.EndMs)
                    {
                        charStartIndex = timing.StartIndex;
                        charLength = timing.Text.Length;
                        if (timing.EndMs != timing.StartMs)
                        {
                            charProgress = (now - timing.StartMs) / (timing.EndMs - timing.StartMs);
                        }
                        else
                        {
                            charProgress = 0f;
                        }
                        return;
                    }
                }
            }
            else
            {
                // 没有逐字时间轴，直接线性
                charProgress = (now - line.StartMs) / line.DurationMs;
                charProgress = Math.Clamp(charProgress, 0f, 1f);
                charStartIndex = 0;
                charLength = line.OriginalText.Length;
            }
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (
                SongInfo == null
                || _multiLangLyrics.SafeGet(_langIndex) == null
                || _multiLangLyrics[_langIndex].Count == 0
            )
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _multiLangLyrics[_langIndex].Count - 1);
        }

        private void LibWatcherService_MusicLibraryFilesChanged(object? sender, LibChangedEventArgs e)
        {
            _logger.LogInformation("Music library files changed: {ChangeType} {FilePath}, refreshing lyrics...", e.ChangeType, e.FilePath);
            RefreshLyricsAsync();
        }

        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            _isPlaying = e.IsPlaying;
        }

        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            if (Math.Abs(_totalTime.TotalMilliseconds - e.Position.TotalMilliseconds) > 300)
            {
                _totalTime = e.Position;
            }
        }

        private async void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;

            if (SongInfo?.AlbumArtSwBitmap != _albumArtSwBitmap)
            {
                _lastAlbumArtSwBitmap = _albumArtSwBitmap;
                _lastAlbumArtCanvasBitmap = null;

                _albumArtSwBitmap = SongInfo?.AlbumArtSwBitmap;
                _albumArtCanvasBitmap = null;

                _albumArtAccentColor = SongInfo?.AlbumArtAccentColor;

                _albumArtBgTransition.Reset(0f);
                _albumArtBgTransition.StartTransition(1f);

                UpdateFontColor();
            }

            if (SongInfo?.Title != _songTitle || SongInfo?.Artist != _songArtist)
            {
                _lastSongTitle = _songTitle;
                _songTitle = SongInfo?.Title;

                _lastSongArtist = _songArtist;
                _songArtist = SongInfo?.Artist;

                _songInfoOpacityTransition.Reset(0f);
                _songInfoOpacityTransition.StartTransition(1f);

                _logger.LogInformation("Song info changed: Title={Title}, Artist={Artist}, refreshing lyrics...", _songTitle, _songArtist);
                await RefreshLyricsAsync();

                _totalTime = TimeSpan.Zero;
            }
        }

        private async Task RefreshLyricsAsync()
        {
            // 取消上一次
            _refreshLyricsCts?.Cancel();
            if (_refreshLyricsTask != null)
            {
                await _refreshLyricsTask;
            }

            var cts = new CancellationTokenSource();
            _refreshLyricsCts = cts;
            var token = cts.Token;

            _refreshLyricsTask = RefreshLyricsCoreAsync(token);
            await _refreshLyricsTask;
        }

        private async Task UpdateTranslationsAsync()
        {
            IsTranslating = true;
            if (_isTranslationEnabled)
            {
                await ShowWithTranslationsAsync();
            }
            else
            {
                ShowOriginalsOnly();
            }
            IsTranslating = false;
        }

        private async Task ShowWithTranslationsAsync()
        {
            _showTranslationsCts?.Cancel();
            if (_showTranslationsTask != null)
            {
                await _showTranslationsTask;
            }

            var cts = new CancellationTokenSource();
            _showTranslationsCts = cts;
            var token = cts.Token;

            _showTranslationsTask = ShowTranslationsCoreAsync(token);
            await _showTranslationsTask;
        }

        private async Task ShowTranslationsCoreAsync(CancellationToken token)
        {
            _logger.LogInformation("Showing translations for lyrics...");
            try
            {
                if (string.IsNullOrEmpty(_settingsService.LibreTranslateServer))
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        App.Current.LyricsWindowNotificationPanel?.Notify(
                            App.ResourceLoader!.GetString("TranslateServerNotSet"),
                            Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
                        );
                    });
                    ShowOriginalsOnly();
                    return;
                }
                var text = string.Join("\n", _multiLangLyrics.FirstOrDefault()?.Select(x => x.OriginalText) ?? []);
                var translated = await _libreTranslateService.TranslateAsync(text, token);
                token.ThrowIfCancellationRequested();

                _translations = translated.Split('\n').ToList();
                bool totallySame = true;
                foreach (var langLyrics in _multiLangLyrics)
                {
                    int i = 0;
                    foreach (var line in langLyrics)
                    {
                        if (line.OriginalText != _translations[i])
                        {
                            totallySame = false;
                            break;
                        }
                        i++;
                    }
                    break;
                }

                foreach (var langLyrics in _multiLangLyrics)
                {
                    int i = 0;
                    foreach (var line in langLyrics)
                    {
                        line.DisplayedText = totallySame ? line.OriginalText : $"{line.OriginalText}\n{_translations[i]}";
                        i++;
                    }
                    break;
                }
                _isLayoutChanged = true;
            }
            catch (Exception)
            {
                IsTranslating = false;
            }
        }

        private void ShowOriginalsOnly()
        {
            _logger.LogInformation("Showing original lyrics only, translations disabled.");
            foreach (var langLyrics in _multiLangLyrics)
            {
                foreach (var line in langLyrics)
                {
                    line.DisplayedText = line.OriginalText;
                }
            }
            _isLayoutChanged = true;
        }

        private async Task RefreshLyricsCoreAsync(CancellationToken token)
        {
            try
            {
                _logger.LogInformation("Refreshing lyrics...");

                SetLyricsLoadingPlaceholder();

                string? lyricsRaw = null;

                if (SongInfo != null)
                {
                    lyricsRaw = await _musicSearchService.SearchLyricsAsync(
                        SongInfo.Title,
                        SongInfo.Artist,
                        SongInfo.Album ?? "",
                        SongInfo.DurationMs ?? 0,
                        token
                    );
                    _logger.LogInformation("Lyrics search result: {LyricsRaw}", lyricsRaw ?? "null");
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    _logger.LogWarning("SongInfo is null, cannot search lyrics.");
                }

                _multiLangLyrics = new LyricsParser().Parse(
                        lyricsRaw,
                        (int?)SongInfo?.DurationMs ?? (int)TimeSpan.FromMinutes(99).TotalMilliseconds
                    );
                _logger.LogInformation("Parsed lyrics: {MultiLangLyricsCount} languages", _multiLangLyrics.Count);
                await UpdateTranslationsAsync();
                token.ThrowIfCancellationRequested();
            }
            catch (Exception) { }
        }

        private void SetLyricsLoadingPlaceholder()
        {
            _multiLangLyrics = [];
            _multiLangLyrics.Add(
                [
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                        OriginalText = "● ● ●",
                        DisplayedText = "● ● ●",
                        CharTimings = [],
                    },
                ]
            );
            _isLayoutChanged = true;
        }
    }
}
