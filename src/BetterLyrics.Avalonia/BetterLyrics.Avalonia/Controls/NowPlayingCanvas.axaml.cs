using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Avalonia.Helpers.Lyrics.LyricsLayoutStrategy;
using BetterLyrics.Avalonia.Models.Lyrics;
using BetterLyrics.Avalonia.Renderer;
using BetterLyrics.Avalonia.Renderer.LyricsRenderer;
using BetterLyrics.Core.Effects;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Controls;

public partial class NowPlayingCanvas : UserControl,
    IRecipient<PropertyChangedMessage<TimeSpan>>,
    IRecipient<PropertyChangedMessage<LyricsData?>>,
    IRecipient<PropertyChangedMessage<SongInfo>>,
    IRecipient<PropertyChangedMessage<int>>,
    IRecipient<PropertyChangedMessage<double>>,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<TextAlignmentType>>,
    IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<NowPlayingPalette>>,
    IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>
{
    public static readonly StyledProperty<LyricsWindowStatus?> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, LyricsWindowStatus?>(nameof(LyricsWindowStatus));

    public static readonly StyledProperty<ParallaxTiltEffect?> ParallaxContextProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, ParallaxTiltEffect?>(nameof(ParallaxContext));

    public static readonly StyledProperty<Rect> AlbumArtRectProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, Rect>(nameof(AlbumArtRect));

    public static readonly StyledProperty<double> LyricsStartXProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsStartX));

    public static readonly StyledProperty<double> LyricsStartYProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsStartY));

    public static readonly StyledProperty<double> LyricsWidthProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsWidth));

    public static readonly StyledProperty<double> LyricsHeightProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsHeight));

    public static readonly StyledProperty<double> LyricsOpacityProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsOpacity));

    public static readonly StyledProperty<double> MouseScrollOffsetProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(MouseScrollOffset));

    public static readonly StyledProperty<Point> MousePositionProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, Point>(nameof(MousePosition));

    public static readonly StyledProperty<bool> IsMouseInLyricsAreaProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMouseInLyricsArea));

    public static readonly StyledProperty<bool> IsMousePressingProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMousePressing));

    public static readonly StyledProperty<bool> IsMouseScrollingProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMouseScrolling));

    private readonly Stopwatch _stopwatch = new Stopwatch();
    private TimeSpan _lastUpdateTime;

    // Transitions & Animations
    private readonly ValueTransition<AppColor> _accentColor1Transition = new(
        Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor2Transition = new(
        Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor3Transition = new(
        Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _accentColor4Transition = new(
        Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<AppColor> _immersiveBgColorTransition = new(
        Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
    );

    private readonly ValueTransition<double> _immersiveBgOpacityTransition = new(
        1f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly ValueTransition<double> _canvasScrollTransition = new(
        0f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly ValueTransition<double> _mouseYScrollTransition = new(
        0f,
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
        0.3f
    );

    private readonly LyricsAnimator _animator = new();

    // Renderers & Effects
    private readonly CompositionRenderer _compositionRenderer = new();
    private readonly CoverBackgroundRenderer _coverRenderer = new();
    private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();
    private readonly FluidBackgroundRenderer _fluidRenderer = new();
    private readonly FogRenderer _fogRenderer = new();
    private readonly LyricsRenderer _lyricsRenderer = new();
    private readonly RaindropRenderer _raindropRenderer = new();
    private readonly SnowRenderer _snowRenderer = new();
    private readonly SpectrumRenderer _spectrumRenderer = new();
    private readonly SpectrumAnalyzer _spectrumAnalyzer = new();

    // Utilities & Services
    private readonly Debouncer _layoutDebouncer = new();
    private readonly Debouncer _lyricsDebouncer = new();
    private readonly Debouncer _scrollChangedDebouncer = new();

    private readonly LyricsSynchronizer _synchronizer = new();

    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly ISpoutTextureProvider _spoutHook = Ioc.Default.GetRequiredService<ISpoutTextureProvider>();

    // State & Cache Variables
    private Rect _albumArtRect;
    private double _canvasTargetScrollOffset;

    private bool _isLayoutChanged = true;
    private bool _isLyricsChanged = true;
    private bool _isMouseInLyricsArea;
    private bool _isMousePressing;
    private bool _isMouseScrolling;
    private bool _isMouseScrollingChanged;
    private bool _isNowPlayingPaletteChanged;

    private ILyricsLayoutStrategy _layoutStrategy = new HorizontalLyricsLayoutStrategy();
    private LyricsWindowStatus? _lyricsWindowStatus;
    private ParallaxTiltEffect? _parallaxContext;

    private Point _mousePosition = new(0, 0);

    private int _primaryPlayingLineIndex;

    private double _renderLyricsHeight;
    private double _renderLyricsWidth;
    private double _renderLyricsStartX;
    private double _renderLyricsStartY;
    private double _renderLyricsOpacity;

    private List<RenderLyricsLine>? _renderLyricsLines;

    private TimeSpan _songPosition;
    private TimeSpan _songPositionWithOffset;

    private (int Start, int End) _visibleRange;

    private DispatcherTimer? _renderTimer;

    // 当前悬停行索引
    public int CurrentHoveringLineIndex { get; private set; } = -1;
    public TimeSpan SongPosition => _songPosition;
    public double CurrentCanvasScroll => _canvasScrollTransition.Value;
    public double ActualLyricsSize => _layoutStrategy.CalculateActualSize(_renderLyricsLines);

    // Standard constructor
    public NowPlayingCanvas()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);

        Loaded += UserControl_Loaded;

        // Initialize replacement render loop since CanvasAnimatedControl is unavailable
        StartRenderLoop();
    }

    // Static constructor for property change handlers in Avalonia
    static NowPlayingCanvas()
    {
        LyricsWindowStatusProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnLyricsWindowStatusChanged(e));
        ParallaxContextProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnParallaxContextChanged(e));
        LyricsStartXProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) =>
        {
            x._renderLyricsStartX = Convert.ToDouble(e.NewValue);
            x.RequestRelayout();
        });
        LyricsStartYProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) =>
        {
            x._renderLyricsStartY = Convert.ToDouble(e.NewValue);
            x.RequestRelayout();
        });
        LyricsWidthProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) =>
        {
            x._renderLyricsWidth = Convert.ToDouble(e.NewValue);
            x.RequestRelayout();
        });
        LyricsHeightProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) =>
        {
            x._renderLyricsHeight = Convert.ToDouble(e.NewValue);
            x.RequestRelayout();
        });
        LyricsOpacityProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) =>
        {
            x._renderLyricsOpacity = Convert.ToDouble(e.NewValue);
            x.RequestRelayout();
        });
    }

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ParallaxTiltEffect? ParallaxContext
    {
        get => GetValue(ParallaxContextProperty);
        set => SetValue(ParallaxContextProperty, value);
    }

    public Rect AlbumArtRect
    {
        get => GetValue(AlbumArtRectProperty);
        set => SetValue(AlbumArtRectProperty, value);
    }

    public double LyricsStartX
    {
        get => GetValue(LyricsStartXProperty);
        set => SetValue(LyricsStartXProperty, value);
    }

    public double LyricsStartY
    {
        get => GetValue(LyricsStartYProperty);
        set => SetValue(LyricsStartYProperty, value);
    }

    public double LyricsWidth
    {
        get => GetValue(LyricsWidthProperty);
        set => SetValue(LyricsWidthProperty, value);
    }

    public double LyricsHeight
    {
        get => GetValue(LyricsHeightProperty);
        set => SetValue(LyricsHeightProperty, value);
    }

    public double LyricsOpacity
    {
        get => GetValue(LyricsOpacityProperty);
        set => SetValue(LyricsOpacityProperty, value);
    }

    public double MouseScrollOffset
    {
        get => GetValue(MouseScrollOffsetProperty);
        set => SetValue(MouseScrollOffsetProperty, value);
    }

    public Point MousePosition
    {
        get => GetValue(MousePositionProperty);
        set => SetValue(MousePositionProperty, value);
    }

    public bool IsMouseInLyricsArea
    {
        get => GetValue(IsMouseInLyricsAreaProperty);
        set => SetValue(IsMouseInLyricsAreaProperty, value);
    }

    public bool IsMousePressing
    {
        get => GetValue(IsMousePressingProperty);
        set => SetValue(IsMousePressingProperty, value);
    }

    public bool IsMouseScrolling
    {
        get => GetValue(IsMouseScrollingProperty);
        set => SetValue(IsMouseScrollingProperty, value);
    }

    private void OnLyricsWindowStatusChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var oldValue = e.OldValue as LyricsWindowStatus;
        var newValue = e.NewValue as LyricsWindowStatus;

        if (oldValue != null)
        {
            oldValue.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged -= LyricsLayerOrder_CollectionChanged;
        }

        if (newValue != null)
        {
            newValue.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged += LyricsLayerOrder_CollectionChanged;
        }

        _lyricsWindowStatus = newValue;
        RequestRelayout();
        UpdatePalette();
    }

    private void OnParallaxContextChanged(AvaloniaPropertyChangedEventArgs e)
    {
        _parallaxContext = e.NewValue as ParallaxTiltEffect;
        // Update renderer parallax contexts here
    }

    // Message recipients remain largely unchanged as they are business logic
    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
        {
            if (message.PropertyName == nameof(LyricsEffectSettings.IsFanLyricsEnabled))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsBlurEffectEnabled))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsFadeOutEffectEnabled))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsOutOfSightEffectEnabled))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsScaleEffectEnabled))
                RequestReloadLyrics();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsGlowEffectEnabled))
                RequestReloadLyrics();
        }
        else if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.AutoWrap))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.UseInternalLyricsAlignment))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
                // 注意：在 Avalonia 中实现 ReloadCoverBackgroundResourcesAsync 
                // 时需要使用 Avalonia.Media.Imaging.Bitmap 替代 Win2D 的 CanvasBitmap
                _ = ReloadCoverBackgroundResourcesAsync();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineOverallSpacingFactor))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineInnerSpacingFactor))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<int> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsFontSize))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.OriginalLyricsFontSize))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsFontSize))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontStrokeWidth))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.PlayingLineTopOffset))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsOpacity))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.UnplayedOriginalLyricsOpacity))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsOpacity))
                RequestRelayout();
        }
        else if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
        {
            if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollDuration))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDuration))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDuration))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDelay))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDelay))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.FanLyricsAngle))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DXAngle))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DYAngle))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DZAngle))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DDepth))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScaleEffectLongSyllableDuration))
                RequestReloadLyrics();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsGlowEffectLongSyllableDuration))
                RequestReloadLyrics();
        }
        else if (message.Sender == LyricsWindowStatus?.LyricsBackgroundSettings)
        {
            if (message.PropertyName == nameof(LyricsBackgroundSettings.SpectrumCount))
                _spectrumAnalyzer.BarCount = message.NewValue;
            else if (message.PropertyName == nameof(LyricsBackgroundSettings.SpectrumSensitivity))
                _spectrumAnalyzer.Sensitivity = message.NewValue;
        }
    }

    public void Receive(PropertyChangedMessage<LyricsData?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.CurrentLyricsData))
                RequestReloadLyrics();
    }

    public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontWeight))
                RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLayoutOrientation))
                RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
    {
        if (message.Sender == _lyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.WindowPalette))
                UpdatePalette();
    }

    public void Receive(PropertyChangedMessage<SongInfo> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.CurrentSongInfo))
                ResetPlaybackState();
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<TextAlignmentType> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsAlignmentType))
                RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<TimeSpan> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.CurrentPosition))
            {
                var realPosition = message.NewValue;

                var diff = Math.Abs(_songPosition.TotalMilliseconds - realPosition.TotalMilliseconds);
                var timelineSyncThreshold = _gsmtcService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

                // 偏移或 seek
                if (diff >= timelineSyncThreshold) _songPosition = realPosition;

                // 拖动进度条需要重新布局
                if (diff >= timelineSyncThreshold + 5000) RequestRelayout();
            }
    }

    private void RequestReloadLyrics()
    {
        _ = _lyricsDebouncer.RunAsync(() => { _isLyricsChanged = true; });
    }

    private void LyricsLayerOrder_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequestRelayout();
    }

    private void StartRenderLoop()
    {
        _stopwatch.Start();
        _lastUpdateTime = _stopwatch.Elapsed;

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // 约 60 FPS
        };
        _renderTimer.Tick += (s, e) =>
        {
            // 1. 计算两帧之间经过的时间 (Delta Time)
            var currentTime = _stopwatch.Elapsed;
            var elapsedTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            // 2. 更新所有动画和逻辑状态
            UpdateLogic(elapsedTime);

            // 3. 通知 Avalonia 重新绘制 (这将触发下面的 Render 方法)
            InvalidateVisual();
        };
        _renderTimer.Start();
    }

    private void UpdateLogic(TimeSpan elapsedTime)
    {
        if (_lyricsWindowStatus == null) return;

        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;
        var lyricsData = _gsmtcService.CurrentLyricsData;

        var isVerticalLayoutStrategy = lyricsStyle.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        // 1. 更新颜色与透明度过渡动画
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

        // 2. 更新播放进度
        UpdatePlaybackState(elapsedTime);

        // 3. 触发可能的重新布局
        TriggerRelayout();

        // 4. 计算当前播放的行
        var primaryPlayingIndex =
            _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds,
                _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList());
        var isPrimaryPlayingLineChanged = primaryPlayingIndex != _primaryPlayingLineIndex;
        _primaryPlayingLineIndex = primaryPlayingIndex;

        // 5. 更新画布滚动目标
        if (isPrimaryPlayingLineChanged || _isLayoutChanged)
        {
            var targetScroll = _layoutStrategy.CalculateTargetScrollOffset(_renderLyricsLines, _primaryPlayingLineIndex);
            if (targetScroll.HasValue) _canvasTargetScrollOffset = targetScroll.Value;

            if (_isLayoutChanged)
            {
                _canvasScrollTransition.JumpTo(_canvasTargetScrollOffset);
            }
            else
            {
                _canvasScrollTransition.SetDurationMs(lyricsEffect.LyricsScrollDuration);
                _canvasScrollTransition.SetInterpolator(
                    EasingHelper.GetInterpolatorByEasingType<double>(lyricsEffect.LyricsScrollEasingType, lyricsEffect.LyricsScrollEasingMode));
                _canvasScrollTransition.Start(_canvasTargetScrollOffset);
            }
        }

        _canvasScrollTransition.Update(elapsedTime);
        _mouseYScrollTransition.Update(elapsedTime);

        // 6. 计算鼠标悬停与可视范围
        CurrentHoveringLineIndex = _layoutStrategy.FindMouseHoverLineIndex(
            _renderLyricsLines,
            GetValue(IsMouseInLyricsAreaProperty),
            _mousePosition.AddX(-_renderLyricsStartX).AddY(-_renderLyricsStartY),
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value,
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        _visibleRange = _layoutStrategy.CalculateVisibleRange(
            _renderLyricsLines,
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value,
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        var maxRange = LyricsLayoutStrategyBase.CalculateMaxRange(_renderLyricsLines);

        // 7. 更新歌词动画器
        var isMouseScrolling = GetValue(IsMouseScrollingProperty);
        _animator.UpdateLines(
            _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList(),
            isMouseScrolling ? maxRange.Start : _visibleRange.Start,
            isMouseScrolling ? maxRange.End : _visibleRange.End,
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
            isMouseScrolling,
            _isLayoutChanged,
            isPrimaryPlayingLineChanged,
            _isMouseScrollingChanged,
            _isNowPlayingPaletteChanged,
            _songPositionWithOffset.TotalMilliseconds
        );

        _isMouseScrollingChanged = false;
        _isNowPlayingPaletteChanged = false;
        _isLayoutChanged = false;

        if (_spectrumAnalyzer.IsCapturing) _spectrumAnalyzer.UpdateSmoothSpectrum();

        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled)
        {
            _edgeFadeMaskRenderer.Update(
                (float)Bounds.Width,
                (float)Bounds.Height,
                _lyricsWindowStatus.EdgeFeatheringLeft,
                _lyricsWindowStatus.EdgeFeatheringTop,
                _lyricsWindowStatus.EdgeFeatheringRight,
                _lyricsWindowStatus.EdgeFeatheringBottom
            );
        }

        _parallaxContext?.Update();

        // 8. 更新各大特效渲染器的内部状态 
        _fluidRenderer.IsEnabled = lyricsBg.IsFluidOverlayEnabled;
        _fluidRenderer.EnableLightWave = lyricsBg.IsFluidOverlayLightWaveEnabled;
        _fluidRenderer.EnableDithering = false; // TODO lyricsBg.IsColorDitheringEnabled;
        _fluidRenderer.Opacity = lyricsBg.FluidOverlayOpacity / 100.0;
        _fluidRenderer.IsStatic = isAccentColorsTransitioning ? false : lyricsBg.IsFluidOverlayStatic;
        _fluidRenderer.Update(elapsedTime, ColorExtensions.FromAppColor(_accentColor1Transition.Value),
            ColorExtensions.FromAppColor(_accentColor2Transition.Value),
            ColorExtensions.FromAppColor(_accentColor3Transition.Value),
            ColorExtensions.FromAppColor(_accentColor4Transition.Value),
            _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.FluidOverlayBreathingIntensity,
            lyricsBg.IsFluidOverlayParallaxEnabled, Bounds.ToSize());

        _coverRenderer.IsEnabled = lyricsBg.IsCoverOverlayEnabled;
        _coverRenderer.Opacity = lyricsBg.CoverOverlayOpacity;
        _coverRenderer.BlurAmount = lyricsBg.CoverOverlayBlurAmount;
        _coverRenderer.Speed = lyricsBg.CoverOverlaySpeed;
        _coverRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.CoverOverlayBreathingIntensity,
            lyricsBg.IsCoverOverlayParallaxEnabled, Bounds.ToSize());

        _snowRenderer.IsEnabled = false; // TODO lyricsBg.IsSnowFlakeOverlayEnabled;
        _snowRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.SnowFlakeOverlayBreathingIntensity,
            lyricsBg.IsSnowFlakeOverlayParallaxEnabled, Bounds.ToSize());

        _fogRenderer.IsEnabled = false; // TODO lyricsBg.IsFogOverlayEnabled;
        _fogRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.FogOverlayBreathingIntensity,
            lyricsBg.IsFogOverlayParallaxEnabled, Bounds.ToSize());

        _raindropRenderer.IsEnabled = false; // TODO lyricsBg.IsRaindropOverlayEnabled;
        _raindropRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.RaindropOverlayBreathingIntensity,
            lyricsBg.IsRaindropOverlayParallaxEnabled, Bounds.ToSize());

        _spectrumRenderer.Update(Bounds.ToSize(), lyricsBg.SpectrumPlacement, _albumArtRect, _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.SpectrumBreathingIntensity, lyricsBg.IsSpectrumOverlayParallaxEnabled);

        // 9. 更新歌词渲染器
        if (_renderLyricsOpacity == 1)
        {
            _lyricsRenderer.MouseHoverLineIndex = CurrentHoveringLineIndex;
            _lyricsRenderer.IsMousePressing = GetValue(IsMousePressingProperty);
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
            _lyricsRenderer.Update(_spectrumAnalyzer.CurrentBassEnergy, lyricsEffect.LyricsBreathingIntensity);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_lyricsWindowStatus == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var albumStyle = _lyricsWindowStatus.AlbumArtLayoutSettings;

        // 1. 计算背景颜色与透明度
        AppColor overlayColor;
        double finalOpacity;

        if (_lyricsWindowStatus.IsAdaptToEnvironment)
        {
            overlayColor = _immersiveBgColorTransition.Value;
            finalOpacity = _immersiveBgOpacityTransition.Value * lyricsBg.PureColorOverlayOpacity / 100.0;
        }
        else
        {
            overlayColor = _accentColor1Transition.Value;
            finalOpacity = lyricsBg.PureColorOverlayOpacity / 100.0;
        }

        // Edge Feathering
        IDisposable? opacityMaskDispose = null;
        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled && _edgeFadeMaskRenderer.Brush != null)
        {
            opacityMaskDispose = context.PushOpacityMask(_edgeFadeMaskRenderer.Brush, Bounds);
        }

        try
        {
            var avaOverlayColor = Color.FromArgb(
                (byte)(finalOpacity * 255), overlayColor.R, overlayColor.G, overlayColor.B);
            context.FillRectangle(new SolidColorBrush(avaOverlayColor), Bounds);

            _coverRenderer.Draw(context, Bounds, lyricsBg.IsCoverOverlayBrethingEffectEnabled);
            _fluidRenderer.Draw(context, Bounds, lyricsBg.IsFluidOverlayBrethingEffectEnabled);

            if (_spectrumAnalyzer.IsCapturing)
            {
                _spectrumRenderer.Draw(
                    context,
                    _spectrumAnalyzer?.SmoothSpectrum,
                    _spectrumAnalyzer?.BarCount ?? 1,
                    lyricsBg.IsSpectrumOverlayEnabled,
                    lyricsBg.IsSpectrumGlowEffectEnabled,
                    lyricsBg.IsSpectrumBrethingEffectEnabled,
                    lyricsBg.SpectrumOpacity / 100.0f,
                    lyricsBg.SpectrumPlacement,
                    lyricsBg.SpectrumStyle,
                    Bounds.Width,
                    Bounds.Height,
                    ColorExtensions.FromAppColor(_lyricsWindowStatus.WindowPalette.SpectrumColor),
                    GetValue(AlbumArtRectProperty),
                    albumStyle.CoverImageRadius
                );
            }

            _snowRenderer.Draw(context, Bounds, lyricsBg.IsSnowFlakeOverlayBrethingEffectEnabled);
            _fogRenderer.Draw(context, Bounds, lyricsBg.IsFogOverlayBrethingEffectEnabled);
            _raindropRenderer.Draw(context, Bounds, lyricsBg.IsRaindropOverlayBrethingEffectEnabled);

            // 4. 绘制核心歌词
            if (_renderLyricsOpacity == 1)
            {
                _lyricsRenderer.Draw(context);
            }
        }
        finally
        {
            // 释放遮罩资源，结束羽化影响范围
            opacityMaskDispose?.Dispose();
        }

        // 5. Debug 蒙版绘制 (如果您需要的话)
        if (_lyricsWindowStatus.ShowDebugOverlay)
        {
            var debugPen = new Pen(new SolidColorBrush(Colors.Cyan), 2);
            context.DrawRectangle(debugPen, new Rect(_renderLyricsStartX, _renderLyricsStartY, _renderLyricsWidth, _renderLyricsHeight));
            context.DrawLine(debugPen, new Point(0, Bounds.Height / 2), new Point(Bounds.Width, Bounds.Height / 2));

            // 绘制 Debug 文本的逻辑由于涉及到 FormattedText，在这里为了简洁先不展开，
            // 它的原理等同于 Win2D 的 CanvasTextLayout
        }
    }

    private void UpdatePlaybackState(TimeSpan elapsedTime)
    {
        if (_gsmtcService.CurrentIsPlaying)
        {
            // 累加两帧之间经过的时间
            _songPosition += elapsedTime;

            // 计算带有偏移量的最终播放位置（用于歌词同步校准）
            _songPositionWithOffset = _songPosition +
                                      TimeSpan.FromMilliseconds(_gsmtcService.CurrentMediaSourceProviderInfo
                                          ?.PositionOffset ?? 0);
        }
    }

    private void ResetPlaybackState()
    {
        _songPosition = TimeSpan.Zero;
    }

    private void TriggerRelayout()
    {
        if (_lyricsWindowStatus == null) return;

        if (_isLyricsChanged)
        {
            DisposeRenderLyricsLines();
            _renderLyricsLines = _gsmtcService.CurrentLyricsData?.LyricsLines.Select(x => new RenderLyricsLine(x)).ToList();
            EnsureRenderLyricsLinesPreservedAnimation();
            _isLyricsChanged = false;
            _isLayoutChanged = true;
        }

        if (_renderLyricsLines == null) return;

        if (_isLayoutChanged)
        {
            _layoutStrategy = _lyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Horizontal
                ? new HorizontalLyricsLayoutStrategy()
                : new VerticalLyricsLayoutStrategy();

            LyricsLayoutStrategyBase.CalculateLanes(_renderLyricsLines);
            LyricsLayoutStrategyBase.CalculateAlignments(_renderLyricsLines);

            // 注意：这里的 Canvas.Size.Width 等需要换成 Avalonia 的 Bounds.Width 或您实际绘图控件的尺寸
            _layoutStrategy.MeasureAndArrange(
                _renderLyricsLines,
                _lyricsWindowStatus,
                _settingsService.AppSettings,
                Bounds.Width,
                Bounds.Height,
                _renderLyricsWidth,
                _renderLyricsHeight
            );
        }
    }

    private void DisposeRenderLyricsLines()
    {
        if (_renderLyricsLines != null)
        {
            foreach (var line in _renderLyricsLines)
            {
                // 注意：在 Avalonia 版本中，如果您的 RenderLyricsLine 类
                // 处理文本布局的方式变了（比如用了 FormattedText 或 SKTextLayout），
                // 确保这里调用的依然是对应缓存的释放方法。
                line.DisposeTextGeometry();
                line.DisposeTextLayout();
                line.DisposeCaches();
            }

            _renderLyricsLines = null;
        }
    }

    /// <summary>
    /// 为长音节在歌词即将离开视图或出场时保留动画时间
    /// </summary>
    private void EnsureRenderLyricsLinesPreservedAnimation()
    {
        if (_lyricsWindowStatus == null) return;
        if (_renderLyricsLines == null) return;

        if (!_lyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled &&
            !_lyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled) return;

        // 这里原有的 Time.AnimationDuration 需要确保在 Avalonia 核心库中依然可以访问
        var animationPadding = (int)Core.Constants.Time.AnimationDuration.TotalMilliseconds;
        var longSyllableThreshold = Math.Max(
            _lyricsWindowStatus.LyricsEffectSettings.LyricsScaleEffectLongSyllableDuration,
            _lyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectLongSyllableDuration
        );

        var lines = _renderLyricsLines;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var isLastLine = i + 1 >= lines.Count;
            // 默认最后一句，使用歌曲总时长作为参考
            var nextLineStartMs = isLastLine ? (int)_gsmtcService.CurrentSongInfo.DurationMs : lines[i + 1].StartMs;

            // 判断这一句中是否存在长音节的长发音
            var isLongSyllable = line.PrimaryRenderSyllables.LastOrDefault()?.DurationMs >= longSyllableThreshold;

            // 如果最后一句是长发音时长，则向后补齐动效时间（Padding）
            if (line.EndMs.HasValue && isLongSyllable)
            {
                if (isLastLine || line.EndMs > nextLineStartMs)
                {
                    // 最后一句或背景/平行动词，直接加上动画时间
                    line.EndMs += animationPadding;
                }
                else
                {
                    // 计算加上动画时间后，是否超出了下一句的开始时间
                    var targetEndMs = line.EndMs.Value + animationPadding;

                    // 限制不能超出下一句的开始时间，但确保不能比原来的 EndMs 更小
                    line.EndMs = Math.Max(line.EndMs.Value, Math.Min(targetEndMs, nextLineStartMs));
                }
            }
        }
    }

    private void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        // 1. 加载各大特效渲染器的内部资源
        _fluidRenderer.LoadResources();
        _snowRenderer.LoadResources();
        _fogRenderer.LoadResources();
        _raindropRenderer.LoadResources();

        // 2. 初始化频谱分析器
        //InitSpectrumAnalyzer();

        // 3. 初始化 Spout (如果您在 Avalonia 跨平台版本中依然保留了 Spout)
        //InitSpoutHook();

        // 4. 初次请求布局与歌词刷新
        RequestRelayout();
        TriggerRelayout();

        // 5. 触发一次背景封面的加载
        _ = ReloadCoverBackgroundResourcesAsync();
    }

    private void UserControl_Unloaded(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        _renderTimer?.Stop();

        // Dispose internal renderers...
    }

    public void HandlePointerEntered(object? sender, PointerEventArgs e)
    {
    }

    public void HandlePointerExited(object? sender, PointerEventArgs e)
    {
        SetValue(IsMouseInLyricsAreaProperty, false);
        SetValue(IsMousePressingProperty, false);

        _parallaxContext?.OnPointerExited();
    }

    public void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        var isInsideLyricsArea = IsPointerInsideLyricsContainer(pointerPoint.Position);

        SetValue(IsMouseInLyricsAreaProperty, isInsideLyricsArea);

        if (isInsideLyricsArea) SetValue(MousePositionProperty, pointerPoint.Position);

        // Convert Point manually if extension isn't directly available
        _parallaxContext?.OnPointerMoved(new AppPoint(pointerPoint.Position.X, pointerPoint.Position.Y),
                                         new AppSize(Bounds.Width, Bounds.Height));
    }

    public void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        if (IsPointerInsideLyricsContainer(pointerPoint.Position))
            SetValue(IsMousePressingProperty, true);
    }

    public void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetValue(IsMousePressingProperty, false);
        var pointerPoint = e.GetCurrentPoint(this);

        if (IsPointerInsideLyricsContainer(pointerPoint.Position))
        {
            // Replace with your actual HoveringLineIndex property
            _ = _gsmtcService.ChangeLyricsLineAsync(CurrentHoveringLineIndex);
        }
    }

    public void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        if (!IsPointerInsideLyricsContainer(pointerPoint.Position)) return;

        SetValue(IsMouseScrollingProperty, true);

        // Avalonia exposes wheel delta differently (Vector instead of simple delta)
        var mouseWheelDelta = e.Delta.Y * 40; // Adjust scalar for standard scroll speed

        var isVertical = LyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        // Remaining scroll logic stays mostly the same...
    }

    private bool IsPointerInsideLyricsContainer(Point position)
    {
        var startX = GetValue(LyricsStartXProperty);
        var startY = GetValue(LyricsStartYProperty);
        var width = GetValue(LyricsWidthProperty);
        var height = GetValue(LyricsHeightProperty);

        return startX <= position.X && position.X <= startX + width &&
               startY <= position.Y && position.Y <= startY + height;
    }

    private void RequestRelayout()
    {
        _ = _layoutDebouncer.RunAsync(() => { _isLayoutChanged = true; });
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

    private async Task ReloadCoverBackgroundResourcesAsync()
    {
        try
        {
            var imageBytes = _gsmtcService.AlbumArtBytes;
            if (imageBytes == null || imageBytes.Length == 0) return;

            // 在 Avalonia 中，直接使用标准的 MemoryStream 即可
            using var memoryStream = new System.IO.MemoryStream(imageBytes);

            // 放到后台线程去解码图片，防止大图阻塞 UI 线程
            var bitmap = await Task.Run(() => new Bitmap(memoryStream));

            // 将 Avalonia 的 Bitmap 传递给您的渲染器
            //_coverRenderer.SetCoverBitmap(bitmap);

            // 触发重新绘制
            InvalidateVisual();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ReloadCoverBackgroundResourcesAsync: {ex}");
        }
    }
}