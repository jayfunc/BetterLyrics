// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Renderer;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpoutDx.Net.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vortice.Direct3D11;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class NowPlayingCanvas : UserControl,
        IRecipient<PropertyChangedMessage<TimeSpan>>,
        IRecipient<PropertyChangedMessage<LyricsData?>>,
        IRecipient<PropertyChangedMessage<SongInfo>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<TextAlignmentType>>,
        IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<IRandomAccessStream?>>,
        IRecipient<PropertyChangedMessage<NowPlayingPalette>>
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();

        private readonly LyricsRenderer _lyricsRenderer = new();
        private readonly FluidBackgroundRenderer _fluidRenderer = new();
        private readonly CoverBackgroundRenderer _coverRenderer = new();
        private readonly SnowRenderer _snowRenderer = new();
        private readonly FogRenderer _fogRenderer = new();
        private readonly SpectrumRenderer _spectrumRenderer = new();
        private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();

        private readonly LyricsSynchronizer _synchronizer = new();
        private readonly LyricsAnimator _animator = new();

        private readonly SpectrumAnalyzer _spectrumAnalyzer = new();

        private readonly ValueTransition<Color> _immersiveBgColorTransition = new(
            initialValue: Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<double> _immersiveBgOpacityTransition = new(
            initialValue: 1f,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            defaultTotalDuration: 0.3f
        );
        private readonly ValueTransition<Color> _accentColor1Transition = new(
            initialValue: Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor2Transition = new(
            initialValue: Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor3Transition = new(
            initialValue: Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<Color> _accentColor4Transition = new(
            initialValue: Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        private readonly ValueTransition<double> _canvasYScrollTransition = new(
            initialValue: 0f,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            defaultTotalDuration: 0.3f
        );
        private readonly ValueTransition<double> _mouseYScrollTransition = new(
            initialValue: 0f,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            defaultTotalDuration: 0.3f
        );

        private CompositionRenderer _compositionRenderer = new CompositionRenderer();
        private SpoutTextureHook _spoutHook = new SpoutTextureHook();

        private TimeSpan _songPositionWithOffset;
        private TimeSpan _songPosition; // 当前歌曲时刻

        private double _renderLyricsStartX = 0;
        private double _renderLyricsStartY = 0;
        private double _renderLyricsWidth = 0;
        private double _renderLyricsHeight = 0;
        private double _renderLyricsOpacity = 0;

        private LyricsWindowStatus? _lyricsWindowStatus = null;
        private Rect _albumArtRect = new();

        private Point _mousePosition = new(0, 0);
        private int _mouseHoverLineIndex = -1;
        private bool _isMouseInLyricsArea = false;
        private bool _isMousePressing = false;
        private bool _isMouseScrolling = false;

        private List<RenderLyricsLine>? _renderLyricsLines = null;

        private bool _isLayoutChanged = true;
        private bool _isMouseScrollingChanged = false;
        private bool _isNowPlayingPaletteChanged = false;

        private int _primaryPlayingLineIndex;
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
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingCanvas), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public Rect AlbumArtRect
        {
            get { return (Rect)GetValue(AlbumArtRectProperty); }
            set { SetValue(AlbumArtRectProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtRectProperty =
            DependencyProperty.Register(nameof(AlbumArtRect), typeof(Rect), typeof(NowPlayingCanvas), new PropertyMetadata(new Rect(), OnDependencyPropertyChanged));

        // 歌词区域起始横 X 坐标
        public double LyricsStartX
        {
            get { return (double)GetValue(LyricsStartXProperty); }
            set { SetValue(LyricsStartXProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartXProperty =
            DependencyProperty.Register(nameof(LyricsStartX), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域起始 Y 坐标
        public double LyricsStartY
        {
            get { return (double)GetValue(LyricsStartYProperty); }
            set { SetValue(LyricsStartYProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartYProperty =
            DependencyProperty.Register(nameof(LyricsStartY), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域最大宽度
        public double LyricsWidth
        {
            get { return (double)GetValue(LyricsWidthProperty); }
            set { SetValue(LyricsWidthProperty, value); }
        }

        public static readonly DependencyProperty LyricsWidthProperty =
            DependencyProperty.Register(nameof(LyricsWidth), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域最大高度
        public double LyricsHeight
        {
            get { return (double)GetValue(LyricsHeightProperty); }
            set { SetValue(LyricsHeightProperty, value); }
        }

        public static readonly DependencyProperty LyricsHeightProperty =
            DependencyProperty.Register(nameof(LyricsHeight), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        // 歌词区域不透明度
        public double LyricsOpacity
        {
            get { return (double)GetValue(LyricsOpacityProperty); }
            set { SetValue(LyricsOpacityProperty, value); }
        }

        public static readonly DependencyProperty LyricsOpacityProperty =
            DependencyProperty.Register(nameof(LyricsOpacity), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        /// <summary>
        /// 用户操控鼠标已滚动的距离（从 0 开始算）
        /// </summary>
        public double MouseScrollOffset
        {
            get { return (double)GetValue(MouseScrollOffsetProperty); }
            set { SetValue(MouseScrollOffsetProperty, value); }
        }

        public static readonly DependencyProperty MouseScrollOffsetProperty =
            DependencyProperty.Register(nameof(MouseScrollOffset), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        /// <summary>
        /// 用户鼠标当前的位置（相对于歌词区域左上角）
        /// </summary>
        public Point MousePosition
        {
            get { return (Point)GetValue(MousePositionProperty); }
            set { SetValue(MousePositionProperty, value); }
        }

        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register(nameof(MousePosition), typeof(Point), typeof(NowPlayingCanvas), new PropertyMetadata(new Point(0, 0), OnDependencyPropertyChanged));

        public bool IsMouseInLyricsArea
        {
            get { return (bool)GetValue(IsMouseInLyricsAreaProperty); }
            set { SetValue(IsMouseInLyricsAreaProperty, value); }
        }

        public static readonly DependencyProperty IsMouseInLyricsAreaProperty =
            DependencyProperty.Register(nameof(IsMouseInLyricsArea), typeof(bool), typeof(NowPlayingCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public bool IsMousePressing
        {
            get { return (bool)GetValue(IsMousePressingProperty); }
            set { SetValue(IsMousePressingProperty, value); }
        }

        public static readonly DependencyProperty IsMousePressingProperty =
            DependencyProperty.Register(nameof(IsMousePressing), typeof(bool), typeof(NowPlayingCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public bool IsMouseScrolling
        {
            get { return (bool)GetValue(IsMouseScrollingProperty); }
            set { SetValue(IsMouseScrollingProperty, value); }
        }

        public static readonly DependencyProperty IsMouseScrollingProperty =
            DependencyProperty.Register(nameof(IsMouseScrolling), typeof(bool), typeof(NowPlayingCanvas), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public NowPlayingCanvas()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NowPlayingCanvas canvas)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    canvas._lyricsWindowStatus = (LyricsWindowStatus)e.NewValue;
                    canvas._isLayoutChanged = true;
                    canvas.UpdatePalette();
                }
                else if (e.Property == AlbumArtRectProperty)
                {
                    canvas._albumArtRect = (Rect)e.NewValue;
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
                    canvas._mouseYScrollTransition.Start(Convert.ToDouble(e.NewValue));
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
            }
        }

        // ====

        private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if (_lyricsWindowStatus == null) return;

            var albumArtLayout = _lyricsWindowStatus.AlbumArtLayoutSettings;
            var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
            var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
            var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
            var albumStyle = _lyricsWindowStatus.AlbumArtLayoutSettings;

            double songDuration = _gsmtcService.CurrentSongInfo.DurationMs;

            Color overlayColor;
            double finalOpacity;

            var bounds = new Rect(0, 0, sender.Size.Width, sender.Size.Height);

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

            if (_edgeFadeMaskRenderer.Brush != null)
            {
                var finalTexture = _compositionRenderer.Render(
                    sender,
                    sender.Size,
                    sender.Dpi,
                    Colors.Transparent,
                    (ds) =>
                    {
                        using (ds.CreateLayer(_edgeFadeMaskRenderer.Brush))
                            {
                                PureColorBackgroundRenderer.Draw(
                                    ds,
                                    bounds,
                                    overlayColor,
                                    finalOpacity,
                                    lyricsBg.IsPureColorOverlayEnabled
                                );

                                _coverRenderer.Draw(sender, ds, lyricsBg.IsCoverOverlayBrethingEffectEnabled);

                                _fluidRenderer.Draw(sender, ds, lyricsBg.IsFluidOverlayBrethingEffectEnabled);

                                if (_spectrumAnalyzer.IsCapturing)
                                {
                                    _spectrumRenderer.Draw(
                                        resourceCreator: sender,
                                        ds: ds,
                                        spectrumData: _spectrumAnalyzer?.SmoothSpectrum,
                                        barCount: _spectrumAnalyzer?.BarCount ?? 1,
                                        isEnabled: lyricsBg.IsSpectrumOverlayEnabled,
                                        isGlowEffectEnabled: lyricsBg.IsSpectrumGlowEffectEnabled,
                                        isBreathingEffectEnabled: lyricsBg.IsSpectrumBrethingEffectEnabled,
                                        opacity: lyricsBg.SpectrumOpacity / 100.0f,
                                        placement: lyricsBg.SpectrumPlacement,
                                        style: lyricsBg.SpectrumStyle,
                                        canvasWidth: sender.Size.Width,
                                        canvasHeight: sender.Size.Height,
                                        fillColor: _lyricsWindowStatus.WindowPalette.SpectrumColor,
                                        albumRect: _albumArtRect,
                                        cornerRadiusPercentage: albumStyle.CoverImageRadius
                                    );
                                }

                                _snowRenderer.Draw(sender, ds, lyricsBg.IsSnowFlakeOverlayBrethingEffectEnabled);

                                _fogRenderer.Draw(sender, ds, lyricsBg.IsFogOverlayBrethingEffectEnabled);

                                _lyricsRenderer.Draw(
                                    control: sender,
                                    ds: ds,
                                    lines: _renderLyricsLines,
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
                                    currentProgressMs: _songPositionWithOffset.TotalMilliseconds);
                            }
                    });

                args.DrawingSession.DrawImage(finalTexture);

                _spoutHook?.SendTexture(finalTexture);
            }

            if (_lyricsWindowStatus.ShowDebugOverlay)
            {
                string debugText =
                    $"Spout Sender : {_spoutHook?.SenderName ?? "Disabled"}\n" +
                    $"FPS          : {(1.0 / args.Timing.ElapsedTime.TotalSeconds):00.0} (Avg: {args.Timing.UpdateCount / args.Timing.TotalTime.TotalSeconds:00.0})\n" +
                    $"----------------------------------------\n" +
                    $"Render Pos   : [{(int)_renderLyricsStartX}, {(int)_renderLyricsStartY}]\n" +
                    $"Render Size  : [{(int)_renderLyricsWidth} x {(int)_renderLyricsHeight}]\n" +
                    $"Actual Height: {LyricsLayoutManager.CalculateActualHeight(_renderLyricsLines)} px\n" +
                    $"----------------------------------------\n" +
                    $"Playing Line : #{_primaryPlayingLineIndex}\n" +
                    $"Hover Line   : #{_mouseHoverLineIndex}\n" +
                    $"Visible Range: [{_visibleRange.Start} -> {_visibleRange.End}]\n" +
                    $"Total Lines  : {LyricsLayoutManager.CalculateMaxRange(_renderLyricsLines).End + 1}\n" +
                    $"----------------------------------------\n" +
                    $"Time         : {_songPosition:mm\\:ss} / {TimeSpan.FromMilliseconds(_gsmtcService.CurrentSongInfo.DurationMs):mm\\:ss}\n" +
                    $"Y Offset     : {_canvasYScrollTransition.Value:0.00}\n" +
                    $"User Scroll  : {_mouseYScrollTransition.Value:0.00}";

                using (var format = new Microsoft.Graphics.Canvas.Text.CanvasTextFormat
                {
                    FontFamily = "Consolas",
                    FontSize = 13,
                    VerticalAlignment = Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment.Top,
                    HorizontalAlignment = Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment.Left
                })
                using (var layout = new Microsoft.Graphics.Canvas.Text.CanvasTextLayout(args.DrawingSession, debugText, format, 2000f, 2000f))
                {
                    var textBounds = layout.LayoutBounds;
                    float padding = 12f;
                    float margin = 12f;

                    float boxWidth = (float)textBounds.Width + (padding * 2);
                    float boxHeight = (float)textBounds.Height + (padding * 2);
                    float canvasWidth = (float)sender.Size.Width;

                    float xPos = canvasWidth - boxWidth - margin;
                    float yPos = margin;

                    var bgRect = new Rect(xPos, yPos, boxWidth, boxHeight);

                    args.DrawingSession.FillRectangle(bgRect, Color.FromArgb(128, 10, 10, 10));
                    args.DrawingSession.DrawRectangle(bgRect, Colors.Cyan, 1.0f);
                    args.DrawingSession.DrawTextLayout(layout, new Vector2(xPos + padding, yPos + padding), Colors.GreenYellow);
                }
            }

        }

        private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            if (_lyricsWindowStatus == null) return;

            var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
            var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
            var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
            var lyricsData = _gsmtcService.CurrentLyricsData;

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

            int primaryPlayingIndex = _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds, _renderLyricsLines);
            bool isPrimaryPlayingLineChanged = primaryPlayingIndex != _primaryPlayingLineIndex;
            _primaryPlayingLineIndex = primaryPlayingIndex;

            #endregion

            #region UpdateTargetScrollOffset

            if (isPrimaryPlayingLineChanged || _isLayoutChanged)
            {
                var targetScroll = LyricsLayoutManager.CalculateTargetScrollOffset(_renderLyricsLines, _primaryPlayingLineIndex);
                if (targetScroll.HasValue) _canvasTargetScrollOffset = targetScroll.Value;

                if (_isLayoutChanged)
                {
                    _canvasYScrollTransition.JumpTo(_canvasTargetScrollOffset);
                }
                else
                {
                    _canvasYScrollTransition.SetDurationMs(lyricsEffect.LyricsScrollDuration);
                    _canvasYScrollTransition.SetInterpolator(EasingHelper.GetInterpolatorByEasingType<double>(lyricsEffect.LyricsScrollEasingType, lyricsEffect.LyricsScrollEasingMode));
                    _canvasYScrollTransition.Start(_canvasTargetScrollOffset);
                }
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
                _primaryPlayingLineIndex,
                sender.Size.Height,
                _canvasTargetScrollOffset,
                lyricsStyle.PlayingLineTopOffset / 100.0,
                _lyricsWindowStatus.LyricsStyleSettings,
                _lyricsWindowStatus.LyricsEffectSettings,
                _canvasYScrollTransition,
                _lyricsWindowStatus.WindowPalette,
                elapsedTime,
                _isMouseScrolling,
                _isLayoutChanged,
                isPrimaryPlayingLineChanged,
                _isMouseScrollingChanged,
                _isNowPlayingPaletteChanged,
                _songPositionWithOffset.TotalMilliseconds
            );

            _isMouseScrollingChanged = false;
            _isNowPlayingPaletteChanged = false;

            _lyricsRenderer.CalculateLyrics3DMatrix(
                lyricsStyle: lyricsStyle,
                lyricsEffect: lyricsEffect,
                lyricsX: _renderLyricsStartX,
                lyricsY: _renderLyricsStartY,
                lyricsWidth: _renderLyricsWidth,
                lyricsHeight: _renderLyricsHeight,
                _isLayoutChanged
            );

            _isLayoutChanged = false;

            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.UpdateSmoothSpectrum();
            }

            _edgeFadeMaskRenderer.Update(
                sender,
                (float)sender.Size.Width,
                (float)sender.Size.Height,
                _lyricsWindowStatus.EdgeFeatheringLeft,
                _lyricsWindowStatus.EdgeFeatheringTop,
                _lyricsWindowStatus.EdgeFeatheringRight,
                _lyricsWindowStatus.EdgeFeatheringBottom
            );

            _fluidRenderer.IsEnabled = lyricsBg.IsFluidOverlayEnabled;
            _fluidRenderer.EnableLightWave = lyricsBg.IsFluidOverlayLightWaveEnabled;
            _fluidRenderer.EnableDithering = lyricsBg.IsColorDitheringEnabled;
            _fluidRenderer.Opacity = lyricsBg.FluidOverlayOpacity / 100.0;
            _fluidRenderer.Update(
                elapsedTime,
                _accentColor1Transition.Value,
                _accentColor2Transition.Value,
                _accentColor3Transition.Value,
                _accentColor4Transition.Value,
                _spectrumAnalyzer.CurrentBassEnergy,
                lyricsBg.FluidOverlayBreathingIntensity);

            _coverRenderer.IsEnabled = lyricsBg.IsCoverOverlayEnabled;
            _coverRenderer.Opacity = lyricsBg.CoverOverlayOpacity;
            _coverRenderer.BlurAmount = lyricsBg.CoverOverlayBlurAmount;
            _coverRenderer.Speed = lyricsBg.CoverOverlaySpeed;
            _coverRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.CoverOverlayBreathingIntensity);

            _snowRenderer.IsEnabled = lyricsBg.IsSnowFlakeOverlayEnabled;
            _snowRenderer.Amount = lyricsBg.SnowFlakeOverlayAmount / 100f;
            _snowRenderer.Speed = lyricsBg.SnowFlakeOverlaySpeed;
            _snowRenderer.Update(elapsedTime.TotalSeconds, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.SnowFlakeOverlayBreathingIntensity);

            _fogRenderer.IsEnabled = lyricsBg.IsFogOverlayEnabled;
            _fogRenderer.Update(elapsedTime.TotalSeconds, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.FogOverlayBreathingIntensity);

            _spectrumRenderer.Update(_spectrumAnalyzer.CurrentBassEnergy, lyricsBg.SpectrumBreathingIntensity);

            _lyricsRenderer.Update(_spectrumAnalyzer.CurrentBassEnergy, lyricsEffect.LyricsBreathingIntensity);
        }

        private void Canvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            _compositionRenderer?.Reset();

            var tasks = new Task[]
            {
                ReloadCoverBackgroundResourcesAsync()
            };
            args.TrackAsyncAction(Task.WhenAll(tasks).AsAsyncAction());

            _fluidRenderer.LoadResources();
            _snowRenderer.LoadResources();
            _fogRenderer.LoadResources();

            InitSpectrumAnalyzer();
            InitSpoutHook(sender);

            _isLayoutChanged = true;
            TriggerRelayout();
        }

        // ====

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);

            Canvas.Draw -= Canvas_Draw;
            Canvas.Update -= Canvas_Update;
            Canvas.CreateResources -= Canvas_CreateResources;

            Canvas.Paused = true;

            Canvas.RemoveFromVisualTree();
            Canvas = null;

            _fluidRenderer.Dispose();
            _coverRenderer.Dispose();
            _snowRenderer.Dispose();
            _fogRenderer.Dispose();
            _spectrumRenderer.Dispose();

            DisposeRenderLyricsLines();
            DisposeSpectrumAnalyzer();

            _edgeFadeMaskRenderer.Dispose();

            _compositionRenderer?.Dispose();
            _spoutHook?.Dispose();
        }

        // ====

        private void InitSpoutHook(CanvasAnimatedControl sender)
        {
            _spoutHook?.Dispose();
            _spoutHook = new SpoutTextureHook();
            _spoutHook.Initialize(sender.Device, $"BetterLyrics ({_lyricsWindowStatus?.GetHashCode()})");
        }

        private void InitSpectrumAnalyzer()
        {
            if (_lyricsWindowStatus == null) return;
            var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;

            _spectrumAnalyzer.BarCount = lyricsBg.SpectrumCount;
            _spectrumAnalyzer.Sensitivity = lyricsBg.SpectrumSensitivity;

            _spectrumAnalyzer.StartCapture();
        }

        private void DisposeSpectrumAnalyzer()
        {
            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumAnalyzer.StopCapture();
            }
            _spectrumAnalyzer.Dispose();
        }

        private void TriggerRelayout()
        {
            if (!_isLayoutChanged || _lyricsWindowStatus == null) return;

            DisposeRenderLyricsLines();
            _renderLyricsLines = _gsmtcService.CurrentLyricsData?.LyricsLines.Select(x => new RenderLyricsLine(x)).ToList();

            if (_renderLyricsLines == null) return;

            LyricsLayoutManager.CalculateLanes(_renderLyricsLines);

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
            if (_gsmtcService.CurrentIsPlaying)
            {
                _songPosition += elapsedTime;
                _songPositionWithOffset = _songPosition + TimeSpan.FromMilliseconds(_gsmtcService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0);
            }
        }

        private void ResetPlaybackState()
        {
            _songPosition = TimeSpan.Zero;
        }

        private async Task ReloadCoverBackgroundResourcesAsync()
        {
            if (Canvas == null || Canvas.Device == null) return;

            try
            {
                var originalStream = _gsmtcService.AlbumArtBitmapStream;
                if (originalStream == null) return;

                using (var localMemoryStream = new InMemoryRandomAccessStream())
                {
                    originalStream.Seek(0);

                    await RandomAccessStream.CopyAsync(originalStream, localMemoryStream);

                    localMemoryStream.Seek(0);

                    if (Canvas.Device == null) return;

                    CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(Canvas, localMemoryStream);
                    _coverRenderer.SetCoverBitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReloadCoverBackgroundResourcesAsync: {ex.Message}");
            }
        }

        private void DisposeRenderLyricsLines()
        {
            if (_renderLyricsLines != null)
            {
                foreach (var item in _renderLyricsLines)
                {
                    item.DisposeTextGeometry();
                    item.DisposeTextLayout();
                    item.DisposeCaches();
                }
                _renderLyricsLines = null;
            }
        }

        private void UpdatePalette()
        {
            if (_lyricsWindowStatus == null) return;

            var palette = _lyricsWindowStatus.WindowPalette;
            _immersiveBgColorTransition.Start(palette.UnderlayColor);
            _accentColor1Transition.Start(palette.AccentColor1);
            _accentColor2Transition.Start(palette.AccentColor2);
            _accentColor3Transition.Start(palette.AccentColor3);
            _accentColor4Transition.Start(palette.AccentColor4);

            _isNowPlayingPaletteChanged = true;
        }

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentPosition))
                {
                    var realPosition = message.NewValue;

                    var diff = Math.Abs(_songPosition.TotalMilliseconds - realPosition.TotalMilliseconds);
                    var timelineSyncThreshold = _gsmtcService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

                    // 偏差 or seek
                    if (diff >= timelineSyncThreshold)
                    {
                        _songPosition = realPosition;
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
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentLyricsData))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<SongInfo> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentSongInfo))
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
                else if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsOpacity))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.UnplayedOriginalLyricsOpacity))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsOpacity))
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
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DXAngle))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DYAngle))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DZAngle))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DDepth))
                {
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender == LyricsWindowStatus?.LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsBackgroundSettings.SpectrumCount))
                {
                    _spectrumAnalyzer.BarCount = message.NewValue;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.SpectrumSensitivity))
                {
                    _spectrumAnalyzer.Sensitivity = message.NewValue;
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
                else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsFadeOutEffectEnabled))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsOutOfSightEffectEnabled))
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

        public void Receive(PropertyChangedMessage<IRandomAccessStream?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapStream))
                {
                    _ = ReloadCoverBackgroundResourcesAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
        {
            if (message.Sender == _lyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowPalette))
                {
                    UpdatePalette();
                }
            }
        }
    }
}
