// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Logic;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Renderer;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsCanvas : UserControl,
        IRecipient<PropertyChangedMessage<TimeSpan>>,
        IRecipient<PropertyChangedMessage<LyricsData?>>,
        IRecipient<PropertyChangedMessage<SongInfo?>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<TextAlignmentType>>,
        IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
        IRecipient<PropertyChangedMessage<string>>
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ILastFMService _lastFMService = Ioc.Default.GetRequiredService<ILastFMService>();

        private readonly LyricsRenderer _lyricsRenderer = new();
        private readonly FluidBackgroundRenderer _fluidRenderer = new();
        private readonly PureColorBackgroundRenderer _pureColorRenderer = new();
        private readonly SnowRenderer _snowRenderer = new();
        private readonly FogRenderer _fogRenderer = new();
        private readonly SpectrumRenderer _spectrumRenderer = new();

        private readonly LyricsSynchronizer _synchronizer = new();
        private readonly LyricsAnimator _animator = new();

        private readonly SpectrumAnalyzer _spectrumAnalyzer = new();

        private readonly ValueTransition<Color> _immersiveBgColorTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<double> _immersiveBgOpacityTransition = new(
            initialValue: 1f,
            durationSeconds: 0.3f
        );
        private readonly ValueTransition<Color> _accentColor1Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor2Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor3Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor4Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<double> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutSine
        );
        private readonly ValueTransition<double> _mouseYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutSine
        );

        private TimeSpan _songPositionWithOffset;
        private TimeSpan _songPosition; // 当前歌曲时刻
        private TimeSpan _totalPlayedTime; // 当前歌曲播放总时长（包括来来回回重复播放的时间）
        private bool _isLastFMTracked = false;

        private double _renderLyricsStartX = 0;
        private double _renderLyricsStartY = 0;
        private double _renderLyricsWidth = 0;
        private double _renderLyricsHeight = 0;
        private double _renderLyricsOpacity = 0;

        private LyricsWindowStatus? _lyricsWindowStatus = null;
        private AlbumArtThemeColors _albumArtThemeColors = new();

        private Point _mousePosition = new(0, 0);
        private int _mouseHoverLineIndex = -1;
        private bool _isMouseInLyricsArea = false;
        private bool _isMousePressing = false;
        private bool _isMouseScrolling = false;

        private List<RenderLyricsLine>? _renderLyricsLines = null;

        private bool _isLayoutChanged = true;
        private bool _isMouseScrollingChanged = false;

        private int _playingLineIndex;
        private (int Start, int End) _visibleRange;
        private double _canvasTargetScrollOffset;

        public TimeSpan SongPosition => _songPosition;
        public double CurrentCanvasYScroll => _canvasYScrollTransition.Value;
        public double ActualLyricsHeight => LyricsLayoutManager.CalculateActualHeight(_renderLyricsLines);
        public int CurrentHoveringLineIndex => _mouseHoverLineIndex;

        public LyricsWindowStatus? LyricsWindowStatus
        {
            get { return (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(LyricsCanvas), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public AlbumArtThemeColors AlbumArtThemeColors
        {
            get { return (AlbumArtThemeColors)GetValue(AlbumArtThemeColorsProperty); }
            set { SetValue(AlbumArtThemeColorsProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtThemeColorsProperty =
            DependencyProperty.Register(nameof(AlbumArtThemeColors), typeof(AlbumArtThemeColors), typeof(LyricsCanvas), new PropertyMetadata(new AlbumArtThemeColors(), OnDependencyPropertyChanged));

        // 歌词区域起始横 X 坐标
        public double LyricsStartX
        {
            get { return (double)GetValue(LyricsStartXProperty); }
            set { SetValue(LyricsStartXProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartXProperty =
            DependencyProperty.Register(nameof(LyricsStartX), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域起始 Y 坐标
        public double LyricsStartY
        {
            get { return (double)GetValue(LyricsStartYProperty); }
            set { SetValue(LyricsStartYProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartYProperty =
            DependencyProperty.Register(nameof(LyricsStartY), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域最大宽度
        public double LyricsWidth
        {
            get { return (double)GetValue(LyricsWidthProperty); }
            set { SetValue(LyricsWidthProperty, value); }
        }

        public static readonly DependencyProperty LyricsWidthProperty =
            DependencyProperty.Register(nameof(LyricsWidth), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域最大高度
        public double LyricsHeight
        {
            get { return (double)GetValue(LyricsHeightProperty); }
            set { SetValue(LyricsHeightProperty, value); }
        }

        public static readonly DependencyProperty LyricsHeightProperty =
            DependencyProperty.Register(nameof(LyricsHeight), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域不透明度
        public double LyricsOpacity
        {
            get { return (double)GetValue(LyricsOpacityProperty); }
            set { SetValue(LyricsOpacityProperty, value); }
        }

        public static readonly DependencyProperty LyricsOpacityProperty =
            DependencyProperty.Register(nameof(LyricsOpacity), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        /// <summary>
        /// 用户操控鼠标已滚动的距离（从 0 开始算）
        /// </summary>
        public double MouseScrollOffset
        {
            get { return (double)GetValue(MouseScrollOffsetProperty); }
            set { SetValue(MouseScrollOffsetProperty, value); }
        }

        public static readonly DependencyProperty MouseScrollOffsetProperty =
            DependencyProperty.Register(nameof(MouseScrollOffset), typeof(double), typeof(LyricsCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        /// <summary>
        /// 用户鼠标当前的位置（相对于歌词区域左上角）
        /// </summary>
        public Point MousePosition
        {
            get { return (Point)GetValue(MousePositionProperty); }
            set { SetValue(MousePositionProperty, value); }
        }

        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register(nameof(MousePosition), typeof(Point), typeof(LyricsCanvas), new PropertyMetadata(new Point(0, 0), OnDependencyPropertyChanged));

        public bool IsMouseInLyricsArea
        {
            get { return (bool)GetValue(IsMouseInLyricsAreaProperty); }
            set { SetValue(IsMouseInLyricsAreaProperty, value); }
        }

        public static readonly DependencyProperty IsMouseInLyricsAreaProperty =
            DependencyProperty.Register(nameof(IsMouseInLyricsArea), typeof(bool), typeof(LyricsCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public bool IsMousePressing
        {
            get { return (bool)GetValue(IsMousePressingProperty); }
            set { SetValue(IsMousePressingProperty, value); }
        }

        public static readonly DependencyProperty IsMousePressingProperty =
            DependencyProperty.Register(nameof(IsMousePressing), typeof(bool), typeof(LyricsCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public bool IsMouseScrolling
        {
            get { return (bool)GetValue(IsMouseScrollingProperty); }
            set { SetValue(IsMouseScrollingProperty, value); }
        }

        public static readonly DependencyProperty IsMouseScrollingProperty =
            DependencyProperty.Register(nameof(IsMouseScrolling), typeof(bool), typeof(LyricsCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public LyricsCanvas()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.RegisterAll(this);

            UpdateRenderLyricsLines();
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricsCanvas canvas)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    canvas._lyricsWindowStatus = (LyricsWindowStatus)e.NewValue;
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == LyricsStartXProperty)
                {
                    canvas._renderLyricsStartX = Convert.ToDouble(e.NewValue);
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == LyricsStartYProperty)
                {
                    canvas._renderLyricsStartY = Convert.ToDouble(e.NewValue);
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == LyricsWidthProperty)
                {
                    canvas._renderLyricsWidth = Convert.ToDouble(e.NewValue);
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == LyricsHeightProperty)
                {
                    canvas._renderLyricsHeight = Convert.ToDouble(e.NewValue);
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == LyricsOpacityProperty)
                {
                    canvas._renderLyricsOpacity = Convert.ToDouble(e.NewValue);
                    canvas._isLayoutChanged = true;
                }
                else if (e.Property == MouseScrollOffsetProperty)
                {
                    canvas._mouseYScrollTransition.StartTransition(Convert.ToDouble(e.NewValue));
                }
                else if (e.Property == MousePositionProperty)
                {
                    canvas._mousePosition = (Point)e.NewValue;
                }
                else if (e.Property == IsMouseInLyricsAreaProperty)
                {
                    canvas._isMouseInLyricsArea = (bool)e.NewValue;
                }
                else if (e.Property == IsMousePressingProperty)
                {
                    canvas._isMousePressing = (bool)e.NewValue;
                }
                else if (e.Property == IsMouseScrollingProperty)
                {
                    var value = (bool)e.NewValue;
                    if (canvas._isMouseScrolling != value)
                    {
                        canvas._isMouseScrollingChanged = true;
                    }
                    canvas._isMouseScrolling = value;
                }
                else if (e.Property == AlbumArtThemeColorsProperty)
                {
                    var albumArtThemeColors = (AlbumArtThemeColors)e.NewValue;
                    canvas._immersiveBgColorTransition.StartTransition(albumArtThemeColors.EnvColor);
                    canvas._accentColor1Transition.StartTransition(albumArtThemeColors.AccentColor1);
                    canvas._accentColor2Transition.StartTransition(albumArtThemeColors.AccentColor2);
                    canvas._accentColor3Transition.StartTransition(albumArtThemeColors.AccentColor3);
                    canvas._accentColor4Transition.StartTransition(albumArtThemeColors.AccentColor4);

                    canvas._albumArtThemeColors = albumArtThemeColors;
                    canvas._isLayoutChanged = true;
                }
            }
        }

        // ====

        private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if (_lyricsWindowStatus == null) return;

            var bounds = new Rect(0, 0, sender.Size.Width, sender.Size.Height);

            var albumArtLayout = _lyricsWindowStatus.AlbumArtLayoutSettings;
            var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
            var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
            var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;

            double songDuration = _mediaSessionsService.CurrentSongInfo?.DurationMs ?? 0;
            bool isForceWordByWord = _settingsService.AppSettings.GeneralSettings.IsForceWordByWordEffect;

            Color overlayColor;
            double finalOpacity;

            if (_lyricsWindowStatus.IsAdaptToEnvironment)
            {
                // 自适应色
                overlayColor = _immersiveBgColorTransition.Value;
                finalOpacity = _immersiveBgOpacityTransition.Value * lyricsBg.PureColorOverlayOpacity / 100.0;
            }
            else
            {
                // 专辑色
                overlayColor = _accentColor1Transition.Value;
                finalOpacity = lyricsBg.PureColorOverlayOpacity / 100.0;
            }

            _pureColorRenderer.Draw(
                args.DrawingSession,
                bounds,
                overlayColor,
                finalOpacity,
                lyricsBg.IsPureColorOverlayEnabled
            );

            _fluidRenderer.Opacity = lyricsBg.FluidOverlayOpacity / 100.0;
            _fluidRenderer.IsEnabled = lyricsBg.IsFluidOverlayEnabled;
            _fluidRenderer.Draw(sender, args.DrawingSession);

            _snowRenderer.Draw(sender, args.DrawingSession);

            _fogRenderer.Draw(sender, args.DrawingSession);

            _lyricsRenderer.Draw(
                control: sender,
                ds: args.DrawingSession,
                lines: _renderLyricsLines,
                playingLineIndex: _playingLineIndex,
                mouseHoverLineIndex: _mouseHoverLineIndex,
                isMousePressing: _isMousePressing,
                startVisibleIndex: _visibleRange.Start,
                endVisibleIndex: _visibleRange.End,
                lyricsX: _renderLyricsStartX,
                lyricsY: _renderLyricsStartY,
                lyricsWidth: _renderLyricsWidth,
                lyricsHeight: _renderLyricsHeight,
                userScrollOffset: _mouseYScrollTransition.Value,
                lyricsOpacity: _renderLyricsOpacity,
                playingLineTopOffsetFactor: lyricsStyle.PlayingLineTopOffset / 100.0,
                windowStatus: _lyricsWindowStatus,
                strokeColor: _albumArtThemeColors.StrokeFontColor,
                bgColor: _albumArtThemeColors.BgFontColor,
                fgColor: _albumArtThemeColors.FgFontColor,
                getPlaybackState: (lineIndex) =>
                {
                    if (_renderLyricsLines == null) return new LinePlaybackState();

                    var line = _renderLyricsLines.ElementAtOrDefault(lineIndex);
                    if (line == null) return new LinePlaybackState();

                    var nextLine = _renderLyricsLines.ElementAtOrDefault(lineIndex + 1);

                    return _synchronizer.GetLinePlayingProgress(
                        _songPositionWithOffset.TotalMilliseconds,
                        line,
                        nextLine,
                        songDuration,
                        isForceWordByWord
                    );
                }
            );

            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumRenderer.Draw(
                    resourceCreator: sender,
                    ds: args.DrawingSession,
                    spectrumData: _spectrumAnalyzer?.SmoothSpectrum,
                    barCount: _spectrumAnalyzer?.BarCount ?? 1,
                    isEnabled: lyricsBg.IsSpectrumOverlayEnabled,
                    placement: lyricsBg.SpectrumPlacement,
                    style: lyricsBg.SpectrumStyle,
                    canvasWidth: sender.Size.Width,
                    canvasHeight: sender.Size.Height,
                    fillColor: _albumArtThemeColors.BgFontColor
                );
            }

#if DEBUG
            //args.DrawingSession.DrawText(
            //        $"Lyrics render start pos: ({(int)_renderLyricsStartX}, {(int)_renderLyricsStartY})\n" +
            //        $"Lyrics render size: [{(int)_renderLyricsWidth} x {(int)_renderLyricsHeight}]\n" +
            //        $"Lyrics actual height: {LyricsLayoutManager.CalculateActualHeight(_renderLyricsLines)}\n" +
            //        $"Playing line (idx): {_playingLineIndex}\n" +
            //        $"Mouse hovering line (idx): {_mouseHoverLineIndex}\n" +
            //        $"Visible lines range (idx): [{_visibleRange.Start}, {_visibleRange.End}]\n" +
            //        $"Total line count: {LyricsLayoutManager.CalculateMaxRange(_renderLyricsLines).End + 1}\n" +
            //        $"Played: {_songPosition} / {TimeSpan.FromMilliseconds(_mediaSessionsService.CurrentSongInfo?.DurationMs ?? 0)}\n" +
            //        $"Y offset: {_canvasYScrollTransition.Value}\n" +
            //        $"User scroll offset: {_mouseYScrollTransition.Value}",
            //    new Vector2(0, 0), Colors.Red);
#endif

        }

        private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            if (_lyricsWindowStatus == null) return;

            var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
            var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
            var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
            var lyricsData = _mediaSessionsService.CurrentLyricsData;

            TimeSpan elapsedTime = args.Timing.ElapsedTime;

            _accentColor1Transition.Update(elapsedTime);
            _accentColor2Transition.Update(elapsedTime);
            _accentColor3Transition.Update(elapsedTime);
            _accentColor4Transition.Update(elapsedTime);

            _immersiveBgOpacityTransition.Update(elapsedTime);
            _immersiveBgColorTransition.Update(elapsedTime);

            UpdatePlaybackState(elapsedTime);

            TriggerRelayout();

            #region UpdatePlayingLineIndex

            int newPlayingIndex = _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds, lyricsData);
            bool isPlayingLineChanged = newPlayingIndex != _playingLineIndex;
            _playingLineIndex = newPlayingIndex;

            #endregion

            #region UpdateTargetScrollOffset

            if (isPlayingLineChanged || _isLayoutChanged)
            {
                var targetScroll = LyricsLayoutManager.CalculateTargetScrollOffset(_renderLyricsLines, _playingLineIndex);
                if (targetScroll.HasValue) _canvasTargetScrollOffset = targetScroll.Value;

                _canvasYScrollTransition.SetEasingType(lyricsEffect.LyricsScrollEasingType);
                _canvasYScrollTransition.SetDuration(lyricsEffect.LyricsScrollDuration / 1000.0);
                _canvasYScrollTransition.StartTransition(_canvasTargetScrollOffset, _isLayoutChanged);
            }
            _canvasYScrollTransition.Update(elapsedTime);

            #endregion

            _mouseYScrollTransition.Update(elapsedTime);

            _mouseHoverLineIndex = LyricsLayoutManager.FindMouseHoverLineIndex(
                _renderLyricsLines,
                _isMouseInLyricsArea,
                _mousePosition,
                _canvasYScrollTransition.Value + _mouseYScrollTransition.Value,
                _renderLyricsStartY,
                _renderLyricsHeight,
                lyricsStyle.PlayingLineTopOffset / 100.0
            );

            _visibleRange = LyricsLayoutManager.CalculateVisibleRange(
                _renderLyricsLines,
                _canvasYScrollTransition.Value + _mouseYScrollTransition.Value, // 当前滚动位置
                _renderLyricsStartY,
                _renderLyricsHeight,
                sender.Size.Height,
                lyricsStyle.PlayingLineTopOffset / 100.0
            );

            var maxRange = LyricsLayoutManager.CalculateMaxRange(_renderLyricsLines);

            _animator.UpdateLines(
                _renderLyricsLines,
                _isMouseScrolling ? maxRange.Start : _visibleRange.Start,
                _isMouseScrolling ? maxRange.End : _visibleRange.End,
                _playingLineIndex,
                sender.Size.Height,
                _canvasTargetScrollOffset,
                lyricsStyle.PlayingLineTopOffset / 100.0,
                _lyricsWindowStatus.LyricsEffectSettings,
                _canvasYScrollTransition,
                _albumArtThemeColors.BgFontColor,
                _albumArtThemeColors.FgFontColor,
                elapsedTime,
                _isMouseScrolling,
                _isLayoutChanged,
                isPlayingLineChanged,
                _isMouseScrollingChanged
            );

            _isMouseScrollingChanged = false;

            _lyricsRenderer.CalculateLyrics3DMatrix(
                lyricsEffect: lyricsEffect,
                lyricsX: _renderLyricsStartX,
                lyricsY: _renderLyricsStartY,
                lyricsWidth: _renderLyricsWidth,
                canvasHeight: sender.Size.Height
            );

            _isLayoutChanged = false;

            if (_fluidRenderer.IsEnabled)
            {
                _fluidRenderer.UpdateColors(
                    _accentColor1Transition.Value,
                    _accentColor2Transition.Value,
                    _accentColor3Transition.Value,
                    _accentColor4Transition.Value
                );
                _fluidRenderer.Update(elapsedTime);
            }

            _snowRenderer.IsEnabled = lyricsBg.IsSnowFlakeOverlayEnabled;
            _snowRenderer.Amount = lyricsBg.SnowFlakeOverlayAmount / 100f;
            _snowRenderer.Speed = lyricsBg.SnowFlakeOverlaySpeed;
            _snowRenderer.Update(elapsedTime.TotalSeconds);

            _fogRenderer.IsEnabled = lyricsBg.IsFogOverlayEnabled;
            _fogRenderer.Update(elapsedTime.TotalSeconds);

            if (lyricsBg.IsSpectrumOverlayEnabled && !_spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.BarCount = lyricsBg.SpectrumCount;
                _spectrumAnalyzer.StartCapture();
            }
            else if (!lyricsBg.IsSpectrumOverlayEnabled && _spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.StopCapture();
            }
            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.UpdateSmoothSpectrum();
            }
        }

        private void Canvas_Unloaded(object sender, RoutedEventArgs e)
        {
            _fluidRenderer.Dispose();
            _snowRenderer.Dispose();
            _fogRenderer.Dispose();
            _spectrumRenderer.Dispose();

            _renderLyricsLines = null;

            DisposeAnalyzer();

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        private async void Canvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(_fluidRenderer.LoadResourcesAsync().AsAsyncAction());
            _snowRenderer.LoadResources();
            _fogRenderer.LoadResources();

            _isLayoutChanged = true;
            TriggerRelayout();
        }

        // ====

        private void DisposeAnalyzer()
        {
            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.StopCapture();
            }
            _spectrumAnalyzer.Dispose();
        }

        private void TriggerRelayout()
        {
            if (_renderLyricsLines == null || !_isLayoutChanged || _lyricsWindowStatus == null) return;

            LyricsLayoutManager.MeasureAndArrange(
                resourceCreator: Canvas,
                lines: _renderLyricsLines,
                status: _lyricsWindowStatus,
                appSettings: _settingsService.AppSettings,
                canvasWidth: Canvas.Size.Width,
                canvasHeight: Canvas.Size.Height,
                lyricsWidth: _renderLyricsWidth,
                lyricsHeight: _renderLyricsHeight
            );
        }

        private void UpdatePlaybackState(TimeSpan elapsedTime)
        {
            if (_mediaSessionsService.CurrentIsPlaying)
            {
                _songPosition += elapsedTime;
                _totalPlayedTime += elapsedTime;
                _songPositionWithOffset = _songPosition + TimeSpan.FromMilliseconds(_mediaSessionsService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0);
                CheckAndScrobbleLastFM();
            }
        }

        private void CheckAndScrobbleLastFM()
        {
            bool isEnabled = _mediaSessionsService.CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
            if (!isEnabled || _isLastFMTracked) return;

            var songInfo = _mediaSessionsService.CurrentSongInfo;
            if (songInfo == null || songInfo.Duration <= 0) return;

            if (_totalPlayedTime.TotalSeconds >= songInfo.Duration * 0.5)
            {
                _isLastFMTracked = true;
                _lastFMService.TrackAsync(songInfo);
            }
        }

        private void ResetPlaybackState()
        {
            _songPosition = TimeSpan.Zero;
            _totalPlayedTime = TimeSpan.Zero;
            _isLastFMTracked = false;
        }

        private void UpdateRenderLyricsLines()
        {
            _renderLyricsLines = null;
            if (_mediaSessionsService.CurrentLyricsData is LyricsData lyricsData)
            {
                _renderLyricsLines = lyricsData.LyricsLines.Select(x => new RenderLyricsLine()
                {
                    LyricsSyllables = x.LyricsSyllables,
                    StartMs = x.StartMs,
                    EndMs = x.EndMs,
                    PhoneticText = x.PhoneticText,
                    OriginalText = x.OriginalText,
                    TranslatedText = x.TranslatedText
                }).ToList();
            }
        }

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentPosition))
                {
                    var realPosition = message.NewValue;

                    var diff = Math.Abs(_songPosition.TotalMilliseconds - realPosition.TotalMilliseconds);
                    var timelineSyncThreshold = _mediaSessionsService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

                    // 偏差 or seek
                    if (diff >= timelineSyncThreshold)
                    {
                        _songPosition = realPosition;

                        // 如果跳回了开头，重置 LastFM 统计状态
                        if (_songPosition.TotalSeconds <= 1)
                        {
                            _totalPlayedTime = TimeSpan.Zero;
                            _isLastFMTracked = false;
                        }
                    }

                    // 拖动进度条等大跨度
                    if (diff >= timelineSyncThreshold + 5000)
                    {
                        _isLayoutChanged = true;
                    }
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsData?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentLyricsData))
                {
                    UpdateRenderLyricsLines();
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<SongInfo?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentSongInfo))
                {
                    ResetPlaybackState();
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.OriginalLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontStrokeWidth))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.PlayingLineTopOffset))
                {
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDelay))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDelay))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.FanLyricsAngle))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineSpacingFactor))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.IsFanLyricsEnabled))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsBlurEffectEnabled))
                {
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TextAlignmentType> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsAlignmentType))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontWeight))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    _isLayoutChanged = true;
                }
            }
        }

    }
}
