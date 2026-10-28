// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Effects;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers.Lyrics.LyricsLayoutStrategy;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.WinUI3.Renderer;
using BetterLyrics.WinUI3.Renderer.LyricsRenderer;
using BetterLyrics.WinUI3.Renderer.SemanticEffectsRenderer;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class NowPlayingCanvas : BaseUserControl,
    IRecipient<CurrentPositionChangedMessage>,
    IRecipient<CurrentLyricsDataChangedMessage>,
    IRecipient<CurrentSongInfoChangedMessage>,
    IRecipient<AlbumArtBytesChangedMessage>,
    IRecipient<NoLyricsFoundHandlingChangedMessage>,
    IRecipient<LyricsWindowPaletteChangedMessage>,
    IRecipient<LyricsStyleRelayoutChangedMessage>,
    IRecipient<LyricsFontFamilyChangedMessage>,
    IRecipient<LyricsEffectRelayoutChangedMessage>,
    IRecipient<LyricsEffectReloadChangedMessage>,
    IRecipient<SpectrumConfigChangedMessage>
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(NowPlayingCanvas), new PropertyMetadata(null, OnDependencyPropertyChanged));

    public static readonly DependencyProperty ParallaxContextProperty =
        DependencyProperty.Register(nameof(ParallaxContext), typeof(ParallaxTiltEffect), typeof(NowPlayingCanvas),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    public static readonly DependencyProperty AlbumArtRectProperty =
        DependencyProperty.Register(nameof(AlbumArtRect), typeof(Rect), typeof(NowPlayingCanvas),
            new PropertyMetadata(new Rect(), OnDependencyPropertyChanged));

    public static readonly DependencyProperty NowPlayingBarRectProperty =
        DependencyProperty.Register(nameof(NowPlayingBarRect), typeof(Rect), typeof(NowPlayingCanvas),
            new PropertyMetadata(new Rect(), OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsStartXProperty =
        DependencyProperty.Register(nameof(LyricsStartX), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsStartYProperty =
        DependencyProperty.Register(nameof(LyricsStartY), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsWidthProperty =
        DependencyProperty.Register(nameof(LyricsWidth), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsHeightProperty =
        DependencyProperty.Register(nameof(LyricsHeight), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsOpacityProperty =
        DependencyProperty.Register(nameof(LyricsOpacity), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty MouseScrollOffsetProperty =
        DependencyProperty.Register(nameof(MouseScrollOffset), typeof(double), typeof(NowPlayingCanvas),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty MousePositionProperty =
        DependencyProperty.Register(nameof(MousePosition), typeof(Point), typeof(NowPlayingCanvas),
            new PropertyMetadata(new Point(0, 0), OnDependencyPropertyChanged));

    public static readonly DependencyProperty IsMouseInLyricsAreaProperty =
        DependencyProperty.Register(nameof(IsMouseInLyricsArea), typeof(bool), typeof(NowPlayingCanvas),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    public static readonly DependencyProperty IsMousePressingProperty =
        DependencyProperty.Register(nameof(IsMousePressing), typeof(bool), typeof(NowPlayingCanvas),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    public static readonly DependencyProperty IsMouseScrollingProperty =
        DependencyProperty.Register(nameof(IsMouseScrolling), typeof(bool), typeof(NowPlayingCanvas),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    private readonly ValueTransition<AppColor> _accentColor1Transition = new(
        Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor2Transition = new(
        Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor3Transition = new(
        Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor4Transition = new(
        Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly LyricsAnimator _animator = new();

    private readonly ValueTransition<double> _canvasScrollTransition = new(
        0f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly CompositionRenderer _compositionRenderer = new();
    private readonly CoverBackgroundRenderer _coverRenderer = new();
    private readonly DesktopWallpaperBackgroundRenderer _desktopWallpaperRenderer = new();
    private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();
    private readonly FluidBackgroundRenderer _fluidRenderer = new();
    private readonly FogRenderer _fogRenderer = new();
    private readonly SemanticEffectsRenderer _semanticEffectsRenderer = new();

    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();

    private readonly ValueTransition<AppColor> _immersiveBgColorTransition = new(
        Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<double> _immersiveBgOpacityTransition = new(
        1f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly Debouncer _layoutDebouncer = new();
    private readonly Debouncer _lyricsDebouncer = new();

    private readonly LyricsRenderer _lyricsRenderer = new();

    private readonly ValueTransition<double> _mouseYScrollTransition = new(
        0f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly RaindropRenderer _raindropRenderer = new();
    private readonly Debouncer _scrollChangedDebouncer = new();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly SnowRenderer _snowRenderer = new();
    private readonly ISystemUIProvider _systemUIProvider = Ioc.Default.GetRequiredService<ISystemUIProvider>();

    private readonly ISpectrumAnalyzerProvider _spectrumAnalyzer = new SpectrumAnalyzerProvider();
    private readonly SpectrumRenderer _spectrumRenderer = new();

    private readonly LyricsSynchronizer _synchronizer = new();
    private Rect _albumArtRect;
    private double _canvasTargetScrollOffset;
    private TimeSpan _desktopWallpaperPathCheckTimer = TimeSpan.FromSeconds(1);
    private string? _loadedWallpaperPath;

    private bool _isLayoutChanged;
    private bool _isLyricsChanged = true;
    private bool _isMouseInLyricsArea;
    private bool _isMousePressing;
    private bool _isMouseScrolling;
    private bool _isMouseScrollingChanged;
    private bool _isNowPlayingPaletteChanged;

    // 翻译 / 音译开关等只改变译文显示时，原地更新歌词并播放过渡动画
    private const int PresentationFadeDurationMs = 180;

    private bool _isPresentationChanged;
    private bool _isPresentationCollapseReady;
    private bool _isPresentationAnimating;
    private bool _isSoftLayoutChanged;
    private DateTime _presentationFadeOutDeadline;
    private DateTime _presentationSlideUntil;
    private (int Start, int End) _presentationVisibleRangeBefore = (-1, -1);
    private List<(RenderLyricsLine Line, double Anchor)>? _presentationAnchorsBefore;

    private ILyricsLayoutStrategy _layoutStrategy = new HorizontalLyricsLayoutStrategy();

    private List<LyricsGapSegment>? _lyricsGaps;
    private bool _isGapListChanged;

    // 间隙检测输入（包含歌词文件自带的空行标记）
    private List<LyricsGapLine>? _gapInputs;

    // 当前动态插入的间隙占位行
    private LyricsGapSegment? _insertedGap;
    private int _insertedGapLineIndex = -1;

    // 间隙占位行插入 / 移除导致的排版变化，需要保持视觉位置并平滑滚动
    private bool _isGapLayoutChanged;
    private List<(RenderLyricsLine Line, double Center)>? _gapLayoutBefore;

    // 渲染行 -> 原始歌词行索引（间隙占位行为 -1）
    private List<int>? _renderLineSourceIndices;

    private LyricsWindowStatus? _lyricsWindowStatus;

    private Point _mousePosition = new(0, 0);

    private ParallaxTiltEffect? _parallaxContext;

    private int _primaryPlayingLineIndex;
    private double _renderLyricsHeight;

    private List<RenderLyricsLine>? _renderLyricsLines;
    private double _renderLyricsOpacity;

    private double _renderLyricsStartX;
    private double _renderLyricsStartY;
    private double _renderLyricsWidth = 9999;
    private TimeSpan _songPosition;

    private TimeSpan _songPositionWithOffset;
    private readonly ISpoutTextureProvider _spoutHook = Ioc.Default.GetRequiredService<ISpoutTextureProvider>();
    private (int Start, int End) _visibleRange;

    public TimeSpan SongPosition => _songPosition;
    public double CurrentCanvasScroll => _canvasScrollTransition.Value;
    public double ActualLyricsSize => _layoutStrategy.CalculateActualSize(_renderLyricsLines);
    public int CurrentHoveringLineIndex { get; private set; } = -1;

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ParallaxTiltEffect? ParallaxContext
    {
        get => (ParallaxTiltEffect?)GetValue(ParallaxContextProperty);
        set => SetValue(ParallaxContextProperty, value);
    }

    public Rect AlbumArtRect
    {
        get => (Rect)GetValue(AlbumArtRectProperty);
        set => SetValue(AlbumArtRectProperty, value);
    }

    public Rect NowPlayingBarRect
    {
        get => (Rect)GetValue(NowPlayingBarRectProperty);
        set => SetValue(NowPlayingBarRectProperty, value);
    }

    public double LyricsStartX
    {
        get => (double)GetValue(LyricsStartXProperty);
        set => SetValue(LyricsStartXProperty, value);
    }

    public double LyricsStartY
    {
        get => (double)GetValue(LyricsStartYProperty);
        set => SetValue(LyricsStartYProperty, value);
    }

    public double LyricsWidth
    {
        get => (double)GetValue(LyricsWidthProperty);
        set => SetValue(LyricsWidthProperty, value);
    }

    public double LyricsHeight
    {
        get => (double)GetValue(LyricsHeightProperty);
        set => SetValue(LyricsHeightProperty, value);
    }

    public double LyricsOpacity
    {
        get => (double)GetValue(LyricsOpacityProperty);
        set => SetValue(LyricsOpacityProperty, value);
    }

    public double MouseScrollOffset
    {
        get => (double)GetValue(MouseScrollOffsetProperty);
        set => SetValue(MouseScrollOffsetProperty, value);
    }

    public Point MousePosition
    {
        get => (Point)GetValue(MousePositionProperty);
        set => SetValue(MousePositionProperty, value);
    }

    public bool IsMouseInLyricsArea
    {
        get => (bool)GetValue(IsMouseInLyricsAreaProperty);
        set => SetValue(IsMouseInLyricsAreaProperty, value);
    }

    public bool IsMousePressing
    {
        get => (bool)GetValue(IsMousePressingProperty);
        set => SetValue(IsMousePressingProperty, value);
    }

    public bool IsMouseScrolling
    {
        get => (bool)GetValue(IsMouseScrollingProperty);
        set => SetValue(IsMouseScrollingProperty, value);
    }

    public NowPlayingCanvas()
    {
        InitializeComponent();
    }

    protected override void BindGeneratedControls()
    {
        Bindings?.Update();
    }

    protected override void UnbindGeneratedControls()
    {
        Bindings?.StopTracking();
    }

    private void PowerManager_DisplayStatusChanged(object sender, bool isDisplayOn)
    {
        if (Canvas != null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Canvas?.Paused = !isDisplayOn;
            });
        }
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NowPlayingCanvas canvas)
        {
            if (e.Property == LyricsWindowStatusProperty)
            {
                var oldValue = (LyricsWindowStatus?)e.OldValue;
                var newValue = (LyricsWindowStatus?)e.NewValue;

                oldValue?.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged -= canvas.LyricsLayerOrder_CollectionChanged;
                newValue?.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged += canvas.LyricsLayerOrder_CollectionChanged;

                canvas._lyricsWindowStatus = newValue;
                canvas.RequestRelayout();
                canvas.UpdatePalette();
            }
            else if (e.Property == ParallaxContextProperty)
            {
                canvas._parallaxContext = (ParallaxTiltEffect)e.NewValue;
                canvas._lyricsRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._fluidRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._coverRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._snowRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._fogRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._raindropRenderer.ParallaxContext = canvas._parallaxContext;
                canvas._spectrumRenderer.ParallaxContext = canvas._parallaxContext;
            }
            else if (e.Property == AlbumArtRectProperty)
            {
                canvas._albumArtRect = (Rect)e.NewValue;
            }
            else if (e.Property == LyricsStartXProperty)
            {
                canvas._renderLyricsStartX = Convert.ToDouble(e.NewValue);
                canvas.RequestRelayout();
            }
            else if (e.Property == LyricsStartYProperty)
            {
                canvas._renderLyricsStartY = Convert.ToDouble(e.NewValue);
                canvas.RequestRelayout();
            }
            else if (e.Property == LyricsWidthProperty)
            {
                canvas._renderLyricsWidth = Convert.ToDouble(e.NewValue);
                canvas.RequestRelayout();
            }
            else if (e.Property == LyricsHeightProperty)
            {
                canvas._renderLyricsHeight = Convert.ToDouble(e.NewValue);
                canvas.RequestRelayout();
            }
            else if (e.Property == LyricsOpacityProperty)
            {
                canvas._renderLyricsOpacity = Convert.ToDouble(e.NewValue);
                canvas.RequestRelayout();
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
                var newValue = (bool)e.NewValue;
                var oldValue = (bool)e.OldValue;
                canvas._isMouseScrolling = newValue;
                if (newValue != oldValue)
                {
                    canvas._isMouseScrollingChanged = true;
                }
            }
        }
    }

    private void LyricsLayerOrder_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        RequestRelayout();
    }

    // ====

    private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        if (_lyricsWindowStatus == null) return;

        var ds = args.DrawingSession;

        var albumArtLayout = _lyricsWindowStatus.AlbumArtLayoutSettings;
        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
        var albumStyle = _lyricsWindowStatus.AlbumArtLayoutSettings;

        var songDuration = _gsmtcService.CurrentSongInfo.DurationMs;

        AppColor overlayColor;
        double finalOpacity;

        var bounds = new Rect(0, 0, sender.Size.Width, sender.Size.Height);

        if (lyricsBg.PureColorOverlayColorType == BetterLyrics.Core.Enums.PureColorOverlayColorType.Custom)
        {
            overlayColor = lyricsBg.PureColorOverlayCustomColor;
            finalOpacity = lyricsBg.PureColorOverlayOpacity / 100.0;
        }
        else if (_lyricsWindowStatus.IsAdaptToEnvironment)
        {
            // Adapt to env color
            overlayColor = _immersiveBgColorTransition.Value;
            finalOpacity = _immersiveBgOpacityTransition.Value * lyricsBg.PureColorOverlayOpacity / 100.0;
        }
        else
        {
            // Use accent color
            overlayColor = _accentColor1Transition.Value;
            finalOpacity = lyricsBg.PureColorOverlayOpacity / 100.0;
        }

        if (_lyricsWindowStatus.IsSpoutOutputEnabled)
        {
            var finalTexture = _compositionRenderer.Render(
                sender,
                sender.Size,
                sender.Dpi,
                ColorExtensions.FromAppColor(Colors.Transparent),
                ds =>
                {
                    DrawCoreWithEdgeFeatheringHandled(sender, ds, bounds,
                        ColorExtensions.FromAppColor(overlayColor), finalOpacity, lyricsStyle, albumStyle,
                        lyricsBg);
                });

            ds.DrawImage(finalTexture);

            _spoutHook.SendTexture(finalTexture);
        }
        else
        {
            DrawCoreWithEdgeFeatheringHandled(sender, ds, bounds, ColorExtensions.FromAppColor(overlayColor),
                finalOpacity, lyricsStyle,
                albumStyle, lyricsBg);
        }

        if (_lyricsWindowStatus.ShowDebugOverlay)
        {
            ds.DrawRectangle(
                new Rect(_renderLyricsStartX, _renderLyricsStartY, _renderLyricsWidth, _renderLyricsHeight),
                ColorExtensions.FromAppColor(Colors.Cyan), 1f);
            ds.DrawLine(new Vector2(0, (float)sender.Size.Height / 2),
                new Vector2((float)sender.Size.Width, (float)sender.Size.Height / 2),
                ColorExtensions.FromAppColor(Colors.Cyan));

            var debugText =
                $"Spout Sender : {_spoutHook.SenderName ?? "Disabled"}\n" +
                $"FPS          : {1.0 / args.Timing.ElapsedTime.TotalSeconds:00.0} (Avg: {args.Timing.UpdateCount / args.Timing.TotalTime.TotalSeconds:00.0})\n" +
                $"----------------------------------------\n" +
                $"Render Pos   : [{(int)_renderLyricsStartX}, {(int)_renderLyricsStartY}]\n" +
                $"Render Size  : [{(int)_renderLyricsWidth} x {(int)_renderLyricsHeight}]\n" +
                $"Actual Size: {_layoutStrategy.CalculateActualSize(_renderLyricsLines)} px\n" +
                $"----------------------------------------\n" +
                $"Playing Line : #{_primaryPlayingLineIndex}\n" +
                $"Hover Line   : #{CurrentHoveringLineIndex}\n" +
                $"Visible Range: [{_visibleRange.Start} -> {_visibleRange.End}]\n" +
                $"Total Lines  : {LyricsLayoutStrategyBase.CalculateMaxRange(_renderLyricsLines).End + 1}\n" +
                $"----------------------------------------\n" +
                $"Time         : {_songPosition:mm\\:ss} / {TimeSpan.FromMilliseconds(_gsmtcService.CurrentSongInfo.DurationMs):mm\\:ss}\n" +
                $"Y Offset     : {_canvasScrollTransition.Value:0.00}\n" +
                $"User Scroll  : {_mouseYScrollTransition.Value:0.00}";

            using (var format = new CanvasTextFormat
            {
                FontFamily = "Consolas",
                FontSize = 13,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                HorizontalAlignment = CanvasHorizontalAlignment.Left
            })
            using (var layout =
                   new CanvasTextLayout(args.DrawingSession, debugText, format,
                       2000f, 2000f))
            {
                var textBounds = layout.LayoutBounds;
                var padding = 12f;
                var margin = 12f;

                var boxWidth = (float)textBounds.Width + padding * 2;
                var boxHeight = (float)textBounds.Height + padding * 2;
                var canvasWidth = (float)sender.Size.Width;

                var xPos = canvasWidth - boxWidth - margin;
                var yPos = margin;

                var bgRect = new Rect(xPos, yPos, boxWidth, boxHeight);

                args.DrawingSession.FillRectangle(bgRect, Color.FromArgb(128, 10, 10, 10));
                args.DrawingSession.DrawRectangle(bgRect, ColorExtensions.FromAppColor(Colors.Cyan), 1.0f);
                args.DrawingSession.DrawTextLayout(layout, new Vector2(xPos + padding, yPos + padding),
                    ColorExtensions.FromAppColor(Colors.GreenYellow));
            }

            ds.DrawCircle(_mousePosition.ToVector2(), 1f, ColorExtensions.FromAppColor(Colors.Cyan));
        }
    }

    private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
        if (_lyricsWindowStatus == null) return;

        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
        var lyricsData = _gsmtcService.CurrentLyricsData;

        var isVerticalLayoutStrategy = lyricsStyle.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        var elapsedTime = args.Timing.ElapsedTime;

        _accentColor1Transition.Update(elapsedTime);
        _accentColor2Transition.Update(elapsedTime);
        _accentColor3Transition.Update(elapsedTime);
        _accentColor4Transition.Update(elapsedTime);

        var isAccentColorsTransitioning =
            _accentColor1Transition.IsTransitioning ||
            _accentColor2Transition.IsTransitioning ||
            _accentColor3Transition.IsTransitioning ||
            _accentColor4Transition.IsTransitioning;

        _immersiveBgOpacityTransition.Update(elapsedTime);
        _immersiveBgColorTransition.Update(elapsedTime);

        UpdatePlaybackState(elapsedTime);

        var isGapLineChanged = UpdateGapLineInsertion();

        // 译文淡出完成后再收起布局，避免文字与行高同时跳变
        if (_presentationFadeOutDeadline != default && DateTime.UtcNow >= _presentationFadeOutDeadline)
        {
            _presentationFadeOutDeadline = default;
            _isPresentationCollapseReady = true;
            _isPresentationChanged = true;
        }

        TriggerRelayout();

        #region UpdatePlayingLineIndex

        var primaryPlayingIndex =
            _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds,
                _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList());
        var isPrimaryPlayingLineChanged = primaryPlayingIndex != _primaryPlayingLineIndex;
        _primaryPlayingLineIndex = primaryPlayingIndex;

        if (isPrimaryPlayingLineChanged && _primaryPlayingLineIndex >= 0 && _renderLyricsLines != null && _primaryPlayingLineIndex < _renderLyricsLines.Count)
        {
            if (_lyricsWindowStatus?.LyricsBackgroundSettings.IsSemanticEffectEnabled == true)
            {
                _semanticEffectsRenderer.PrepareLine(_renderLyricsLines[_primaryPlayingLineIndex]);
            }
        }

        #endregion

        UpdateGapIndicator();

        #region UpdateTargetScrollOffset

        if (isPrimaryPlayingLineChanged || _isLayoutChanged || isGapLineChanged || _isPresentationAnimating)
        {
            var targetScroll = _layoutStrategy.CalculateTargetScrollOffset(_renderLyricsLines, _primaryPlayingLineIndex);
            if (targetScroll.HasValue)
            {
                _canvasTargetScrollOffset = targetScroll.Value;
            }

            // 排版变化立即更新滚动基准；歌词本身的过渡由各行的 OffsetTransition 完成
            if (_isLayoutChanged || _isPresentationAnimating)
            {
                _canvasScrollTransition.JumpTo(_canvasTargetScrollOffset);
            }
            else
            {
                _canvasScrollTransition.SetDurationMs(lyricsEffect.LyricsScrollDuration);
                _canvasScrollTransition.SetInterpolator(
                    EasingHelper.GetInterpolatorByEasingType<double>(lyricsEffect.LyricsScrollEasingType,
                        lyricsEffect.LyricsScrollEasingMode));
                _canvasScrollTransition.Start(_canvasTargetScrollOffset);
            }
        }

        _canvasScrollTransition.Update(elapsedTime);

        #endregion

        _mouseYScrollTransition.Update(elapsedTime);

        CurrentHoveringLineIndex = _layoutStrategy.FindMouseHoverLineIndex(
            _renderLyricsLines,
            _isMouseInLyricsArea,
            _mousePosition.AddX(-_renderLyricsStartX).AddY(-_renderLyricsStartY),
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value,
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        // 空行占位不参与交互，避免点击时跳转到错误的时间
        if (GetSourceLineIndex(CurrentHoveringLineIndex) < 0)
            CurrentHoveringLineIndex = -1;

        _visibleRange = _layoutStrategy.CalculateVisibleRange(
            _renderLyricsLines,
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value, // 当前滚动偏移
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        var maxRange = LyricsLayoutStrategyBase.CalculateMaxRange(_renderLyricsLines);

        var isMouseScrollJustEnded = _isMouseScrollingChanged && !_isMouseScrolling;
        var shouldUpdateAllLines = _isMouseScrolling || isMouseScrollJustEnded;

        var animatorStartIndex = shouldUpdateAllLines ? maxRange.Start : _visibleRange.Start;
        var animatorEndIndex = shouldUpdateAllLines ? maxRange.End : _visibleRange.End;

        var isPresentationSlideActive = _presentationSlideUntil != default &&
                                        DateTime.UtcNow < _presentationSlideUntil;
        if (_presentationSlideUntil != default && !isPresentationSlideActive)
            _presentationSlideUntil = default;

        if ((_isPresentationAnimating || isPresentationSlideActive) &&
            !shouldUpdateAllLines && _renderLyricsLines != null)
        {
            // 过渡期间行位置仍接近旧排版，把旧可见范围和最终可见范围都纳入绘制 / 动画范围，
            // 避免视口边缘因为排版位移短暂漏画歌词。
            const int visiblePadding = 3;

            animatorStartIndex = Math.Max(0, animatorStartIndex - visiblePadding);
            animatorEndIndex = Math.Min(_renderLyricsLines.Count - 1, animatorEndIndex + visiblePadding);

            var oldRange = _presentationVisibleRangeBefore;
            if (oldRange.Start >= 0)
            {
                animatorStartIndex = Math.Min(animatorStartIndex, Math.Max(0, oldRange.Start - visiblePadding));
                animatorEndIndex = Math.Max(animatorEndIndex,
                    Math.Min(_renderLyricsLines.Count - 1, oldRange.End + visiblePadding));
            }

            _visibleRange = (animatorStartIndex, animatorEndIndex);

            if (_isPresentationAnimating)
            {
                // 过渡范围外的行直接对齐最终位置，避免它们之后进入视口时跳变
                for (var i = 0; i < _renderLyricsLines.Count; i++)
                {
                    if (i < animatorStartIndex || i > animatorEndIndex)
                        _renderLyricsLines[i].OffsetTransition.JumpTo(_canvasTargetScrollOffset);
                }
            }
        }

        _animator.UpdateLines(
            _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList(),
            animatorStartIndex,
            animatorEndIndex,
            _primaryPlayingLineIndex,
            _renderLyricsWidth,
            _renderLyricsHeight,
            _canvasTargetScrollOffset,
            lyricsStyle.PlayingLineTopOffset / 100.0,
            _lyricsWindowStatus.LyricsStyleSettings,
            _lyricsWindowStatus.LyricsEffectSettings,
            _canvasScrollTransition,
            _lyricsWindowStatus.WindowPalette,
            elapsedTime,
            _isMouseScrolling,
            _isLayoutChanged,
            isPrimaryPlayingLineChanged || isGapLineChanged || _isPresentationAnimating,
            _isMouseScrollingChanged,
            _isNowPlayingPaletteChanged,
            _songPositionWithOffset.TotalMilliseconds
        );

        _isMouseScrollingChanged = false;
        _isNowPlayingPaletteChanged = false;

        _isLayoutChanged = false;
        _isPresentationAnimating = false;

        if (_spectrumAnalyzer.IsCapturing) _spectrumAnalyzer.UpdateSmoothSpectrum();

        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled)
            _edgeFadeMaskRenderer.Update(
                sender,
                (float)sender.Size.Width,
                (float)sender.Size.Height,
                _lyricsWindowStatus.EdgeFeatheringLeft,
                _lyricsWindowStatus.EdgeFeatheringTop,
                _lyricsWindowStatus.EdgeFeatheringRight,
                _lyricsWindowStatus.EdgeFeatheringBottom
            );

        _parallaxContext?.Update();

        _fluidRenderer.IsEnabled = lyricsBg.IsFluidOverlayEnabled;
        _fluidRenderer.EnableLightWave = lyricsBg.IsFluidOverlayLightWaveEnabled;
        _fluidRenderer.EnableDithering = lyricsBg.IsColorDitheringEnabled;
        _fluidRenderer.Opacity = lyricsBg.FluidOverlayOpacity / 100.0;
        _fluidRenderer.Speed = lyricsBg.FluidOverlaySpeed / 100f;
        _fluidRenderer.IsStatic = isAccentColorsTransitioning ? false : lyricsBg.IsFluidOverlayStatic;
        _fluidRenderer.Update(
            sender, elapsedTime,
            ColorExtensions.FromAppColor(_accentColor1Transition.Value),
            ColorExtensions.FromAppColor(_accentColor2Transition.Value),
            ColorExtensions.FromAppColor(_accentColor3Transition.Value),
            ColorExtensions.FromAppColor(_accentColor4Transition.Value),
            _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.FluidOverlayBreathingIntensity,
            lyricsBg.IsFluidOverlayParallaxEnabled);

        _desktopWallpaperRenderer.IsEnabled = lyricsBg.IsDesktopWallpaperOverlayEnabled;
        _desktopWallpaperRenderer.Opacity = lyricsBg.DesktopWallpaperOverlayOpacity;
        _desktopWallpaperRenderer.BlurAmount = lyricsBg.DesktopWallpaperOverlayBlurAmount;
        UpdateDesktopWallpaperSource(elapsedTime, lyricsBg.IsDesktopWallpaperOverlayEnabled);

        _coverRenderer.IsEnabled = lyricsBg.IsCoverOverlayEnabled;
        _coverRenderer.Opacity = lyricsBg.CoverOverlayOpacity;
        _coverRenderer.BlurAmount = lyricsBg.CoverOverlayBlurAmount;
        _coverRenderer.Speed = lyricsBg.CoverOverlaySpeed;
        _coverRenderer.Update(
            sender, elapsedTime,
            _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.CoverOverlayBreathingIntensity,
            lyricsBg.IsCoverOverlayParallaxEnabled);

        _snowRenderer.IsEnabled = lyricsBg.IsSnowFlakeOverlayEnabled;
        _snowRenderer.Amount = lyricsBg.SnowFlakeOverlayAmount / 100f;
        _snowRenderer.Speed = lyricsBg.SnowFlakeOverlaySpeed;
        _snowRenderer.Update(
            sender, elapsedTime,
            _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.SnowFlakeOverlayBreathingIntensity,
            lyricsBg.IsSnowFlakeOverlayParallaxEnabled);

        _fogRenderer.IsEnabled = lyricsBg.IsFogOverlayEnabled;
        _fogRenderer.Update(
            sender, elapsedTime,
            _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.FogOverlayBreathingIntensity,
            lyricsBg.IsFogOverlayParallaxEnabled);

        _raindropRenderer.IsEnabled = lyricsBg.IsRaindropOverlayEnabled;
        _raindropRenderer.RainSpeed = lyricsBg.RaindropSpeed / 100f;
        _raindropRenderer.RainSize = lyricsBg.RaindropSize / 100f;
        _raindropRenderer.RainDensity = lyricsBg.RaindropDensity / 100f;
        _raindropRenderer.LightAngle = MathF.PI * lyricsBg.RaindropLightAngle / 180f;
        _raindropRenderer.ShadowIntensity = lyricsBg.RaindropShadowIntensity / 100f;
        _raindropRenderer.Update(
            sender, elapsedTime,
            _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.RaindropOverlayBreathingIntensity,
            lyricsBg.IsRaindropOverlayParallaxEnabled);

        _spectrumRenderer.Update(
            sender,
            lyricsBg.SpectrumPlacement, _albumArtRect, _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.SpectrumBreathingIntensity, lyricsBg.IsSpectrumOverlayParallaxEnabled);

        _semanticEffectsRenderer.Update(sender, elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, 0, false, _songPositionWithOffset.TotalMilliseconds);

        if (_renderLyricsOpacity == 1)
        {
            _lyricsRenderer.MouseHoverLineIndex = CurrentHoveringLineIndex;
            _lyricsRenderer.IsMousePressing = _isMousePressing;
            _lyricsRenderer.StartVisibleLineIndex = _visibleRange.Start;
            _lyricsRenderer.EndVisibleLineIndex = _visibleRange.End;
            _lyricsRenderer.UserScrollOffset = _mouseYScrollTransition.Value;
            _lyricsRenderer.LyricsX = _renderLyricsStartX;
            _lyricsRenderer.LyricsY = _renderLyricsStartY;
            _lyricsRenderer.LyricsWidth = _renderLyricsWidth;
            _lyricsRenderer.LyricsHeight = _renderLyricsHeight;
            _lyricsRenderer.LyricsOpacity = _renderLyricsOpacity;
            _lyricsRenderer.PlayingLineTopOffsetFactor = lyricsStyle.PlayingLineTopOffset / 100.0;
            _lyricsRenderer.CurrentProgressMs = _songPositionWithOffset.TotalMilliseconds;
            _lyricsRenderer.LyricsWindowStatus = _lyricsWindowStatus;
            _lyricsRenderer.RenderLyricsLines = _renderLyricsLines;
            _lyricsRenderer.Update(sender, elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsEffect.LyricsBreathingIntensity);
        }
    }

    private void Canvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
    {
        _compositionRenderer?.Dispose();

        var tasks = new[]
        {
            ReloadCoverBackgroundResourcesAsync()
        };
        args.TrackAsyncAction(Task.WhenAll(tasks).AsAsyncAction());

        _fluidRenderer.LoadResources();
        _snowRenderer.LoadResources();
        _fogRenderer.LoadResources();
        _raindropRenderer.LoadResources();

        _desktopWallpaperRenderer.Reset();
        _loadedWallpaperPath = null;
        _desktopWallpaperPathCheckTimer = TimeSpan.FromSeconds(1);

        InitSpectrumAnalyzer();
        InitSpoutHook(sender);

        RequestRelayout();
        TriggerRelayout();
    }

    // ====

    protected override void OnLoaded()
    {
        _monitorProvider.DisplayStatusChanged -= PowerManager_DisplayStatusChanged;
        _monitorProvider.DisplayStatusChanged += PowerManager_DisplayStatusChanged;
    }

    protected override void OnUnloaded()
    {
        _monitorProvider.DisplayStatusChanged -= PowerManager_DisplayStatusChanged;

        if (Canvas != null)
        {
            Canvas.Draw -= Canvas_Draw;
            Canvas.Update -= Canvas_Update;
            Canvas.CreateResources -= Canvas_CreateResources;

            Canvas.Paused = true;

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        _lyricsRenderer.Dispose();
        _fluidRenderer.Dispose();
        _coverRenderer.Dispose();
        _desktopWallpaperRenderer.Dispose();
        _snowRenderer.Dispose();
        _fogRenderer.Dispose();
        _raindropRenderer.Dispose();
        _spectrumRenderer.Dispose();
        _semanticEffectsRenderer.Dispose();

        DisposeRenderLyricsLines();
        DisposeSpectrumAnalyzer();

        _edgeFadeMaskRenderer.Dispose();

        _compositionRenderer?.Dispose();
        _spoutHook.Close();

        _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged -=
            LyricsLayerOrder_CollectionChanged;

        _layoutDebouncer.Dispose();
        _lyricsDebouncer.Dispose();
        _scrollChangedDebouncer.Dispose();
    }

    // ====

    public void HandlePointerEntered(PointerRoutedEventArgs e)
    {
    }

    public void HandlePointerExited(PointerRoutedEventArgs e)
    {
        IsMouseInLyricsArea = false;
        IsMousePressing = false;

        _parallaxContext?.OnPointerExited();
    }

    public void HandlePointerMoved(PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        var isInsideLyricsArea = IsPointerInsideLyricsContainer(pointerPoint.Position);

        IsMouseInLyricsArea = isInsideLyricsArea;

        if (isInsideLyricsArea)
        {
            MousePosition = pointerPoint.Position;
        }

        _parallaxContext?.OnPointerMoved(pointerPoint.Position.ToAppPoint(), ActualSize.ToAppSize());
    }

    public void HandlePointerPressed(PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        // 鼠标只有左键参与歌词交互
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse &&
            !pointerPoint.Properties.IsLeftButtonPressed) return;

        // 触摸 / 笔可能没有先触发 PointerMoved，这里同步位置，保证按下动画立即出现
        var isInsideLyricsArea = IsPointerInsideLyricsContainer(pointerPoint.Position);
        IsMouseInLyricsArea = isInsideLyricsArea;

        if (isInsideLyricsArea)
        {
            MousePosition = pointerPoint.Position;
            IsMousePressing = true;
        }
    }

    public void HandlePointerReleased(PointerRoutedEventArgs e)
    {
        IsMousePressing = false;
        var pointerPoint = e.GetCurrentPoint(this);

        // 鼠标只响应左键抬起，避免右键 / 中键误触发 seek
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse &&
            pointerPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;

        if (IsPointerInsideLyricsContainer(pointerPoint.Position))
        {
            var sourceLineIndex = GetSourceLineIndex(CurrentHoveringLineIndex);
            if (sourceLineIndex >= 0) _ = _gsmtcService.ChangeLyricsLineAsync(sourceLineIndex);
        }
    }

    public void HandlePointerWheelChanged(PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        if (!IsPointerInsideLyricsContainer(pointerPoint.Position)) return;

        IsMouseScrolling = true;

        var mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        double adjustedDelta;
        double minOffset;
        double maxOffset;

        if (isVertical)
        {
            adjustedDelta = -mouseWheelDelta;

            minOffset = 0;
            maxOffset = ActualLyricsSize;
        }
        else
        {
            adjustedDelta = mouseWheelDelta;

            minOffset = -ActualLyricsSize;
            maxOffset = 0;
        }

        var currentTotalOffset = CurrentCanvasScroll + MouseScrollOffset;

        var targetTotalOffset = currentTotalOffset + adjustedDelta;

        targetTotalOffset = Math.Clamp(targetTotalOffset, minOffset, maxOffset);

        MouseScrollOffset = targetTotalOffset - CurrentCanvasScroll;

        _ = _scrollChangedDebouncer.RunAsync(() =>
        {
            MouseScrollOffset = 0;
            IsMouseScrolling = false;
        }, 3000);
    }

    private bool IsPointerInsideLyricsContainer(Point position)
    {
        var inLyricsArea = _renderLyricsStartX <= position.X && position.X <= _renderLyricsStartX + _renderLyricsWidth &&
                           _renderLyricsStartY <= position.Y && position.Y <= _renderLyricsStartY + _renderLyricsHeight;

        var inNowPlayingBarArea = NowPlayingBarRect.Contains(position);

        return inLyricsArea && !inNowPlayingBarArea;
    }

    // ====

    private void DrawCore(ICanvasAnimatedControl sender, CanvasDrawingSession ds,
        Rect bounds, Color overlayColor, double finalOpacity,
        LyricsStyleSettings lyricsStyle, AlbumArtAreaStyleSettings albumStyle, LyricsBackgroundSettings lyricsBg)
    {
        if (_lyricsWindowStatus == null) return;

        PureColorBackgroundRenderer.Draw(
            ds,
            bounds,
            overlayColor,
            finalOpacity,
            lyricsBg.IsPureColorOverlayEnabled
        );

        _desktopWallpaperRenderer.Draw(sender, ds);

        _coverRenderer.Draw(sender, ds, lyricsBg.IsCoverOverlayBrethingEffectEnabled);

        _fluidRenderer.Draw(sender, ds, lyricsBg.IsFluidOverlayBrethingEffectEnabled);

        if (_spectrumAnalyzer.IsCapturing)
            _spectrumRenderer.Draw(
                sender,
                ds,
                _spectrumAnalyzer?.SmoothSpectrum,
                _spectrumAnalyzer?.BarCount ?? 1,
                lyricsBg.IsSpectrumOverlayEnabled,
                lyricsBg.IsSpectrumGlowEffectEnabled,
                lyricsBg.IsSpectrumBrethingEffectEnabled,
                lyricsBg.SpectrumOpacity / 100.0f,
                lyricsBg.SpectrumPlacement,
                lyricsBg.SpectrumStyle,
                sender.Size.Width,
                sender.Size.Height,
                ColorExtensions.FromAppColor(_lyricsWindowStatus.WindowPalette.SpectrumColor),
                _albumArtRect,
                albumStyle.CoverImageRadius
            );

        _snowRenderer.Draw(sender, ds, lyricsBg.IsSnowFlakeOverlayBrethingEffectEnabled);

        _fogRenderer.Draw(sender, ds, lyricsBg.IsFogOverlayBrethingEffectEnabled);

        _raindropRenderer.Draw(sender, ds, lyricsBg.IsRaindropOverlayBrethingEffectEnabled);

        _semanticEffectsRenderer.Draw(sender, ds, false);

        if (_renderLyricsOpacity == 1) _lyricsRenderer.Draw(sender, ds);
    }

    private void DrawCoreWithEdgeFeatheringHandled(ICanvasAnimatedControl sender, CanvasDrawingSession ds,
        Rect bounds, Color overlayColor, double finalOpacity,
        LyricsStyleSettings lyricsStyle, AlbumArtAreaStyleSettings albumStyle, LyricsBackgroundSettings lyricsBg)
    {
        if (_lyricsWindowStatus == null) return;

        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled && _edgeFadeMaskRenderer.Brush != null)
        {
            using (ds.CreateLayer(_edgeFadeMaskRenderer.Brush))
            {
                DrawCore(sender, ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg);
            }
        }
        else
        {
            DrawCore(sender, ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg);
        }
    }

    private void InitSpoutHook(CanvasAnimatedControl sender)
    {
        _spoutHook.Close();
        _spoutHook.Initialize(sender.Device, $"BetterLyrics ({_lyricsWindowStatus?.GetHashCode()})");
    }

    private void InitSpectrumAnalyzer()
    {
        if (_lyricsWindowStatus == null) return;
        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;

        _spectrumAnalyzer.BarCount = lyricsBg.SpectrumCount;
        _spectrumAnalyzer.Sensitivity = lyricsBg.SpectrumSensitivity;
        _spectrumAnalyzer.DelayMs = lyricsBg.SpectrumDelayMs;

        _spectrumAnalyzer.StartCapture();
    }

    private void DisposeSpectrumAnalyzer()
    {
        if (_spectrumAnalyzer.IsCapturing) _spectrumAnalyzer.StopCapture();

        _spectrumAnalyzer.Dispose();
    }

    private void TriggerRelayout()
    {
        if (_lyricsWindowStatus == null) return;

        // 翻译 / 音译开关：行结构与原文不变时原地更新，避免整页歌词重建
        if (_isPresentationChanged && !_isLyricsChanged)
        {
            var result = TryApplyPresentationChange();
            _isPresentationChanged = false;

            if (result == PresentationApplyResult.Failed) _isLyricsChanged = true;
        }

        if (_isLyricsChanged)
        {
            _isPresentationChanged = false;
            _isSoftLayoutChanged = false;
            _isPresentationAnimating = false;
            ResetPresentationTransitionState();

            DisposeRenderLyricsLines();

            // 旧列表里的间隙占位行已随 DisposeRenderLyricsLines 一起释放
            _insertedGapLineIndex = -1;
            _insertedGap = null;

            var lyricsData = _gsmtcService.CurrentLyricsData;
            if (lyricsData == LyricsDataExtensions.NotFoundPlaceholder)
            {
                var settings = _settingsService.AppSettings.GeneralSettings;
                if (settings.NoLyricsFoundHandlingType == NoLyricsFoundHandlingType.HideLyricsArea)
                {
                    lyricsData = null;
                }
                else if (!string.IsNullOrWhiteSpace(settings.CustomNotFoundMessage))
                {
                    lyricsData = new LyricsData([
                        new LyricsLine
                        {
                            StartMs = 0,
                            EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                            PrimaryText = settings.CustomNotFoundMessage,
                            PrimarySyllables =
                            [
                                new BaseLyrics { Text = settings.CustomNotFoundMessage, StartMs = 0, EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds }
                            ]
                        }
                    ]);
                }
            }

            var sourceLines = lyricsData?.LyricsLines.Select(x => new RenderLyricsLine(x)).ToList();
            if (sourceLines != null)
            {
                // 空行只作为间隙标记，不参与排版；间隙行在播放到间隙时才动态插入
                var renderLines = new List<RenderLyricsLine>(sourceLines.Count);
                var sourceIndices = new List<int>(sourceLines.Count);
                for (var i = 0; i < sourceLines.Count; i++)
                {
                    var line = sourceLines[i];
                    if (line.IsGapLine) continue;

                    renderLines.Add(line);
                    sourceIndices.Add(i);
                }

                EnsureRenderLyricsLinesPreservedAnimation(renderLines);

                // 间隙始终按完整歌词（含空行标记）计算，且不预先过滤时长
                _gapInputs = sourceLines
                    .Select(x => new LyricsGapLine(x.StartMs, x.EndMs, x.IsGapLine))
                    .ToList();

                _renderLyricsLines = renderLines;
                _renderLineSourceIndices = sourceIndices;
            }
            else
            {
                _gapInputs = null;
                _renderLyricsLines = null;
                _renderLineSourceIndices = null;
            }

            _lyricsGaps = BuildLyricsGaps();
            _isGapListChanged = false;
            _isGapLayoutChanged = false;
            _gapLayoutBefore = null;
            _isLyricsChanged = false;
            _isLayoutChanged = true;
        }
        else if (_isGapListChanged)
        {
            _lyricsGaps = BuildLyricsGaps();
            _isGapListChanged = false;
        }

        if (_renderLyricsLines == null) return;
        if (!_isLayoutChanged && !_isGapLayoutChanged && !_isSoftLayoutChanged) return;

        _layoutStrategy = _lyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation ==
            LyricsLayoutOrientation.Horizontal ? new HorizontalLyricsLayoutStrategy() : new VerticalLyricsLayoutStrategy();
        LyricsLayoutStrategyBase.CalculateLanes(_renderLyricsLines);
        LyricsLayoutStrategyBase.CalculateAlignments(_renderLyricsLines);
        _layoutStrategy.MeasureAndArrange(
            Canvas,
            _renderLyricsLines,
            _lyricsWindowStatus,
            _settingsService.AppSettings,
            Canvas.Size.Width,
            Canvas.Size.Height,
            _renderLyricsWidth,
            _renderLyricsHeight
        );

        if (_isGapLayoutChanged)
        {
            // 普通的整页重排会直接跳变，间隙占位行只在保持视觉位置的前提下平滑滚动
            if (!_isLayoutChanged) ApplyGapLayoutCompensation();
            else _gapLayoutBefore = null;

            _isGapLayoutChanged = false;
        }

        if (_isSoftLayoutChanged)
        {
            // 译文显示 / 隐藏导致行高变化：按旧位置反向补偿，再由滚动动画平滑归位
            ApplyPresentationLayoutCompensation();
            _isSoftLayoutChanged = false;
        }
    }

    /// <summary>
    /// 将新解析出的译文 / 音译文本原地写入现有歌词行。
    /// 只处理行结构与原文完全一致的“显示变化”，否则退回整页刷新。
    /// </summary>
    private PresentationApplyResult TryApplyPresentationChange()
    {
        if (_lyricsWindowStatus == null || _renderLyricsLines == null || _renderLineSourceIndices == null)
        {
            return PresentationApplyResult.Failed;
        }

        var lyricsData = _gsmtcService.CurrentLyricsData;
        if (lyricsData == null || lyricsData == LyricsDataExtensions.NotFoundPlaceholder)
        {
            return PresentationApplyResult.Failed;
        }

        var sourceLines = lyricsData.LyricsLines;
        if (_renderLineSourceIndices.Count != _renderLyricsLines.Count)
        {
            return PresentationApplyResult.Failed;
        }

        if (sourceLines.Count(x => !string.IsNullOrWhiteSpace(x.PrimaryText)) !=
            _renderLyricsLines.Count(x => !x.IsGapLine))
        {
            return PresentationApplyResult.Failed;
        }

        var hasChange = false;
        var hasRemoval = false;

        for (var i = 0; i < _renderLyricsLines.Count; i++)
        {
            var line = _renderLyricsLines[i];
            var sourceIndex = _renderLineSourceIndices[i];
            if (line.IsGapLine || sourceIndex < 0) continue;
            if (sourceIndex >= sourceLines.Count)
            {
                return PresentationApplyResult.Failed;
            }

            var source = sourceLines[sourceIndex];

            // 原文或时间轴变化不属于译文显示变化，必须整页刷新
            if (source.StartMs != line.StartMs ||
                !string.Equals(source.PrimaryText, line.PrimaryText, StringComparison.Ordinal))
            {
                return PresentationApplyResult.Failed;
            }

            if (!string.Equals(source.SecondaryText, line.SecondaryText, StringComparison.Ordinal))
            {
                hasChange = true;
                if (source.SecondaryText.Length == 0 && line.SecondaryText.Length > 0)
                {
                    hasRemoval = true;
                }
            }

            if (!string.Equals(source.TertiaryText, line.TertiaryText, StringComparison.Ordinal))
            {
                hasChange = true;
                if (source.TertiaryText.Length == 0 && line.TertiaryText.Length > 0)
                {
                    hasRemoval = true;
                }
            }
        }

        if (!hasChange)
        {
            RestoreHiddenPresentationLayers(sourceLines);
            ResetPresentationFadeState();
            return PresentationApplyResult.Applied;
        }

        // 关闭译文 / 音译时先让旧文字淡出，淡出结束后再收起它占用的行高
        if (hasRemoval && !_isPresentationCollapseReady)
        {
            BeginPresentationFadeOut(sourceLines);
            _presentationFadeOutDeadline = DateTime.UtcNow.AddMilliseconds(PresentationFadeDurationMs + 120);
            return PresentationApplyResult.Deferred;
        }

        ApplyPresentationTexts(sourceLines);
        return PresentationApplyResult.Applied;
    }

    private void BeginPresentationFadeOut(IReadOnlyList<LyricsLine> sourceLines)
    {
        for (var i = 0; i < _renderLyricsLines!.Count; i++)
        {
            var line = _renderLyricsLines[i];
            var sourceIndex = _renderLineSourceIndices![i];
            if (line.IsGapLine || sourceIndex < 0 || sourceIndex >= sourceLines.Count) continue;

            var source = sourceLines[sourceIndex];

            // 可见区从上往下轻微错开淡出，避免整片文字同时消失
            var visibleStart = _visibleRange.Start >= 0 ? _visibleRange.Start : 0;
            var delayMs = Math.Clamp((i - visibleStart) * 10, 0, 90);

            if (line.IsSecondaryVisible && line.SecondaryText.Length > 0 && source.SecondaryText.Length == 0)
            {
                line.IsSecondaryVisible = false;
                StartPresentationOpacityTransition(line.SecondaryOpacityTransition, 0, delayMs);
            }

            if (line.IsTertiaryVisible && line.TertiaryText.Length > 0 && source.TertiaryText.Length == 0)
            {
                line.IsTertiaryVisible = false;
                StartPresentationOpacityTransition(line.TertiaryOpacityTransition, 0, delayMs);
            }
        }
    }

    private void ApplyPresentationTexts(IReadOnlyList<LyricsLine> sourceLines)
    {
        // 先记录旧排版的原文位置和可见范围，排版后逐行反向补偿
        _presentationAnchorsBefore = CapturePresentationAnchors();
        _presentationVisibleRangeBefore = _visibleRange;

        // 行位移沿用歌词滚动动画的缓动 / 弹簧与逐行延迟，因此需要覆盖最长时长 + 延迟
        var effect = _lyricsWindowStatus!.LyricsEffectSettings;
        var maxScrollDuration = Math.Max(effect.LyricsScrollDuration,
            Math.Max(effect.LyricsScrollTopDuration, effect.LyricsScrollBottomDuration));
        var maxScrollDelay = Math.Max(effect.LyricsScrollTopDelay, effect.LyricsScrollBottomDelay) + 400;
        _presentationSlideUntil = DateTime.UtcNow.AddMilliseconds(maxScrollDuration + maxScrollDelay + 300);

        var style = _lyricsWindowStatus!.LyricsStyleSettings;
        var translatedOpacity = style.TranslatedLyricsOpacity / 100.0;
        var phoneticOpacity = style.PhoneticLyricsOpacity / 100.0;

        for (var i = 0; i < _renderLyricsLines!.Count; i++)
        {
            var line = _renderLyricsLines[i];
            var sourceIndex = _renderLineSourceIndices![i];
            if (line.IsGapLine || sourceIndex < 0 || sourceIndex >= sourceLines.Count) continue;

            var source = sourceLines[sourceIndex];

            if (!string.Equals(source.SecondaryText, line.SecondaryText, StringComparison.Ordinal))
            {
                line.SecondaryText = source.SecondaryText;
                // 新出现的译文从透明开始淡入
                if (source.SecondaryText.Length > 0)
                {
                    line.SecondaryOpacityTransition.JumpTo(0);
                }
            }

            if (!line.IsSecondaryVisible)
            {
                line.IsSecondaryVisible = true;
                if (source.SecondaryText.Length > 0)
                {
                    StartPresentationOpacityTransition(line.SecondaryOpacityTransition, translatedOpacity);
                }
            }

            if (!string.Equals(source.TertiaryText, line.TertiaryText, StringComparison.Ordinal))
            {
                line.TertiaryText = source.TertiaryText;
                if (source.TertiaryText.Length > 0)
                {
                    line.TertiaryOpacityTransition.JumpTo(0);
                }
            }

            if (!line.IsTertiaryVisible)
            {
                line.IsTertiaryVisible = true;
                if (source.TertiaryText.Length > 0)
                {
                    StartPresentationOpacityTransition(line.TertiaryOpacityTransition, phoneticOpacity);
                }
            }
        }

        ResetPresentationFadeState();
        _isSoftLayoutChanged = true;
        _isPresentationAnimating = true;
    }

    private void RestoreHiddenPresentationLayers(IReadOnlyList<LyricsLine> sourceLines)
    {
        if (_lyricsWindowStatus == null || _renderLyricsLines == null || _renderLineSourceIndices == null) return;

        var style = _lyricsWindowStatus.LyricsStyleSettings;
        var translatedOpacity = style.TranslatedLyricsOpacity / 100.0;
        var phoneticOpacity = style.PhoneticLyricsOpacity / 100.0;

        for (var i = 0; i < _renderLyricsLines.Count; i++)
        {
            var line = _renderLyricsLines[i];
            var sourceIndex = _renderLineSourceIndices[i];
            if (line.IsGapLine || sourceIndex < 0 || sourceIndex >= sourceLines.Count) continue;

            var source = sourceLines[sourceIndex];

            if (!line.IsSecondaryVisible && source.SecondaryText.Length > 0)
            {
                line.IsSecondaryVisible = true;
                StartPresentationOpacityTransition(line.SecondaryOpacityTransition, translatedOpacity);
            }

            if (!line.IsTertiaryVisible && source.TertiaryText.Length > 0)
            {
                line.IsTertiaryVisible = true;
                StartPresentationOpacityTransition(line.TertiaryOpacityTransition, phoneticOpacity);
            }
        }
    }

    private static void StartPresentationOpacityTransition(ValueTransition<double> transition, double target,
        int delayMs = 0)
    {
        transition.SetDurationMs(PresentationFadeDurationMs);
        transition.SetDelay(delayMs / 1000.0);
        transition.Start(target);
    }

    private void ApplyPresentationLayoutCompensation()
    {
        var before = _presentationAnchorsBefore;
        _presentationAnchorsBefore = null;
        if (before == null) return;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        foreach (var (line, oldAnchor) in before)
        {
            // 用原文实际绘制位置做补偿：行框中心会随译文高度变化，
            // 按中心补偿会让原文跳半个行高。
            var newAnchor = isVertical ? line.PrimaryPosition.X : line.PrimaryPosition.Y;
            var delta = newAnchor - oldAnchor;
            if (Math.Abs(delta) < 0.01) continue;

            line.OffsetTransition.JumpTo(line.OffsetTransition.Value - delta);
        }
    }

    private List<(RenderLyricsLine Line, double Anchor)>? CapturePresentationAnchors()
    {
        if (_renderLyricsLines == null) return null;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        var anchors = new List<(RenderLyricsLine, double)>(_renderLyricsLines.Count);
        foreach (var line in _renderLyricsLines)
        {
            anchors.Add((line, isVertical ? line.PrimaryPosition.X : line.PrimaryPosition.Y));
        }

        return anchors;
    }

    private void ResetPresentationFadeState()
    {
        _presentationFadeOutDeadline = default;
        _isPresentationCollapseReady = false;
    }

    private void ResetPresentationTransitionState()
    {
        ResetPresentationFadeState();
        _presentationAnchorsBefore = null;
        _presentationVisibleRangeBefore = (-1, -1);
        _presentationSlideUntil = default;
    }

    private void RequestRelayout()
    {
        _ = _layoutDebouncer.RunAsync(() => { _isLayoutChanged = true; });
    }

    private void RequestReloadLyrics()
    {
        _ = _lyricsDebouncer.RunAsync(() => { _isLyricsChanged = true; });
    }

    /// <summary>
    /// 只有真正处于间隙中时才把占位行插进渲染列表，其余时间歌词行保持原样。
    /// 返回本次是否改变了占位行（会推动后续歌词，需要重新排版）。
    /// </summary>
    private bool UpdateGapLineInsertion()
    {
        if (_lyricsWindowStatus == null || _renderLyricsLines == null) return false;

        var effectSettings = _lyricsWindowStatus.LyricsEffectSettings;
        var timeMs = _songPositionWithOffset.TotalMilliseconds;

        LyricsGapSegment? activeGap = null;
        if (effectSettings.IsLyricsGapDotsEnabled && _lyricsGaps != null)
        {
            foreach (var candidate in _lyricsGaps)
            {
                if (timeMs < candidate.StartMs || timeMs >= candidate.EndMs) continue;
                if (candidate.DurationMs < effectSettings.LyricsGapDotsMinimumDuration) continue;

                activeGap = candidate;
                break;
            }
        }

        var isSameGap = activeGap is { } next && _insertedGap is { } current &&
                        next.StartMs == current.StartMs && next.EndMs == current.EndMs;

        if (isSameGap) return false;
        if (activeGap is null && _insertedGap is null) return false;

        // 记录排版前各行的位置：插入 / 移除后抵消位移，歌词就不会整块硬跳
        _gapLayoutBefore = CaptureFlowCenters();

        RemoveGapLine();

        if (activeGap is { } gapToInsert) InsertGapLine(gapToInsert);

        _isGapLayoutChanged = true;
        return true;
    }

    private List<(RenderLyricsLine Line, double Center)>? CaptureFlowCenters()
    {
        if (_renderLyricsLines == null) return null;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        var centers = new List<(RenderLyricsLine, double)>(_renderLyricsLines.Count);
        foreach (var line in _renderLyricsLines)
        {
            centers.Add((line, isVertical ? line.CenterPosition.X : line.CenterPosition.Y));
        }

        return centers;
    }

    /// <summary>
    /// 排版后按位移反向补偿滚动量，使歌词保持原位，再由滚动动画平滑移动
    /// </summary>
    private void ApplyGapLayoutCompensation()
    {
        var before = _gapLayoutBefore;
        _gapLayoutBefore = null;
        if (before == null) return;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        foreach (var (line, oldCenter) in before)
        {
            var newCenter = isVertical ? line.CenterPosition.X : line.CenterPosition.Y;
            var delta = newCenter - oldCenter;
            if (Math.Abs(delta) < 0.01) continue;

            line.OffsetTransition.JumpTo(line.OffsetTransition.Value - delta);
        }
    }

    /// <summary>
    /// 间隙占位行不占行高，只作为“当前行”的锚点插入，因此不会推动任何歌词行
    /// </summary>
    private void InsertGapLine(LyricsGapSegment gap)
    {
        if (_renderLyricsLines == null || Canvas == null) return;

        var index = _renderLyricsLines.Count;
        for (var i = 0; i < _renderLyricsLines.Count; i++)
        {
            if (_renderLyricsLines[i].StartMs >= gap.EndMs)
            {
                index = i;
                break;
            }
        }

        var previousLine = index - 1 >= 0 ? _renderLyricsLines[index - 1] : null;
        var nextLine = index < _renderLyricsLines.Count ? _renderLyricsLines[index] : null;

        // 没有任何歌词行可参考时无法定位
        if (previousLine == null && nextLine == null) return;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        var lineHeight = previousLine?.PrimaryLineHeight ?? nextLine?.PrimaryLineHeight ?? 0;
        if (lineHeight <= 0)
        {
            lineHeight = (_lyricsWindowStatus?.LyricsStyleSettings.OriginalLyricsFontSize ?? 32) * 1.3;
        }

        float flowCenter;
        if (isVertical)
        {
            flowCenter = (float)(previousLine != null && nextLine != null
                ? (previousLine.TopLeftPosition.X + nextLine.BottomRightPosition.X) / 2
                : nextLine != null
                    ? nextLine.BottomRightPosition.X + lineHeight / 2
                    : previousLine!.TopLeftPosition.X - lineHeight / 2);
        }
        else
        {
            flowCenter = (float)(previousLine != null && nextLine != null
                ? (previousLine.BottomRightPosition.Y + nextLine.TopLeftPosition.Y) / 2
                : nextLine != null
                    ? nextLine.TopLeftPosition.Y - lineHeight / 2
                    : previousLine!.BottomRightPosition.Y + lineHeight / 2);
        }

        var line = CreateGapLine(gap);
        line.PlaceAsGap(Canvas, isVertical ? new Vector2(flowCenter, 0) : new Vector2(0, flowCenter));

        _renderLyricsLines.Insert(index, line);
        _renderLineSourceIndices?.Insert(index, -1);

        _insertedGapLineIndex = index;
        _insertedGap = gap;
    }

    private void RemoveGapLine()
    {
        if (_renderLyricsLines != null &&
            _insertedGapLineIndex >= 0 && _insertedGapLineIndex < _renderLyricsLines.Count)
        {
            var line = _renderLyricsLines[_insertedGapLineIndex];

            _renderLyricsLines.RemoveAt(_insertedGapLineIndex);
            _renderLineSourceIndices?.RemoveAt(_insertedGapLineIndex);

            line.DisposeTextLayout();
            line.DisposeTextGeometry();
            line.DisposeCaches();
        }

        _insertedGapLineIndex = -1;
        _insertedGap = null;
    }

    private static RenderLyricsLine CreateGapLine(LyricsGapSegment gap) =>
        new(new LyricsLine
        {
            StartMs = gap.StartMs,
            EndMs = gap.EndMs,
            PrimaryText = ""
        });

    /// <summary>
    /// 重新计算所有无歌词间隙（只在歌词变化时执行）
    /// </summary>
    private List<LyricsGapSegment>? BuildLyricsGaps()
    {
        if (_gapInputs == null || _gapInputs.Count == 0) return null;

        return LyricsGapHelper.FindGaps(_gapInputs);
    }

    /// <summary>
    /// 给渲染器提供呼吸圆点的位置
    /// </summary>
    private void UpdateGapIndicator()
    {
        _lyricsRenderer.GapIndicator = null;

        if (_insertedGap is not { } gap) return;
        if (_lyricsWindowStatus == null || _renderLyricsLines == null) return;
        if (_insertedGapLineIndex < 0 || _insertedGapLineIndex >= _renderLyricsLines.Count) return;

        var markerLine = _renderLyricsLines[_insertedGapLineIndex];
        if (markerLine == null) return;

        // 占位行自身没有动画状态，因此用相邻的真实歌词行作为滚动位移的参考
        var referenceLineIndex = _insertedGapLineIndex - 1 >= 0
            ? _insertedGapLineIndex - 1
            : _insertedGapLineIndex + 1;

        var referenceLine = GetRenderLyricsLine(referenceLineIndex);
        if (referenceLine == null) return;

        var lineHeight = referenceLine.PrimaryLineHeight ?? 0;
        if (lineHeight <= 0)
            lineHeight = _lyricsWindowStatus.LyricsStyleSettings.OriginalLyricsFontSize * 1.3;

        var isVertical = _lyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        var flowCenter = isVertical ? markerLine.CenterPosition.X : markerLine.CenterPosition.Y;

        _lyricsRenderer.GapIndicator = new GapIndicatorState(
            gap.StartMs,
            gap.EndMs,
            referenceLineIndex,
            flowCenter,
            lineHeight);
    }

    private RenderLyricsLine? GetRenderLyricsLine(int index)
    {
        if (_renderLyricsLines == null) return null;
        if (index < 0 || index >= _renderLyricsLines.Count) return null;

        return _renderLyricsLines[index];
    }

    /// <summary>
    /// 把渲染行索引映射回原始歌词行索引，间隙占位行返回 -1
    /// </summary>
    private int GetSourceLineIndex(int renderLineIndex)
    {
        if (_renderLineSourceIndices == null) return -1;
        if (renderLineIndex < 0 || renderLineIndex >= _renderLineSourceIndices.Count) return -1;

        return _renderLineSourceIndices[renderLineIndex];
    }

    private void UpdatePlaybackState(TimeSpan elapsedTime)
    {
        if (_gsmtcService.CurrentIsPlaying)
        {
            _songPosition += elapsedTime;
            _songPositionWithOffset = _songPosition +
                TimeSpan.FromMilliseconds(_gsmtcService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0);
        }
    }

    private void ResetPlaybackState()
    {
        _songPosition = TimeSpan.Zero;
    }

    private void UpdateDesktopWallpaperSource(TimeSpan elapsedTime, bool isEnabled)
    {
        if (!isEnabled)
        {
            _desktopWallpaperPathCheckTimer = TimeSpan.FromSeconds(1);
            return;
        }

        _desktopWallpaperPathCheckTimer += elapsedTime;
        if (_desktopWallpaperPathCheckTimer < TimeSpan.FromSeconds(1)) return;

        _desktopWallpaperPathCheckTimer = TimeSpan.Zero;
        RefreshDesktopWallpaperSource();
    }

    private void RefreshDesktopWallpaperSource()
    {
        if (_lyricsWindowStatus == null || Canvas == null) return;

        var path = _systemUIProvider.GetWallpaperPath(_lyricsWindowStatus.MonitorBounds);
        if (path == _loadedWallpaperPath && _desktopWallpaperRenderer.HasWallpaper) return;

        _loadedWallpaperPath = path;
        _desktopWallpaperRenderer.SetWallpaperPath(Canvas, path);
    }

    private async Task ReloadCoverBackgroundResourcesAsync()
    {
        if (Canvas == null) return;

        try
        {
            if (Canvas.Device == null) return;

            var imageBytes = _gsmtcService.AlbumArtBytes;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _coverRenderer.SetCoverBitmap(null);
                return;
            }

            using (var localMemoryStream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(localMemoryStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(imageBytes);
                    await writer.StoreAsync();
                }

                localMemoryStream.Seek(0);

                if (Canvas.Device == null) return;

                var bitmap = await CanvasBitmap.LoadAsync(Canvas, localMemoryStream);
                _coverRenderer.SetCoverBitmap(bitmap);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ReloadCoverBackgroundResourcesAsync: {ex}");
        }
    }

    private void DisposeRenderLyricsLines()
    {
        if (_renderLyricsLines != null)
        {
            foreach (var line in _renderLyricsLines)
            {
                line.DisposeTextGeometry();
                line.DisposeTextLayout();
                line.DisposeCaches();
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

    private void EnsureRenderLyricsLinesPreservedAnimation(List<RenderLyricsLine> lines)
    {
        if (_lyricsWindowStatus == null) return;

        if (!_lyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled &&
            !_lyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled)
        {
            return;
        }

        var animationPadding = (int)Time.AnimationDuration.TotalMilliseconds;
        var longSyllableThreshold = Math.Max(
            _lyricsWindowStatus.LyricsEffectSettings.LyricsScaleEffectLongSyllableDuration,
            _lyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectLongSyllableDuration
        );

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var isLastLine = i + 1 >= lines.Count;
            var nextLineStartMs = isLastLine ? (int)_gsmtcService.CurrentSongInfo.DurationMs : lines[i + 1].StartMs;

            var isLongSyllable = line.PrimaryRenderSyllables.LastOrDefault()?.DurationMs >= longSyllableThreshold;

            if (line.EndMs.HasValue && isLongSyllable)
            {
                if (isLastLine || line.EndMs > nextLineStartMs)
                {
                    line.EndMs += animationPadding;
                }
                else
                {
                    var targetEndMs = line.EndMs.Value + animationPadding;

                    line.EndMs = Math.Max(line.EndMs.Value, Math.Min(targetEndMs, nextLineStartMs));
                }
            }
        }
    }

    public void Receive(LyricsEffectRelayoutChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsEffectSettings)
        {
            RequestRelayout();
        }
    }

    public void Receive(LyricsEffectReloadChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsEffectSettings)
        {
            RequestReloadLyrics();
        }
    }

    public void Receive(LyricsStyleRelayoutChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            RequestRelayout();
        }
    }

    public void Receive(LyricsFontFamilyChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            RequestRelayout();
        }
    }

    public void Receive(SpectrumConfigChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsBackgroundSettings)
        {
            _spectrumAnalyzer.BarCount = message.Sender.SpectrumCount;
            _spectrumAnalyzer.Sensitivity = message.Sender.SpectrumSensitivity;
            _spectrumAnalyzer.DelayMs = message.Sender.SpectrumDelayMs;
        }
    }

    public void Receive(AlbumArtBytesChangedMessage message)
    {
        _ = ReloadCoverBackgroundResourcesAsync();
    }

    public void Receive(CurrentLyricsDataChangedMessage message)
    {
        // 只有译文显示变化（翻译 / 音译开关等）时原地更新，保留当前歌词行与滚动位置
        if (message.IsPresentationChange)
        {
            _isPresentationChanged = true;
            return;
        }

        _isPresentationChanged = false;
        ResetPresentationTransitionState();
        RequestReloadLyrics();
    }

    public void Receive(NoLyricsFoundHandlingChangedMessage message)
    {
        RequestReloadLyrics();
    }

    public void Receive(LyricsWindowPaletteChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus)
        {
            UpdatePalette();
        }
    }

    public void Receive(CurrentSongInfoChangedMessage message)
    {
        ResetPlaybackState();

        _isGapListChanged = true;
    }

    public void Receive(CurrentPositionChangedMessage message)
    {
        var realPosition = message.Value;

        var diff = Math.Abs(_songPosition.TotalMilliseconds - realPosition.TotalMilliseconds);
        var timelineSyncThreshold = _gsmtcService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

        // seek
        if (diff >= timelineSyncThreshold)
        {
            _songPosition = realPosition;
        }

        if (diff >= timelineSyncThreshold + 5000)
        {
            RequestRelayout();
        }
    }
}
