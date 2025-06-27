// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="LyricsRendererViewModel" />
    /// </summary>
    public partial class LyricsRendererViewModel : BaseViewModel
    {
        #region Fields

        /// <summary>
        /// Defines the _albumArtBgTransition
        /// </summary>
        private readonly ValueTransition<float> _albumArtBgTransition = new(
            initialValue: 0f,
            durationSeconds: 1.0f
        );

        /// <summary>
        /// Defines the _canvasYScrollTransition
        /// </summary>
        private readonly ValueTransition<float> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.8f,
            easingType: EasingType.SmootherStep
        );

        /// <summary>
        /// Defines the _coverRotateSpeed
        /// </summary>
        private readonly float _coverRotateSpeed = 0.003f;

        /// <summary>
        /// Defines the _defaultOpacity
        /// </summary>
        private readonly float _defaultOpacity = 0.3f;

        /// <summary>
        /// Defines the _defaultScale
        /// </summary>
        private readonly float _defaultScale = 0.75f;

        /// <summary>
        /// Defines the _highlightedOpacity
        /// </summary>
        private readonly float _highlightedOpacity = 1.0f;

        /// <summary>
        /// Defines the _highlightedScale
        /// </summary>
        private readonly float _highlightedScale = 1.0f;

        private bool _isDebugOverlayEnabled = false;

        /// <summary>
        /// Defines the _immersiveBgrTransition
        /// </summary>
        private readonly ValueTransition<Color> _immersiveBgTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) =>
                Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        /// <summary>
        /// Defines the _libWatcherService
        /// </summary>
        private readonly ILibWatcherService _libWatcherService;

        /// <summary>
        /// Defines the _limitedLineWidthTransition
        /// </summary>
        private readonly ValueTransition<float> _maxLyricsWidthTransition = new(
            initialValue: 0f,
            durationSeconds: 0.8f,
            interpolator: (from, to, progress) => to
        );

        /// <summary>
        /// Defines the _lyricsGlowEffectAmount
        /// </summary>
        private readonly float _lyricsGlowEffectAmount = 8f;

        /// <summary>
        /// Defines the _musicSearchService
        /// </summary>
        private protected readonly IMusicSearchService _musicSearchService;

        /// <summary>
        /// Defines the _playbackService
        /// </summary>
        private protected readonly IPlaybackService _playbackService;

        /// <summary>
        /// Defines the _rightMargin
        /// </summary>
        private readonly float _rightMargin = 36f;

        /// <summary>
        /// Defines the _topMargin
        /// </summary>
        private readonly float _topMargin = 0f;

        /// <summary>
        /// Defines the _albumArtAccentColor
        /// </summary>
        private Color? _albumArtAccentColor = null;

        /// <summary>
        /// Defines the _albumArtBitmap
        /// </summary>
        private SoftwareBitmap? _albumArtBitmap = null;

        /// <summary>
        /// Defines the _darkFontColor
        /// </summary>
        private Color _darkFontColor = Colors.Black;

        /// <summary>
        /// Defines the _endVisibleLineIndex
        /// </summary>
        private int _endVisibleLineIndex = -1;

        /// <summary>
        /// Defines the _fontColor
        /// </summary>
        private protected Color _fontColor;

        /// <summary>
        /// Defines the _isPlaying
        /// </summary>
        private bool _isPlaying = true;

        /// <summary>
        /// Defines the _isRelayoutNeeded
        /// </summary>
        private protected bool _isRelayoutNeeded = true;

        /// <summary>
        /// Defines the _langIndex
        /// </summary>
        private int _langIndex = 0;

        /// <summary>
        /// Defines the _lastAlbumArtBitmap
        /// </summary>
        private SoftwareBitmap? _lastAlbumArtBitmap = null;

        /// <summary>
        /// Defines the _lightFontColor
        /// </summary>
        private Color _lightFontColor = Colors.White;

        /// <summary>
        /// Defines the _multiLangLyrics
        /// </summary>
        private List<List<LyricsLine>> _multiLangLyrics = [];

        /// <summary>
        /// Defines the _rotateAngle
        /// </summary>
        private float _rotateAngle = 0f;

        /// <summary>
        /// Defines the _startVisibleLineIndex
        /// </summary>
        private int _startVisibleLineIndex = -1;

        /// <summary>
        /// Defines the _textFormat
        /// </summary>
        private protected CanvasTextFormat _textFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsRendererViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        /// <param name="playbackService">The playbackService<see cref="IPlaybackService"/></param>
        /// <param name="musicSearchService">The musicSearchService<see cref="IMusicSearchService"/></param>
        /// <param name="libWatcherService">The libWatcherService<see cref="ILibWatcherService"/></param>
        public LyricsRendererViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService,
            IMusicSearchService musicSearchService,
            ILibWatcherService libWatcherService
        )
            : base(settingsService)
        {
            _musicSearchService = musicSearchService;
            _playbackService = playbackService;
            _libWatcherService = libWatcherService;

            CoverImageRadius = _settingsService.CoverImageRadius;
            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            LyricsFontColorType = _settingsService.LyricsFontColorType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.PositionChanged += PlaybackService_PositionChanged;

            _isPlaying = _playbackService.IsPlaying;
            SongInfo = _playbackService.SongInfo;
            TotalTime = _playbackService.Position;

            UpdateFontColor();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        public int CoverImageRadius { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayBlurAmount
        /// </summary>
        public int CoverOverlayBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayOpacity
        /// </summary>
        public int CoverOverlayOpacity { get; set; }

        /// <summary>
        /// Gets or sets the DisplayType
        /// </summary>
        public LyricsDisplayType DisplayType { get; set; }

        /// <summary>
        /// Gets or sets the ElapsedTime
        /// </summary>
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether IsCoverOverlayEnabled
        /// </summary>
        public bool IsCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsDynamicCoverOverlayEnabled
        /// </summary>
        public bool IsDynamicCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsLyricsGlowEffectEnabled
        /// </summary>
        public bool IsLyricsGlowEffectEnabled { get; set; }

        /// <summary>
        /// Gets or sets the LyricsAlignmentType
        /// </summary>
        public LyricsAlignmentType LyricsAlignmentType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsBlurAmount
        /// </summary>
        public int LyricsBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontColorType
        /// </summary>
        [ObservableProperty]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontWeight
        /// </summary>
        [ObservableProperty]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        /// <summary>
        /// Gets or sets the LyricsGlowEffectScope
        /// </summary>
        public LineRenderingType LyricsGlowEffectScope { get; set; }

        /// <summary>
        /// Gets or sets the LyricsLineSpacingFactor
        /// </summary>
        [ObservableProperty]
        public partial float LyricsLineSpacingFactor { get; set; }

        /// <summary>
        /// Gets or sets the LyricsStatus
        /// </summary>
        [NotifyPropertyChangedRecipients]
        [ObservableProperty]
        public partial LyricsStatus LyricsStatus { get; set; } = LyricsStatus.Loading;

        /// <summary>
        /// Gets or sets the LyricsVerticalEdgeOpacity
        /// </summary>
        public int LyricsVerticalEdgeOpacity { get; set; }

        /// <summary>
        /// Gets or sets the SongInfo
        /// </summary>
        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        /// <summary>
        /// Gets or sets the Theme
        /// </summary>
        [ObservableProperty]
        public partial ElementTheme Theme { get; set; }

        /// <summary>
        /// Gets or sets the TotalTime
        /// </summary>
        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        private bool _isDockMode = false;

        private bool _isDesktopMode = false;

        #endregion

        #region Methods

        /// <summary>
        /// The GetCurrentPlayingLineIndex
        /// </summary>
        /// <returns>The <see cref="int"/></returns>
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
                    line.StartMs <= TotalTime.TotalMilliseconds
                    && TotalTime.TotalMilliseconds <= line.EndMs
                )
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// The GetLinePlayingProgress
        /// </summary>
        /// <param name="line">The line<see cref="LyricsLine"/></param>
        /// <returns>The <see cref="float"/></returns>
        private void GetLinePlayingProgress(
            LyricsLine line,
            out int charStartIndex,
            out int charLength,
            out float charProgress
        )
        {
            charStartIndex = 0;
            charLength = 0;
            charProgress = 0f;

            float now = (float)TotalTime.TotalMilliseconds;

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
                charLength = line.Text.Length;
            }
        }

        /// <summary>
        /// The GetMaxLyricsLineIndexBoundaries
        /// </summary>
        /// <returns>The <see cref="Tuple{int, int}"/></returns>
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

        /// <summary>
        /// The LibWatcherService_MusicLibraryFilesChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="Events.LibChangedEventArgs"/></param>
        private void LibWatcherService_MusicLibraryFilesChanged(
            object? sender,
            LibChangedEventArgs e
        )
        {
            RefreshLyricsAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// The PlaybackService_IsPlayingChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="IsPlayingChangedEventArgs"/></param>
        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            _isPlaying = e.IsPlaying;
        }

        /// <summary>
        /// The PlaybackService_PositionChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="PositionChangedEventArgs"/></param>
        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            if (Math.Abs(TotalTime.TotalMilliseconds - e.Position.TotalMilliseconds) > 100)
                TotalTime = e.Position;
        }

        /// <summary>
        /// The PlaybackService_SongInfoChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="SongInfoChangedEventArgs"/></param>
        private void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
        }

        /// <summary>
        /// Should invoke this function when:
        /// 1. The song info is changed (new song is played).
        /// 2. Lyrics search provider info is changed (change order, enable or disable any provider).
        /// 3. Local music/lyrics files are changed (added, removed, renamed)
        /// </summary>
        /// <returns></returns>
        private async Task RefreshLyricsAsync()
        {
            _multiLangLyrics = [];
            _isRelayoutNeeded = true;
            LyricsStatus = LyricsStatus.Loading;
            string? lyricsRaw = null;
            LyricsFormat? lyricsFormat = null;

            if (SongInfo != null)
            {
                (lyricsRaw, lyricsFormat) = await _musicSearchService.SearchLyricsAsync(
                    SongInfo.Title,
                    SongInfo.Artist,
                    SongInfo.Album ?? "",
                    SongInfo.DurationMs ?? 0
                );
            }

            if (lyricsRaw == null)
            {
                LyricsStatus = LyricsStatus.NotFound;
            }
            else if (SongInfo != null)
            {
                _multiLangLyrics = new LyricsParser().Parse(
                    lyricsRaw,
                    lyricsFormat,
                    SongInfo.Title,
                    SongInfo.Artist,
                    (int)(SongInfo.DurationMs ?? 0)
                );
                _isRelayoutNeeded = true;
                LyricsStatus = LyricsStatus.Found;
            }
        }

        #endregion
    }
}
