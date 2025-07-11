// 2025/6/23 by Zhe Fang

using ABI.Microsoft.UI.Xaml;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Helpers.General;
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

        private TextAlignmentType _lyricsAlignmentType;

        private readonly float _lyricsGlowEffectAmount = 8f;
        private int _lyricsBlurAmount;
        private int _lyricsVerticalEdgeOpacity;

        private ElementTheme _lyricsBgTheme;
        private LineRenderingType _lyricsGlowEffectScope;

        private int _lyricsFontStrokeWidth;
        private int _lyricsFontSize;
        private float _lyricsLineSpacingFactor;

        private LyricsFontColorType _lyricsBgFontColorType;
        private LyricsFontColorType _lyricsFgFontColorType;
        private LyricsFontColorType _lyricsStrokeFontColorType;

        private float _maxLyricsWidth = 0f;

        private readonly ILyricsSearchService _lyrcsSearchService;
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;
        private readonly ITranslateService _translateService;
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

        private int _playingLineIndex = -1;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _isDebugOverlayEnabled = false;
        private bool _isDesktopMode = false;
        private bool _isDockMode = false;
        private bool _isFanLyricsEnabled = false;

        private bool _isPlaying = true;

        private bool _isLyricsWindowLocked = false;
        private bool _isMouseWithinWindow = false;

        private bool _isDynamicCoverOverlayEnabled;
        private bool _isLyricsGlowEffectEnabled;

        private bool _isLayoutChanged = true;

        private int _langIndex = 0;

        private List<List<LyricsLine>> _multiLangLyrics = [];
        private List<string> _translationList = [];
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

        private LatestOnlyTaskRunner _refreshLyricsRunner = new();
        private LatestOnlyTaskRunner _showTranslationsRunner = new();

        private LyricsDisplayType _displayTypeReceived = LyricsDisplayType.PlaceholderOnly;
        private LyricsDisplayType _displayType = LyricsDisplayType.PlaceholderOnly;

        private int _albumArtBgBlurAmount;
        private int _albumArtBgOpacity;

        [ObservableProperty]
        public partial bool IsTranslating { get; set; } = false;

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
            _ = _refreshLyricsRunner.RunAsync(async token =>
            {
                await RefreshLyricsAsync(token);
            });
        }

        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            _isPlaying = e.IsPlaying;
        }

        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            _totalTime = e.Position;
        }

        private void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;

            if (SongInfo?.Title != _songTitle || SongInfo?.Artist != _songArtist)
            {
                _lastSongTitle = _songTitle;
                _songTitle = SongInfo?.Title;

                _lastSongArtist = _songArtist;
                _songArtist = SongInfo?.Artist;

                _songInfoOpacityTransition.Reset(0f);
                _songInfoOpacityTransition.StartTransition(1f);

                _logger.LogInformation("Song info changed: Title={Title}, Artist={Artist}, refreshing lyrics...", _songTitle, _songArtist);
                _ = _refreshLyricsRunner.RunAsync(async token =>
                {
                    await RefreshLyricsAsync(token);
                });
                _totalTime = TimeSpan.Zero;
            }
        }

        private void PlaybackService_AlbumArtChangedChanged(object? sender, AlbumArtChangedEventArgs e)
        {
            if (e.AlbumArtSwBitmap != _albumArtSwBitmap)
            {
                _lastAlbumArtSwBitmap = _albumArtSwBitmap;
                _lastAlbumArtCanvasBitmap = null;

                _albumArtSwBitmap = e.AlbumArtSwBitmap;
                _albumArtCanvasBitmap = null;

                _albumArtAccentColor = e.AlbumArtAccentColor;

                _albumArtBgTransition.Reset(0f);
                _albumArtBgTransition.StartTransition(1f);

                UpdateFontColor();
            }
        }

        private void UpdateTranslations()
        {
            IsTranslating = true;
            if (_isTranslationEnabled)
            {
                _ = _refreshLyricsRunner.RunAsync(async token =>
                {
                    await ShowTranslationsAsync(token);
                    IsTranslating = false;
                });
            }
            else
            {
                ShowOriginalsOnly();
                IsTranslating = false;
            }
        }

        private async Task ShowTranslationsAsync(CancellationToken token)
        {
            _logger.LogInformation("Showing translation for lyrics...");
            string targetLangCode = LanguageHelper.SupportedTargetLanguages[_settingsService.SelectedTargetLanguageIndex].Code;
            var originalText = string.Join("\n", _multiLangLyrics.FirstOrDefault()?.Select(x => x.OriginalText) ?? []);
            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);

            if (originalLangCode == targetLangCode)
            {
                _logger.LogInformation("Original lyrics already in target language: {TargetLangCode}", targetLangCode);
                ShowOriginalsOnly();
                return;
            }

            // Try get translation from itself first
            if (_multiLangLyrics.Count > 1)
            {
                foreach (var langLyrics in _multiLangLyrics.Skip(1))
                {
                    var translationList = langLyrics.Select(x => x.OriginalText).ToList();
                    var translation = string.Join("\n", translationList);
                    if (LanguageHelper.DetectLanguageCode(translation) == targetLangCode)
                    {
                        _translationList = translationList;
                        break;
                    }
                }
            }
            else
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

                var translated = await _translateService.TranslateAsync(originalText, targetLangCode, token);
                token.ThrowIfCancellationRequested();

                _translationList = translated.Split('\n').ToList();
            }

            int i = 0;
            foreach (var line in _multiLangLyrics.FirstOrDefault() ?? [])
            {
                line.DisplayedText = i < _translationList.Count ? $"{line.OriginalText}\n{_translationList[i]}" : line.OriginalText;
                i++;
            }
            _isLayoutChanged = true;
        }

        private void ShowOriginalsOnly()
        {
            _logger.LogInformation("Showing original lyrics only, translation disabled.");
            foreach (var langLyrics in _multiLangLyrics)
            {
                foreach (var line in langLyrics)
                {
                    line.DisplayedText = line.OriginalText;
                }
            }
            _isLayoutChanged = true;
        }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing lyrics...");

            SetLyricsLoadingPlaceholder();

            string? lyricsRaw = null;

            if (SongInfo != null)
            {
                lyricsRaw = await _lyrcsSearchService.SearchAsync(
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

            // This ensures that original lyrics are always shown while waiting for translations
            ShowOriginalsOnly();
            UpdateTranslations();
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
