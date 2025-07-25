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
using Lyricify.Lyrics.Providers;
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

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        private TimeSpan _positionOffset = TimeSpan.Zero;

        private int _songDurationMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds;

        private SoftwareBitmap? _lastAlbumArtSwBitmap = null;
        private SoftwareBitmap? _albumArtSwBitmap = null;

        private CanvasBitmap? _lastAlbumArtCanvasBitmap = null;
        private CanvasBitmap? _albumArtCanvasBitmap = null;

        private CanvasBitmap? _coverAcrylicNoiseCanvasBitmap = null;

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

        private float _defaultOpacity;
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
        private LineRenderingType _lyricsHighlightScope;

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

        private bool _isLyricsFloatAnimationEnabled;

        private bool _isLayoutChanged = true;

        private int _langIndex = 0;

        private List<LyricsData> _lyricsDataArr = [];
        private List<string> _translationList = [];
        private bool _isTranslationEnabled;
        private bool _showTranslationOnly;
        private int _targetLanguageIndex;

        private int _timelineSyncThreshold;

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

        private LyricsDisplayType _displayTypeReceived;
        private LyricsDisplayType _displayType;

        private int _albumArtBgBlurAmount;
        private int _albumArtBgOpacity;

        private int _coverAcrylicEffectAmount;

        [ObservableProperty]
        public partial bool IsTranslating { get; set; } = false;

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeTypeSent { get; set; }

        private int GetCurrentPlayingLineIndex()
        {
            var totalMs = TotalTime.TotalMilliseconds + _positionOffset.TotalMilliseconds;
            if (totalMs < _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.FirstOrDefault()?.StartMs) return 0;

            for (int i = 0; i < _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.Count; i++)
            {
                var line = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;
                var nextLine = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(i + 1);
                if (nextLine != null && line.StartMs <= totalMs && totalMs < nextLine.StartMs)
                {
                    return i;
                }
                else if (nextLine == null && line.StartMs <= totalMs)
                {
                    return i;
                }
            }

            return GetMaxLyricsLineIndexBoundaries().Item2;
        }

        private void GetLinePlayingProgress(int lineIndex, out int charStartIndex, out int charLength, out float charProgress)
        {
            charStartIndex = 0;
            charLength = 0;
            charProgress = 0f;

            var line = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(lineIndex);
            if (line == null) return;
            var nextLine = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(lineIndex + 1);

            int lineEndMs;
            if (line.EndMs != null) lineEndMs = line.EndMs.Value;
            else if (nextLine != null) lineEndMs = nextLine.StartMs;
            else lineEndMs = _songDurationMs;

            float now = (float)TotalTime.TotalMilliseconds + (float)_positionOffset.TotalMilliseconds;

            // 1. 还没到本句
            if (now < line.StartMs)
            {
                return;
            }

            // 2. 已经超过本句
            if (now > lineEndMs)
            {
                charProgress = 1f;
                charStartIndex = line.OriginalText.Length - 1;
                charLength = 1;
                return;
            }

            // 3. 有逐字时间轴
            if (line.LyricsChars != null && line.LyricsChars.Count > 1)
            {
                int charTimingsCount = line.LyricsChars.Count;
                for (int i = 0; i < charTimingsCount; i++)
                {
                    var timing = line.LyricsChars[i];
                    var nextTiming = line.LyricsChars.ElementAtOrDefault(i + 1);

                    int timingEndMs;
                    if (timing.EndMs != null) timingEndMs = timing.EndMs.Value;
                    else if (nextTiming != null) timingEndMs = nextTiming.StartMs;
                    else timingEndMs = lineEndMs;

                    charStartIndex = timing.StartIndex;
                    charLength = timing.Text.Length;

                    // 当前时间在某个字的高亮区间
                    if (now >= timing.StartMs && now <= timingEndMs)
                    {
                        if (timingEndMs != timing.StartMs)
                        {
                            charProgress = (now - timing.StartMs) / (timingEndMs - timing.StartMs);
                        }
                        else
                        {
                            charProgress = 0f;
                        }
                        return;
                    }
                    else if (now > timingEndMs && (nextTiming == null || now < nextTiming?.StartMs))
                    {
                        charProgress = 1f;
                        return;
                    }
                }
            }
            else
            {
                // 没有逐字时间轴，均匀分配每个字的高亮时间
                int textLength = line.OriginalText.Length;
                if (textLength == 0) return;

                float lineProgress = (now - line.StartMs) / (lineEndMs - line.StartMs);
                lineProgress = Math.Clamp(lineProgress, 0f, 1f);

                // 计算当前高亮到第几个字
                float charFloatIndex = lineProgress * textLength;
                int charIndex = (int)charFloatIndex;
                charStartIndex = Math.Clamp(charIndex, 0, textLength - 1);
                charLength = 1;

                // 当前字的进度（0~1）
                charProgress = charFloatIndex - charIndex;
            }
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (
                SongInfo == null
                || _lyricsDataArr.ElementAtOrDefault(_langIndex) == null
                || _lyricsDataArr[_langIndex].LyricsLines.Count == 0
            )
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _lyricsDataArr[_langIndex].LyricsLines.Count - 1);
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

        private void PlaybackService_TimelineChanged(object? sender, TimelineChangedEventArgs e)
        {
            if (Math.Abs(TotalTime.TotalMilliseconds - e.Position.TotalMilliseconds) >= _timelineSyncThreshold)
            {
                TotalTime = e.Position;
            }
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

                _songDurationMs = (int)(SongInfo?.DurationMs ?? TimeSpan.FromMinutes(99).TotalMilliseconds);

                _songInfoOpacityTransition.Reset(0f);
                _songInfoOpacityTransition.StartTransition(1f);

                _logger.LogInformation("Song info changed: Title={Title}, Artist={Artist}, refreshing lyrics...", _songTitle, _songArtist);
                _ = _refreshLyricsRunner.RunAsync(async token =>
                {
                    await RefreshLyricsAsync(token);
                });
                TotalTime = TimeSpan.Zero;
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

                UpdateColorConfig();
            }
        }

        private void UpdateTranslations()
        {
            _lyricsDataArr.ElementAtOrDefault(0)?.SetDisplayedTextInOriginalText();
            _isLayoutChanged = true;

            IsTranslating = true;
            if (_isTranslationEnabled)
            {
                _ = _refreshLyricsRunner.RunAsync(async token =>
                {
                    await SetDisplayedAlongWithTranslationsAsync(token);
                    IsTranslating = false;
                    _isLayoutChanged = true;
                });
            }
            else
            {
                _lyricsDataArr.ElementAtOrDefault(0)?.SetDisplayedTextInOriginalText();
                _langIndex = 0;
                IsTranslating = false;
                _isLayoutChanged = true;
            }
        }

        private async Task SetDisplayedAlongWithTranslationsAsync(CancellationToken token)
        {
            _logger.LogInformation("Showing translation for lyrics...");
            string targetLangCode = LanguageHelper.GetUserTargetLanguageCode();
            string? originalText = _lyricsDataArr.FirstOrDefault()?.WrappedOriginalText;
            if (originalText == null) return;

            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);

            if (originalLangCode == targetLangCode)
            {
                _logger.LogInformation("Original lyrics already in target language: {TargetLangCode}", targetLangCode);
                _lyricsDataArr[0].SetDisplayedTextInOriginalText();
            }
            else
            {
                // Try get translation from itself first
                int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr);
                if (found >= 0)
                {
                    if (_showTranslationOnly)
                    {
                        _lyricsDataArr[found].SetDisplayedTextInOriginalText();
                        _langIndex = found;
                    }
                    else
                    {
                        _lyricsDataArr[0].SetDisplayedTextAlongWith(_lyricsDataArr[found]);
                        _langIndex = 0;
                    }
                }
                else
                {
                    string translated = string.Empty;
                    try
                    {
                        translated = await _translateService.TranslateTextAsync(originalText, targetLangCode, token);
                        if (translated == string.Empty) return;

                        if (_showTranslationOnly)
                        {
                            _lyricsDataArr[^1] = _lyricsDataArr[0].CreateLyricsDataFrom(translated);
                            _lyricsDataArr[^1].SetDisplayedTextInOriginalText();
                            _langIndex = _lyricsDataArr.Count - 1;
                        }
                        else
                        {
                            _lyricsDataArr[0].SetDisplayedTextAlongWith(translated);
                            _langIndex = 0;
                        }
                        token.ThrowIfCancellationRequested();
                    }
                    catch (Exception)
                    {
                        App.Current.LyricsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("LibreTranslateFailed")!, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    }
                }
            }
        }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing lyrics...");

            _lyricsDataArr = [LyricsData.GetLoadingPlaceholder()];
            _isLayoutChanged = true;

            string? lyricsRaw = null;
            LyricsSearchProvider? provider = null;

            if (SongInfo != null)
            {
                (lyricsRaw, provider) = await _lyrcsSearchService.SearchAsync(
                    SongInfo.Title,
                    SongInfo.Artist,
                    SongInfo.Album ?? "",
                    SongInfo.DurationMs ?? 0,
                    token
                );
                _logger.LogInformation("Lyrics search result: {LyricsRaw}", lyricsRaw ?? "null");
                token.ThrowIfCancellationRequested();
                _lyricsDataArr = new LyricsParser().Parse(lyricsRaw, (int?)SongInfo?.DurationMs);
                FillTranslationFromCache(provider);
            }
            else
            {
                _logger.LogWarning("SongInfo is null, cannot search lyrics.");
            }

            _logger.LogInformation("Parsed lyrics: {MultiLangLyricsCount} languages", _lyricsDataArr.Count);

            // This ensures that original lyrics are always shown while waiting for translations
            _lyricsDataArr[0].SetDisplayedTextInOriginalText();
            _isLayoutChanged = true;

            UpdateTranslations();
        }

        private void FillTranslationFromCache(LyricsSearchProvider? provider)
        {
            string? translationRaw = null;
            switch (provider)
            {
                case LyricsSearchProvider.QQ:
                    translationRaw = FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, PathHelper.QQTranslationCacheDirectory);
                    break;
                case LyricsSearchProvider.Kugou:
                    break;
                case LyricsSearchProvider.Netease:
                    break;
                case LyricsSearchProvider.LrcLib:
                    break;
                case LyricsSearchProvider.AmllTtmlDb:
                    break;
                case LyricsSearchProvider.LocalMusicFile:
                    break;
                case LyricsSearchProvider.LocalLrcFile:
                    break;
                case LyricsSearchProvider.LocalEslrcFile:
                    break;
                case LyricsSearchProvider.LocalTtmlFile:
                    break;
                default:
                    break;
            }
            if (translationRaw != null)
            {
                var translationData = new LyricsParser().Parse(translationRaw, (int?)SongInfo?.DurationMs);
                foreach (var data in translationData)
                {
                    data.LyricsLines = data.LyricsLines.Where(line => !string.IsNullOrWhiteSpace(line.OriginalText)).ToList();
                    foreach (var item in data.LyricsLines)
                    {
                        if (item.OriginalText == "//") item.OriginalText = "";
                    }
                }
                _lyricsDataArr = _lyricsDataArr.Concat(translationData).ToList();
            }
        }
    }
}
