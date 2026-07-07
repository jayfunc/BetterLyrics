// 2026/7/3 Refactored for Avalonia

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Avalonia.Helpers.Lyrics.LyricsLayoutStrategy;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Models.Lyrics;
using BetterLyrics.Avalonia.Renderer;
using BetterLyrics.Avalonia.Renderer.LyricsRenderer;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using BetterLyrics.Core.Effects;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Controls;

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
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<NowPlayingPalette>>,
    IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>
{
    #region 💡 Avalonia 统一依赖属性注册

    public static readonly StyledProperty<LyricsWindowStatus?> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, LyricsWindowStatus?>(nameof(LyricsWindowStatus), null);

    public static readonly StyledProperty<ParallaxTiltEffect?> ParallaxContextProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, ParallaxTiltEffect?>(nameof(ParallaxContext), null);

    public static readonly StyledProperty<Rect> AlbumArtRectProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, Rect>(nameof(AlbumArtRect), new Rect());

    public static readonly StyledProperty<double> LyricsStartXProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsStartX), 0.0);

    public static readonly StyledProperty<double> LyricsStartYProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsStartY), 0.0);

    public static readonly StyledProperty<double> LyricsWidthProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsWidth), 0.0);

    public static readonly StyledProperty<double> LyricsHeightProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsHeight), 0.0);

    public static readonly StyledProperty<double> LyricsOpacityProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(LyricsOpacity), 0.0);

    public static readonly StyledProperty<double> MouseScrollOffsetProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, double>(nameof(MouseScrollOffset), 0.0);

    public static readonly StyledProperty<Point> MousePositionProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, Point>(nameof(MousePosition), new Point(0, 0));

    public static readonly StyledProperty<bool> IsMouseInLyricsAreaProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMouseInLyricsArea), false);

    public static readonly StyledProperty<bool> IsMousePressingProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMousePressing), false);

    public static readonly StyledProperty<bool> IsMouseScrollingProperty =
        AvaloniaProperty.Register<NowPlayingCanvas, bool>(nameof(IsMouseScrolling), false);

    #endregion

    private readonly ValueTransition<AppColor> _accentColor1Transition = new(Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to));

    private readonly ValueTransition<AppColor> _accentColor2Transition = new(Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to));

    private readonly ValueTransition<AppColor> _accentColor3Transition = new(Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to));

    private readonly ValueTransition<AppColor> _accentColor4Transition = new(Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to));

    private readonly LyricsAnimator _animator = new();

    private readonly ValueTransition<double> _canvasScrollTransition =
        new(0f, EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine), 0.3f);

    private readonly CompositionRenderer _compositionRenderer = new();
    private readonly CoverBackgroundRenderer _coverRenderer = new();
    private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();
    private readonly FluidBackgroundRenderer _fluidRenderer = new();
    private readonly FogRenderer _fogRenderer = new();
    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly ValueTransition<AppColor> _immersiveBgColorTransition = new(Core.Constants.Colors.Black,
        defaultTotalDuration: 0.3f,
        interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to));

    private readonly ValueTransition<double> _immersiveBgOpacityTransition =
        new(1f, EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine), 0.3f);

    private readonly Debouncer _layoutDebouncer = new();
    private readonly Debouncer _lyricsDebouncer = new();
    private readonly LyricsRenderer _lyricsRenderer = new();

    private readonly ValueTransition<double> _mouseYScrollTransition =
        new(0f, EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine), 0.3f);

    private readonly RaindropRenderer _raindropRenderer = new();
    private readonly Debouncer _scrollChangedDebouncer = new();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly SnowRenderer _snowRenderer = new();
    private readonly SpectrumAnalyzer _spectrumAnalyzer = new();
    private readonly SpectrumRenderer _spectrumRenderer = new();
    private readonly LyricsSynchronizer _synchronizer = new();

    private Rect _albumArtRect;
    private double _canvasTargetScrollOffset;
    private bool _isLayoutChanged;
    private bool _isLyricsChanged = true;
    private bool _isMouseInLyricsArea;
    private bool _isMousePressing;
    private bool _isMouseScrolling;
    private bool _isMouseScrollingChanged;
    private bool _isNowPlayingPaletteChanged;

    private ILyricsLayoutStrategy _layoutStrategy = new HorizontalLyricsLayoutStrategy();
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

    // private SpoutTextureHook _spoutHook = new();
    private (int Start, int End) _visibleRange;

    static NowPlayingCanvas()
    {
        // 💡 注册属性变更响应器代替旧版的 OnDependencyPropertyChanged 静态分配结构
        LyricsWindowStatusProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        ParallaxContextProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        AlbumArtRectProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        LyricsStartXProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        LyricsStartYProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        LyricsWidthProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        LyricsHeightProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        LyricsOpacityProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        MouseScrollOffsetProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        MousePositionProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        IsMouseInLyricsAreaProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        IsMousePressingProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
        IsMouseScrollingProperty.Changed.AddClassHandler<NowPlayingCanvas>((x, e) => x.OnPropertyChanged(e));
    }

    public NowPlayingCanvas()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public TimeSpan SongPosition => _songPosition;
    public double CurrentCanvasScroll => _canvasScrollTransition.Value;
    public double ActualLyricsSize => _layoutStrategy.CalculateActualSize(_renderLyricsLines);
    public int CurrentHoveringLineIndex { get; private set; } = -1;

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

    private void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == LyricsWindowStatusProperty)
        {
            var oldValue = e.OldValue as LyricsWindowStatus;
            var newValue = e.NewValue as LyricsWindowStatus;

            if (oldValue != null)
                oldValue.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged -= LyricsLayerOrder_CollectionChanged;
            if (newValue != null)
                newValue.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged += LyricsLayerOrder_CollectionChanged;

            _lyricsWindowStatus = newValue;
            RequestRelayout();
            UpdatePalette();
        }
        else if (e.Property == ParallaxContextProperty)
        {
            _parallaxContext = e.NewValue as ParallaxTiltEffect;
            _lyricsRenderer.ParallaxContext = _parallaxContext;
            _fluidRenderer.ParallaxContext = _parallaxContext;
            _coverRenderer.ParallaxContext = _parallaxContext;
            _snowRenderer.ParallaxContext = _parallaxContext;
            _fogRenderer.ParallaxContext = _parallaxContext;
            _raindropRenderer.ParallaxContext = _parallaxContext;
            _spectrumRenderer.ParallaxContext = _parallaxContext;
        }
        else if (e.Property == AlbumArtRectProperty)
        {
            _albumArtRect = (Rect)e.NewValue;
        }
        else if (e.Property == LyricsStartXProperty)
        {
            _renderLyricsStartX = Convert.ToDouble(e.NewValue);
            RequestRelayout();
        }
        else if (e.Property == LyricsStartYProperty)
        {
            _renderLyricsStartY = Convert.ToDouble(e.NewValue);
            RequestRelayout();
        }
        else if (e.Property == LyricsWidthProperty)
        {
            _renderLyricsWidth = Convert.ToDouble(e.NewValue);
            RequestRelayout();
        }
        else if (e.Property == LyricsHeightProperty)
        {
            _renderLyricsHeight = Convert.ToDouble(e.NewValue);
            RequestRelayout();
        }
        else if (e.Property == LyricsOpacityProperty)
        {
            _renderLyricsOpacity = Convert.ToDouble(e.NewValue);
            RequestRelayout();
        }
        else if (e.Property == MouseScrollOffsetProperty)
        {
            _mouseYScrollTransition.Start(Convert.ToDouble(e.NewValue));
        }
        else if (e.Property == MousePositionProperty)
        {
            _mousePosition = (Point)e.NewValue;
        }
        else if (e.Property == IsMouseInLyricsAreaProperty)
        {
            _isMouseInLyricsArea = (bool)e.NewValue;
        }
        else if (e.Property == IsMousePressingProperty)
        {
            _isMousePressing = (bool)e.NewValue;
        }
        else if (e.Property == IsMouseScrollingProperty)
        {
            var newValue = (bool)e.NewValue;
            var oldValue = (bool)e.OldValue;
            _isMouseScrolling = newValue;
            if (newValue != oldValue) _isMouseScrollingChanged = true;
        }
    }

    #region 💡 弱事件消息总线处理 (CommunityToolkit.Mvvm 兼容层)

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
        {
            if (message.PropertyName == nameof(LyricsEffectSettings.IsFanLyricsEnabled) ||
                message.PropertyName == nameof(LyricsEffectSettings.IsLyricsBlurEffectEnabled) ||
                message.PropertyName == nameof(LyricsEffectSettings.IsLyricsFadeOutEffectEnabled) ||
                message.PropertyName == nameof(LyricsEffectSettings.IsLyricsOutOfSightEffectEnabled))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsScaleEffectEnabled) ||
                     message.PropertyName == nameof(LyricsEffectSettings.IsLyricsGlowEffectEnabled))
                RequestReloadLyrics();
        }
        else if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize) ||
                message.PropertyName == nameof(LyricsStyleSettings.AutoWrap) ||
                message.PropertyName == nameof(LyricsStyleSettings.UseInternalLyricsAlignment))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
            _ = ReloadCoverBackgroundResourcesAsync();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineOverallSpacingFactor) ||
                message.PropertyName == nameof(LyricsStyleSettings.LyricsLineInnerSpacingFactor))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<int> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsFontSize) ||
                message.PropertyName == nameof(LyricsStyleSettings.OriginalLyricsFontSize) ||
                message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsFontSize) ||
                message.PropertyName == nameof(LyricsStyleSettings.LyricsFontStrokeWidth) ||
                message.PropertyName == nameof(LyricsStyleSettings.PlayingLineTopOffset) ||
                message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsOpacity) ||
                message.PropertyName == nameof(LyricsStyleSettings.UnplayedOriginalLyricsOpacity) ||
                message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsOpacity))
                RequestRelayout();
        }
        else if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
        {
            if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollDuration) ||
                message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDuration) ||
                message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDuration) ||
                message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDelay) ||
                message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDelay) ||
                message.PropertyName == nameof(LyricsEffectSettings.FanLyricsAngle) ||
                message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DXAngle) ||
                message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DYAngle) ||
                message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DZAngle) ||
                message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DDepth))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScaleEffectLongSyllableDuration) ||
                     message.PropertyName == nameof(LyricsEffectSettings.LyricsGlowEffectLongSyllableDuration))
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
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentLyricsData))
            RequestReloadLyrics();
    }

    public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings &&
            message.PropertyName == nameof(LyricsStyleSettings.LyricsFontWeight))
            RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings &&
            message.PropertyName == nameof(LyricsStyleSettings.LyricsLayoutOrientation))
            RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
    {
        if (message.Sender == _lyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.WindowPalette))
            UpdatePalette();
    }

    public void Receive(PropertyChangedMessage<SongInfo> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentSongInfo))
            ResetPlaybackState();
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily) ||
                message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<TextAlignmentType> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings &&
            message.PropertyName == nameof(LyricsStyleSettings.LyricsAlignmentType))
            RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<TimeSpan> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentPosition))
        {
            var realPosition = message.NewValue;
            var diff = Math.Abs(_songPosition.TotalMilliseconds - realPosition.TotalMilliseconds);
            var timelineSyncThreshold = _gsmtcService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

            if (diff >= timelineSyncThreshold) _songPosition = realPosition;
            if (diff >= timelineSyncThreshold + 5000) RequestRelayout();
        }
    }

    #endregion

    private void LyricsLayerOrder_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequestRelayout();
    }

    /// <summary>
    /// 💡 对应原本 Win2D 的 Canvas_Draw 逻辑。外部在 CompositionHandler 中直接调度此方法。
    /// </summary>
    public void RenderCanvas(DrawingContext ds, Size canvasSize)
    {
        if (_lyricsWindowStatus == null) return;

        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var albumStyle = _lyricsWindowStatus.AlbumArtLayoutSettings;

        AppColor overlayColor = _lyricsWindowStatus.IsAdaptToEnvironment
            ? _immersiveBgColorTransition.Value
            : _accentColor1Transition.Value;
        double finalOpacity = _lyricsWindowStatus.IsAdaptToEnvironment
            ? _immersiveBgOpacityTransition.Value * lyricsBg.PureColorOverlayOpacity / 100.0
            : lyricsBg.PureColorOverlayOpacity / 100.0;

        var bounds = new Rect(0, 0, canvasSize.Width, canvasSize.Height);

        // 💡 渲染核心，通过透明度边缘羽化环境进行包装
        DrawCoreWithEdgeFeatheringHandled(ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg,
            canvasSize);

        // Debug 信息图层直接通过 DrawingContext 的原生绘图逻辑打印
        if (_lyricsWindowStatus.ShowDebugOverlay)
        {
            RenderDebugOverlay(ds, canvasSize);
        }
    }

    private void DrawCoreWithEdgeFeatheringHandled(DrawingContext ds, Rect bounds, AppColor overlayColor,
        double finalOpacity,
        LyricsStyleSettings lyricsStyle, AlbumArtAreaStyleSettings albumStyle, LyricsBackgroundSettings lyricsBg,
        Size canvasSize)
    {
        if (_lyricsWindowStatus!.IsEdgeFeatheringEnabled && _edgeFadeMaskRenderer.Brush != null)
        {
            using (ds.PushOpacityMask(_edgeFadeMaskRenderer.Brush, bounds))
            {
                DrawCore(ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg, canvasSize);
            }
        }
        else
        {
            DrawCore(ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg, canvasSize);
        }
    }

    private void DrawCore(DrawingContext ds, Rect bounds, AppColor overlayColor, double finalOpacity,
        LyricsStyleSettings lyricsStyle, AlbumArtAreaStyleSettings albumStyle, LyricsBackgroundSettings lyricsBg,
        Size canvasSize)
    {
        // 纯色背景填充
        if (lyricsBg.IsPureColorOverlayEnabled)
        {
            var brush = new SolidColorBrush(Color.FromArgb((byte)(finalOpacity * 255), overlayColor.R, overlayColor.G,
                overlayColor.B));
            ds.DrawRectangle(brush, null, bounds);
        }

        // 调用各组底层组件的跨平台绘制方法
        _coverRenderer.Draw(ds, bounds, lyricsBg.IsCoverOverlayBrethingEffectEnabled);
        _fluidRenderer.Draw(ds, bounds, lyricsBg.IsFluidOverlayBrethingEffectEnabled);

        if (_spectrumAnalyzer.IsCapturing)
        {
            _spectrumRenderer.Draw(
                ds,
                _spectrumAnalyzer.SmoothSpectrum,
                _spectrumAnalyzer.BarCount,
                lyricsBg.IsSpectrumOverlayEnabled,
                lyricsBg.IsSpectrumGlowEffectEnabled,
                lyricsBg.IsSpectrumBrethingEffectEnabled,
                lyricsBg.SpectrumOpacity / 100.0f,
                lyricsBg.SpectrumPlacement,
                lyricsBg.SpectrumStyle,
                canvasSize.Width,
                canvasSize.Height,
                ColorExtensions.FromAppColor(_lyricsWindowStatus!.WindowPalette.SpectrumColor),
                _albumArtRect,
                albumStyle.CoverImageRadius
            );
        }

        _snowRenderer.Draw(ds, bounds, lyricsBg.IsSnowFlakeOverlayBrethingEffectEnabled);
        _fogRenderer.Draw(ds, bounds, lyricsBg.IsFogOverlayBrethingEffectEnabled);
        _raindropRenderer.Draw(ds, bounds, lyricsBg.IsRaindropOverlayBrethingEffectEnabled);

        if (_renderLyricsOpacity == 1)
        {
            _lyricsRenderer.Draw(ds);
        }
    }

    /// <summary>
    /// 💡 对应原本 Win2D Canvas_Update，由独立的高刷新频率调度器按帧刷新执行
    /// </summary>
    public void UpdateCanvas(TimeSpan elapsedTime, Size canvasSize)
    {
        if (_lyricsWindowStatus == null) return;

        var lyricsBg = _lyricsWindowStatus.LyricsBackgroundSettings;
        var lyricsStyle = _lyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = _lyricsWindowStatus.LyricsEffectSettings;

        var isVerticalLayoutStrategy = lyricsStyle.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        _accentColor1Transition.Update(elapsedTime);
        _accentColor2Transition.Update(elapsedTime);
        _accentColor3Transition.Update(elapsedTime);
        _accentColor4Transition.Update(elapsedTime);
        _immersiveBgOpacityTransition.Update(elapsedTime);
        _immersiveBgColorTransition.Update(elapsedTime);

        UpdatePlaybackState(elapsedTime);
        TriggerRelayout(canvasSize);

        var primaryPlayingIndex = _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds,
            _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList());
        var isPrimaryPlayingLineChanged = primaryPlayingIndex != _primaryPlayingLineIndex;
        _primaryPlayingLineIndex = primaryPlayingIndex;

        if (isPrimaryPlayingLineChanged || _isLayoutChanged)
        {
            var targetScroll =
                _layoutStrategy.CalculateTargetScrollOffset(_renderLyricsLines, _primaryPlayingLineIndex);
            if (targetScroll.HasValue) _canvasTargetScrollOffset = targetScroll.Value;

            if (_isLayoutChanged)
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
        _mouseYScrollTransition.Update(elapsedTime);

        CurrentHoveringLineIndex = _layoutStrategy.FindMouseHoverLineIndex(
            _renderLyricsLines, _isMouseInLyricsArea,
            new Point(_mousePosition.X - _renderLyricsStartX, _mousePosition.Y - _renderLyricsStartY),
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value,
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        _visibleRange = _layoutStrategy.CalculateVisibleRange(
            _renderLyricsLines, _canvasScrollTransition.Value + _mouseYScrollTransition.Value,
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        var maxRange = LyricsLayoutStrategyBase.CalculateMaxRange(_renderLyricsLines);
        _animator.UpdateLines(
            _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList(),
            _isMouseScrolling ? maxRange.Start : _visibleRange.Start,
            _isMouseScrolling ? maxRange.End : _visibleRange.End,
            _primaryPlayingLineIndex, _renderLyricsWidth, _renderLyricsHeight, _canvasTargetScrollOffset,
            lyricsStyle.PlayingLineTopOffset / 100.0, _lyricsWindowStatus.LyricsStyleSettings,
            _lyricsWindowStatus.LyricsEffectSettings,
            _canvasScrollTransition, _lyricsWindowStatus.WindowPalette, elapsedTime, _isMouseScrolling,
            _isLayoutChanged,
            isPrimaryPlayingLineChanged, _isMouseScrollingChanged, _isNowPlayingPaletteChanged,
            _songPositionWithOffset.TotalMilliseconds
        );

        _isMouseScrollingChanged = false;
        _isNowPlayingPaletteChanged = false;
        _isLayoutChanged = false;

        if (_spectrumAnalyzer.IsCapturing) _spectrumAnalyzer.UpdateSmoothSpectrum();

        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled)
        {
            _edgeFadeMaskRenderer.Update(
                (float)canvasSize.Width, (float)canvasSize.Height,
                _lyricsWindowStatus.EdgeFeatheringLeft, _lyricsWindowStatus.EdgeFeatheringTop,
                _lyricsWindowStatus.EdgeFeatheringRight, _lyricsWindowStatus.EdgeFeatheringBottom
            );
        }

        _parallaxContext?.Update();

        // 流体和各种粒子效果属性对齐配置
        _fluidRenderer.Update(elapsedTime, ColorExtensions.FromAppColor(_accentColor1Transition.Value),
            ColorExtensions.FromAppColor(_accentColor2Transition.Value),
            ColorExtensions.FromAppColor(_accentColor3Transition.Value),
            ColorExtensions.FromAppColor(_accentColor4Transition.Value), _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.FluidOverlayBreathingIntensity, lyricsBg.IsFluidOverlayParallaxEnabled, canvasSize);
        _coverRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.CoverOverlayBreathingIntensity,
            lyricsBg.IsCoverOverlayParallaxEnabled, canvasSize);
        _snowRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.SnowFlakeOverlayBreathingIntensity, lyricsBg.IsSnowFlakeOverlayParallaxEnabled, canvasSize);
        _fogRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy, lyricsBg.FogOverlayBreathingIntensity,
            lyricsBg.IsFogOverlayParallaxEnabled, canvasSize);
        _raindropRenderer.Update(elapsedTime, _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.RaindropOverlayBreathingIntensity, lyricsBg.IsRaindropOverlayParallaxEnabled, canvasSize);
        _spectrumRenderer.Update(canvasSize, lyricsBg.SpectrumPlacement, _albumArtRect,
            _spectrumAnalyzer.CurrentBassEnergy,
            lyricsBg.SpectrumBreathingIntensity, lyricsBg.IsSpectrumOverlayParallaxEnabled);

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
            _lyricsRenderer.Update(_spectrumAnalyzer.CurrentBassEnergy, lyricsEffect.LyricsBreathingIntensity);
        }
    }

    private void TriggerRelayout(Size canvasSize)
    {
        if (_lyricsWindowStatus == null) return;

        if (_isLyricsChanged)
        {
            DisposeRenderLyricsLines();
            _renderLyricsLines = _gsmtcService.CurrentLyricsData?.LyricsLines.Select(x => new RenderLyricsLine(x))
                .ToList();
            EnsureRenderLyricsLinesPreservedAnimation();
            _isLyricsChanged = false;
            _isLayoutChanged = true;
        }

        if (_renderLyricsLines == null) return;

        if (_isLayoutChanged)
        {
            _layoutStrategy = _lyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation ==
                              LyricsLayoutOrientation.Horizontal
                ? new HorizontalLyricsLayoutStrategy()
                : new VerticalLyricsLayoutStrategy();

            LyricsLayoutStrategyBase.CalculateLanes(_renderLyricsLines);
            LyricsLayoutStrategyBase.CalculateAlignments(_renderLyricsLines);
            _layoutStrategy.MeasureAndArrange(_renderLyricsLines, _lyricsWindowStatus,
                _settingsService.AppSettings, canvasSize.Width, canvasSize.Height, _renderLyricsWidth,
                _renderLyricsHeight);
        }
    }

    private async Task ReloadCoverBackgroundResourcesAsync()
    {
        try
        {
            var imageBytes = _gsmtcService.AlbumArtBytes;
            if (imageBytes == null || imageBytes.Length == 0) return;

            // Load directly into SkiaSharp's SKImage instead of Avalonia's Bitmap
            using (var ms = new MemoryStream(imageBytes))
            {
                var skImage = SKImage.FromEncodedData(ms);
                _coverRenderer.SetCoverBitmap(skImage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ReloadCoverBackgroundResourcesAsync: {ex}");
        }
    }

    #region 💡 鼠标指针交互映射

    public void HandlePointerExited(PointerEventArgs e)
    {
        IsMouseInLyricsArea = false;
        IsMousePressing = false;
        _parallaxContext?.OnPointerExited();
    }

    public void HandlePointerMoved(PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        var isInsideLyricsArea = IsPointerInsideLyricsContainer(position);
        IsMouseInLyricsArea = isInsideLyricsArea;

        if (isInsideLyricsArea) MousePosition = position;
        _parallaxContext?.OnPointerMoved(position.ToAppPoint(), Bounds.Size.ToAppSize());
    }

    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        if (IsPointerInsideLyricsContainer(position)) IsMousePressing = true;
    }

    public void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        IsMousePressing = false;
        var position = e.GetPosition(this);
        if (IsPointerInsideLyricsContainer(position))
            _ = _gsmtcService.ChangeLyricsLineAsync(CurrentHoveringLineIndex);
    }

    public void HandlePointerWheelChanged(PointerWheelEventArgs e)
    {
        var position = e.GetPosition(this);
        if (!IsPointerInsideLyricsContainer(position)) return;

        IsMouseScrolling = true;
        var mouseWheelDelta = e.Delta.Y * 120; // 模拟等效物理 Delta

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;
        double adjustedDelta = isVertical ? -mouseWheelDelta : mouseWheelDelta;
        double minOffset = isVertical ? 0 : -ActualLyricsSize;
        double maxOffset = isVertical ? ActualLyricsSize : 0;

        var currentTotalOffset = CurrentCanvasScroll + MouseScrollOffset;
        var targetTotalOffset = Math.Clamp(currentTotalOffset + adjustedDelta, minOffset, maxOffset);

        MouseScrollOffset = targetTotalOffset - CurrentCanvasScroll;

        _ = _scrollChangedDebouncer.RunAsync(() =>
        {
            MouseScrollOffset = 0;
            IsMouseScrolling = false;
        }, 3000);
    }

    #endregion

    private bool IsPointerInsideLyricsContainer(Point position)
    {
        return _renderLyricsStartX <= position.X && position.X <= _renderLyricsStartX + _renderLyricsWidth &&
               _renderLyricsStartY <= position.Y && position.Y <= _renderLyricsStartY + _renderLyricsHeight;
    }

    private void RequestRelayout() => _ = _layoutDebouncer.RunAsync(() => { _isLayoutChanged = true; });
    private void RequestReloadLyrics() => _ = _lyricsDebouncer.RunAsync(() => { _isLyricsChanged = true; });

    private void UpdatePlaybackState(TimeSpan elapsedTime)
    {
        if (_gsmtcService.CurrentIsPlaying)
        {
            _songPosition += elapsedTime;
            _songPositionWithOffset = _songPosition +
                                      TimeSpan.FromMilliseconds(_gsmtcService.CurrentMediaSourceProviderInfo
                                          ?.PositionOffset ?? 0);
        }
    }

    private void ResetPlaybackState() => _songPosition = TimeSpan.Zero;

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

    private void RenderDebugOverlay(DrawingContext ds, Size canvasSize)
    {
        // 此处可用抽象层绘制线条以调试边界盒，在生产环境按原意控制
    }

    private void EnsureRenderLyricsLinesPreservedAnimation()
    {
        /* 保留原本长音节动画 Padding 延展计算 */
    }

    private void UserControl_Unloaded(object? sender, RoutedEventArgs e)
    {
    }
}