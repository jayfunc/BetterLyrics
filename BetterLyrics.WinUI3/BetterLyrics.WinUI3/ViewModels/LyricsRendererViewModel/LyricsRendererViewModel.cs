// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
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

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        private bool _isLastFMTrackEnabled = false;
        private bool _isLastFMTracked = false;
        private TimeSpan _totalPlayingTime = TimeSpan.Zero;

        private TimeSpan _elapsedTime = TimeSpan.Zero;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        private TimeSpan _positionOffset = TimeSpan.Zero;

        private int _songDurationMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds;

        private Stopwatch? _drawFrameStopwatch;
        private int _drawFrameCount = 0;
        private int _displayedDrawFrameCount = 0;

        private Queue<SoftwareBitmap?> _cachedAlbumArtSwBitmaps = [];

        private SoftwareBitmap? _lastAlbumArtSwBitmap = null;
        private SoftwareBitmap? _albumArtSwBitmap = null;

        private CanvasBitmap? _lastAlbumArtCanvasBitmap = null;
        private CanvasBitmap? _albumArtCanvasBitmap = null;

        private CanvasRenderTarget? _albumArtBgRenderTarget;
        private CanvasRenderTarget? _albumArtRenderTarget;

        private CanvasBitmap? _coverAcrylicNoiseCanvasBitmap = null;

        private double _albumArtSize = 0f;

        private string? _lastSongTitle;
        private string? _songTitle;

        private string? _lastSongArtist;
        private string? _songArtist;

        private double _canvasWidth = 0f;
        private double _canvasHeight = 0f;

        private readonly double _defaultScale = 0.75f;
        private readonly double _highlightedScale = 1.0f;

        private readonly double _coverRotateBaseSpeed = 0.003f;
        private double _rotateAngle = 0f;

        private double _canvasTargetYScrollOffset = 0;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsSearchProvider? LyricsSearchProvider { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TranslationSearchProvider? TranslationSearchProvider { get; set; } = null;

        private double _maxLyricsWidth = 0f;

        private readonly ISettingsService _settingsService;
        private readonly ILyricsSearchService _lyrcsSearchService;
        private readonly ILibWatcherService _libWatcherService;
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ITranslateService _translateService;
        private readonly ILastFMService _lastFMService;
        private readonly ILiveStatesService _liveStatesService;
        private readonly ILogger _logger;

        private readonly double _leftMargin = 36f;
        private readonly double _middleMargin = 36f;
        private readonly double _rightMargin = 36f;
        private readonly double _topMargin = 36f;
        private readonly double _bottomMargin = 36f;

        private Color _adaptiveGrayedFontColor = Colors.Transparent;
        private Color? _adaptiveColoredFontColor = null;

        private Color _albumArtLightAccentColor = Colors.Transparent;
        private Color _albumArtDarkAccentColor = Colors.Transparent;
        private Color _environmentalColor = Colors.Transparent;
        private Color _grayedEnvironmentalColor = Colors.Transparent;

        private Color _lightColor = Colors.White;
        private Color _darkColor = Colors.Black;

        private Color _bgFontColor;
        private Color _fgFontColor;
        private Color _strokeFontColor;

        private int _playingLineIndex = -1;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _isDebugOverlayEnabled = false;

        [ObservableProperty]
        public partial bool IsPlaying { get; set; } = false;

        private bool _isLyricsWindowLocked = false;
        private bool _isMouseWithinWindow = false;

        private bool _isLayoutChanged = true;

        private int _langIndex = 0;

        private List<LyricsData> _lyricsDataArr = [];

        private int _timelineSyncThreshold = 0;

        private CanvasTextFormat _lyricsTextFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
            FontSize = 12,
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
        private CanvasTextFormat _debugTextFormat = new()
        {
            FontSize = 12,
            FontWeight = FontWeights.ExtraBlack,
        };

        private LatestOnlyTaskRunner _refreshLyricsRunner = new();
        private LatestOnlyTaskRunner _showTranslationsRunner = new();

        private LyricsLayoutOrientation _lyricsLayoutOrientation;

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

        private void GetLinePlayingProgress(int lineIndex, out int charStartIndex, out int charLength, out double charProgress)
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

            double now = (double)TotalTime.TotalMilliseconds + (double)_positionOffset.TotalMilliseconds;

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

                double lineProgress = (now - line.StartMs) / (lineEndMs - line.StartMs);
                lineProgress = Math.Clamp(lineProgress, 0f, 1f);

                // 计算当前高亮到第几个字
                double charFloatIndex = lineProgress * textLength;
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
            IsPlaying = e.IsPlaying;
        }

        private void PlaybackService_TimelineChanged(object? sender, TimelineChangedEventArgs e)
        {
            var diff = Math.Abs(TotalTime.TotalMilliseconds - e.Position.TotalMilliseconds);
            if (diff >= _timelineSyncThreshold)
            {
                TotalTime = e.Position;
                if (TotalTime.TotalSeconds <= 1)
                {
                    _totalPlayingTime = TimeSpan.Zero;
                    _isLastFMTracked = false;
                }
            }
            // 大跨度，刷新布局，避免歌词不显示
            if (diff >= _timelineSyncThreshold + 5000)
            {
                _isLayoutChanged = true;
            }
        }

        private void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;

            UpdateTimelineSyncThreshold();
            UpdatePositionOffset();
            UpdateIsLastFMTrackEnabled();

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

                // 处理 Last.fm 追踪
                _totalPlayingTime = TimeSpan.Zero;
                _isLastFMTracked = false;
            }
        }

        private void PlaybackService_AlbumArtChangedChanged(object? sender, AlbumArtChangedEventArgs e)
        {
            if (e.AlbumArtSwBitmap != _albumArtSwBitmap)
            {
                _cachedAlbumArtSwBitmaps.Append(_albumArtSwBitmap);

                _lastAlbumArtSwBitmap = _albumArtSwBitmap;

                if (_cachedAlbumArtSwBitmaps.Count > 2)
                {
                    _cachedAlbumArtSwBitmaps.Dequeue()?.Dispose();
                }

                _cachedAlbumArtSwBitmaps.Append(e.AlbumArtSwBitmap);

                _albumArtSwBitmap = e.AlbumArtSwBitmap;

                if (_cachedAlbumArtSwBitmaps.Count > 2)
                {
                    _cachedAlbumArtSwBitmaps.Dequeue()?.Dispose();
                }

                _albumArtChanged = true;

                _albumArtLightAccentColor = e.AlbumArtLightAccentColor ?? Colors.Transparent;
                _albumArtDarkAccentColor = e.AlbumArtDarkAccentColor ?? Colors.Transparent;

                UpdateColorConfig();
            }
            else
            {
                e.AlbumArtSwBitmap?.Dispose();
            }
        }

        private void UpdateTranslations()
        {
            TranslationSearchProvider = null;
            _lyricsDataArr.ElementAtOrDefault(0)?.SetDisplayedTextInOriginalText();
            _isLayoutChanged = true;

            IsTranslating = true;
            if (_settingsService.AppSettings.TranslationSettings.IsTranslationEnabled)
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
            string targetLangCode = LanguageHelper.SupportedTargetLanguages[_settingsService.AppSettings.TranslationSettings.SelectedTargetLanguageIndex].Code;
            _logger.LogInformation("Target language code: {TargetLangCode}", targetLangCode);
            string? originalText = _lyricsDataArr.FirstOrDefault()?.WrappedOriginalText;
            if (originalText == null) return;

            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);
            _logger.LogInformation("Original language code: {OriginalLangCode}", originalLangCode ?? "null");

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
                    _logger.LogInformation("Found translation in lyrics data at index {FoundIndex}", found);
                    if (_settingsService.AppSettings.TranslationSettings.ShowTranslationOnly)
                    {
                        _lyricsDataArr[found].SetDisplayedTextInOriginalText();
                        _langIndex = found;
                    }
                    else
                    {
                        _lyricsDataArr[0].SetDisplayedTextAlongWith(_lyricsDataArr[found], _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsTranslationSeparator, 50);
                        _langIndex = 0;
                    }
                    TranslationSearchProvider = LyricsSearchProvider.ToTranslationSearchProvider();
                }
                else if (_settingsService.AppSettings.TranslationSettings.IsLibreTranslateEnabled)
                {
                    _logger.LogInformation("LibreTranslate is enabled, trying to translate lyrics...");
                    string translated = string.Empty;
                    try
                    {
                        translated = await _translateService.TranslateTextAsync(originalText, targetLangCode, token);
                        if (translated == string.Empty) return;

                        if (_settingsService.AppSettings.TranslationSettings.ShowTranslationOnly)
                        {
                            _lyricsDataArr[^1] = _lyricsDataArr[0].CreateLyricsDataFrom(translated);
                            _lyricsDataArr[^1].SetDisplayedTextInOriginalText();
                            _langIndex = _lyricsDataArr.Count - 1;
                        }
                        else
                        {
                            _lyricsDataArr[0].SetDisplayedTextAlongWith(translated, _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsTranslationSeparator);
                            _langIndex = 0;
                        }
                        TranslationSearchProvider = Enums.TranslationSearchProvider.LibreTranslate;
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
            LyricsSearchProvider = null;
            _logger.LogInformation("Refreshing lyrics...");

            _lyricsDataArr = [LyricsData.GetLoadingPlaceholder()];
            _isLayoutChanged = true;

            string? lyricsRaw = null;
            LyricsSearchProvider? lyricsSearchProvider = null;

            if (SongInfo != null)
            {
                _logger.LogInformation("Searching lyrics for: Title={Title}, Artist={Artist}, Album={Album}, DurationMs={DurationMs}",
                    SongInfo.Title, SongInfo.Artist, SongInfo.Album, SongInfo.DurationMs);
                (lyricsRaw, lyricsSearchProvider) = await _lyrcsSearchService.SearchAsync(
                    SongInfo.SourceAppUserModelId ?? "",
                    SongInfo.Title,
                    SongInfo.Artist,
                    SongInfo.Album ?? "",
                    SongInfo.DurationMs ?? 0,
                    token
                );
                _logger.LogInformation("Lyrics was found? {Found}, Provider: {LyricsSearchProvider}", lyricsRaw != null, lyricsSearchProvider?.ToString() ?? "null");
                LyricsSearchProvider = lyricsSearchProvider;
                token.ThrowIfCancellationRequested();
                _lyricsDataArr = new LyricsParser().Parse(lyricsRaw, (int?)SongInfo?.DurationMs);
                FillTranslationFromCache(LyricsSearchProvider);
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
                case Enums.LyricsSearchProvider.QQ:
                    translationRaw = Helper.FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, Helper.PathHelper.QQTranslationCacheDirectory);
                    break;
                case Enums.LyricsSearchProvider.Kugou:
                    break;
                case Enums.LyricsSearchProvider.Netease:
                    translationRaw = Helper.FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, Helper.PathHelper.NeteaseTranslationCacheDirectory);
                    break;
                case Enums.LyricsSearchProvider.LrcLib:
                    break;
                case Enums.LyricsSearchProvider.AmllTtmlDb:
                    break;
                case Enums.LyricsSearchProvider.LocalMusicFile:
                    break;
                case Enums.LyricsSearchProvider.LocalLrcFile:
                    break;
                case Enums.LyricsSearchProvider.LocalEslrcFile:
                    break;
                case Enums.LyricsSearchProvider.LocalTtmlFile:
                    break;
                default:
                    break;
            }
            if (translationRaw != null)
            {
                var translationData = new LyricsParser().Parse(translationRaw, (int?)SongInfo?.DurationMs);
                if (provider == Enums.LyricsSearchProvider.QQ)
                {
                    foreach (var data in translationData)
                    {
                        foreach (var item in data.LyricsLines)
                        {
                            if (item.OriginalText == "//")
                            {
                                item.OriginalText = "";
                            }
                        }
                    }
                }

                _lyricsDataArr = _lyricsDataArr.Concat(translationData).ToList();
            }
        }
    }
}
