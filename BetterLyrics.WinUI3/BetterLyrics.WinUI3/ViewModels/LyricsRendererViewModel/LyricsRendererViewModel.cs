// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        private bool _isLastFMTracked = false;

        /// <summary>
        /// This includes all the time the song has been played, even if the user seeks back.
        /// </summary>
        private TimeSpan _totalPlayingTime = TimeSpan.Zero;

        /// <summary>
        /// Refresh elapsed time for every frame by render.
        /// </summary>
        private TimeSpan _elapsedTime = TimeSpan.Zero;

        /// <summary>
        /// Get or set the current time position of the song.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        private int _songDurationMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds;

        private Stopwatch? _drawFrameStopwatch;
        private int _drawFrameCount = 0;
        private int _displayedDrawFrameCount = 0;

        private double _albumArtSize = 0f;

        [ObservableProperty] public partial double AlbumArtSize { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double AlbumArtX { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double AlbumArtY { get; set; } = 0;

        private double _canvasWidth = 0f;
        private double _canvasHeight = 0f;

        private readonly double _defaultScale = 0.75f;
        private readonly double _highlightedScale = 1.0f;

        private double _canvasTargetYScrollOffset = 0;

        private double _lyricsX = 0f;
        private double _maxLyricsWidth = 0f;
        private double _maxSongInfoWidth = 0f;

        private readonly ISettingsService _settingsService;
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ILastFMService _lastFMService;
        private readonly ILiveStatesService _liveStatesService;
        private readonly ILogger _logger;

        private double _leftMargin = 36f;
        private double _middleMargin = 36f;
        private double _rightMargin = 36f;
        private double _topMargin = 36f;
        private double _bottomMargin = 36f;

        private Color _adaptiveGrayedFontColor = Colors.Transparent;
        private Color? _adaptiveColoredFontColor = null;

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

        private int _phoneticLyricsFontSize = 18;
        private int _originalLyricsFontSize = 36;
        private int _translatedLyricsFontSize = 18;

        private LyricsFontWeight _originalLyricsFontWeight = LyricsFontWeight.Bold;

        private CanvasTextFormat _debugTextFormat = new()
        {
            FontSize = 12,
            FontWeight = FontWeights.ExtraBlack,
        };

        private CanvasGeometry? _spectrumGeometry = null;

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

            _mediaSessionsService.LyricsChanged += MediaSessionsService_LyricsChanged;

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
            var totalMs = TotalTime.TotalMilliseconds + _mediaSessionsService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0;
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

        private void GetLinePlayingProgress(int lineIndex, out int syllableStartIndex, out int syllableLength, out double syllableProgress)
        {
            syllableStartIndex = 0;
            syllableLength = 0;
            syllableProgress = 0f;

            var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(lineIndex);
            if (line == null) return;
            var nextLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(lineIndex + 1);

            int lineEndMs;
            if (line.EndMs != null) lineEndMs = line.EndMs.Value;
            else if (nextLine != null) lineEndMs = nextLine.StartMs;
            else lineEndMs = _songDurationMs;

            double now = (double)TotalTime.TotalMilliseconds + (double)(_mediaSessionsService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0);

            // 1. 还没到本句
            if (now < line.StartMs)
            {
                return;
            }

            // 2. 已经超过本句
            if (now > lineEndMs)
            {
                syllableProgress = 1f;
                syllableStartIndex = line.OriginalText.Length - 1;
                syllableLength = 1;
                return;
            }

            // 3. 有逐字时间轴
            if (line.LyricsSyllables != null && line.LyricsSyllables.Count > 1)
            {
                int charTimingsCount = line.LyricsSyllables.Count;
                for (int i = 0; i < charTimingsCount; i++)
                {
                    var timing = line.LyricsSyllables[i];
                    var nextTiming = line.LyricsSyllables.ElementAtOrDefault(i + 1);

                    int timingEndMs;
                    if (timing.EndMs != null) timingEndMs = timing.EndMs.Value;
                    else if (nextTiming != null) timingEndMs = nextTiming.StartMs;
                    else timingEndMs = lineEndMs;

                    syllableStartIndex = timing.StartIndex;
                    syllableLength = timing.Text.Length;

                    // 当前时间在某个字的高亮区间
                    if (now >= timing.StartMs && now <= timingEndMs)
                    {
                        if (timingEndMs != timing.StartMs)
                        {
                            syllableProgress = (now - timing.StartMs) / (timingEndMs - timing.StartMs);
                        }
                        else
                        {
                            syllableProgress = 0f;
                        }
                        return;
                    }
                    else if (now > timingEndMs && (nextTiming == null || now < nextTiming?.StartMs))
                    {
                        syllableProgress = 1f;
                        return;
                    }
                }
            }
            else
            {
                int textLength = line.OriginalText.Length;
                if (textLength == 0) return;

                if (_settingsService.AppSettings.GeneralSettings.IsForceWordByWordEffect)
                {
                    // 没有逐字时间轴，均匀分配每个字的高亮时间
                    double lineProgress = (now - line.StartMs) / (lineEndMs - line.StartMs);
                    lineProgress = Math.Clamp(lineProgress, 0f, 1f);

                    // 计算当前高亮到第几个字
                    double charFloatIndex = lineProgress * textLength;
                    int charIndex = (int)charFloatIndex;
                    syllableStartIndex = Math.Clamp(charIndex, 0, textLength - 1);
                    syllableLength = 1;

                    // 当前字的进度（0~1）
                    syllableProgress = charFloatIndex - charIndex;
                }
                else
                {
                    syllableStartIndex = textLength;
                    syllableProgress = 1f;
                }
            }
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (_mediaSessionsService.CurrentSongInfo == null
                || _currentLyricsData == null
                || _currentLyricsData.LyricsLines.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _currentLyricsData.LyricsLines.Count - 1);
        }
    }
}
