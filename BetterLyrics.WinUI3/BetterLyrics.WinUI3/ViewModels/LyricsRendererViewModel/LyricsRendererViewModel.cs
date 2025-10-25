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
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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

        private SoftwareBitmap? _lastAlbumArtSwBitmap = null;
        private SoftwareBitmap? _albumArtSwBitmap = null;

        private CanvasBitmap? _lastAlbumArtCanvasBitmap = null;
        private CanvasBitmap? _albumArtCanvasBitmap = null;

        private CanvasRenderTarget? _albumArtBgRenderTarget;
        private CanvasRenderTarget? _albumArtRenderTarget;

        private CanvasBitmap? _coverAcrylicNoiseCanvasBitmap = null;

        private double _albumArtSize = 0f;
        private int _songInfoHeight = 0;

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

        private double _maxLyricsWidth = 0f;

        private readonly ISettingsService _settingsService;
        private readonly IMediaSessionsService _mediaSessionsService;
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

        private List<Color> _albumArtLightAccentColors = Enumerable.Repeat(Colors.Transparent, 4).ToList();
        private List<Color> _albumArtDarkAccentColors = Enumerable.Repeat(Colors.Transparent, 4).ToList();
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

        private LyricsData? _currentLyricsData;

        private bool _isDebugOverlayEnabled = false;

        [ObservableProperty]
        public partial bool IsPlaying { get; set; } = false;

        private bool _isLayoutChanged = true;

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

        //private LyricsLayoutOrientation _lyricsLayoutOrientation;

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeTypeSent { get; set; }

        private SpectrumAnalyzer? _spectrumAnalyzer;

        private Matrix4x4 _lyrics3DMatrix = Matrix4x4.Identity;

        public LyricsRendererViewModel(
            ISettingsService settingsService,
            IMediaSessionsService mediaSessionsService,
            ILastFMService lastFMService,
            ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _mediaSessionsService = mediaSessionsService;
            _liveStatesService = liveStatesService;
            _lastFMService = lastFMService;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            AppSettings = _settingsService.AppSettings;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.SongInfoAlignmentType.ToCanvasHorizontalAlignment();
            UpdateSongInfoFontSize();

            _timelineSyncThreshold = 0;

            _mediaSessionsService.IsPlayingChanged += MediaSessionsService_IsPlayingChanged;
            _mediaSessionsService.SongInfoChanged += MediaSessionsService_SongInfoChanged;
            _mediaSessionsService.AlbumArtChanged += MediaSessionsService_AlbumArtChangedChanged;
            _mediaSessionsService.LyricsChanged += MediaSessionsService_LyricsChanged;
            _mediaSessionsService.TimelineChanged += MediaSessionsService_TimelineChanged;

            IsPlaying = _mediaSessionsService.IsPlaying;

            UpdateColorConfig();

            _spectrumAnalyzer = new SpectrumAnalyzer();
        }

        private void MediaSessionsService_LyricsChanged(object? sender, LyricsChangedEventArgs e)
        {
            _currentLyricsData = e.LyricsData;
            _isLayoutChanged = true;
        }

        private int GetCurrentPlayingLineIndex()
        {
            var totalMs = TotalTime.TotalMilliseconds + _positionOffset.TotalMilliseconds;
            if (totalMs < _currentLyricsData?.LyricsLines.FirstOrDefault()?.StartMs) return 0;

            for (int i = 0; i < _currentLyricsData?.LyricsLines.Count; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;
                var nextLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i + 1);
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

            var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(lineIndex);
            if (line == null) return;
            var nextLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(lineIndex + 1);

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
                || _currentLyricsData == null
                || _currentLyricsData.LyricsLines.Count == 0
            )
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _currentLyricsData.LyricsLines.Count - 1);
        }

        private void MediaSessionsService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            IsPlaying = e.IsPlaying;
        }

        private void MediaSessionsService_TimelineChanged(object? sender, TimelineChangedEventArgs e)
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

        private void MediaSessionsService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
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

                TotalTime = TimeSpan.Zero;

                // 处理 Last.fm 追踪
                _totalPlayingTime = TimeSpan.Zero;
                _isLastFMTracked = false;
            }
        }

        private void MediaSessionsService_AlbumArtChangedChanged(object? sender, AlbumArtChangedEventArgs e)
        {
            _lastAlbumArtCanvasBitmap?.Dispose();
            _lastAlbumArtCanvasBitmap = null;

            _lastAlbumArtSwBitmap = _albumArtSwBitmap;
            _albumArtSwBitmap = e.AlbumArtSwBitmap;

            _albumArtChanged = true;

            _albumArtLightAccentColors = e.AlbumArtLightAccentColors;
            _albumArtDarkAccentColors = e.AlbumArtDarkAccentColors;

            UpdateColorConfig();
        }
    }
}
