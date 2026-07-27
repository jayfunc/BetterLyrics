// 2025/6/23 by Zhe Fang

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
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers.Lyrics.LyricsLayoutStrategy;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Renderer;
using BetterLyrics.WinUI3.Renderer.LyricsRenderer;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using BetterLyrics.Core.Effects;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.WinUI3.Controls;

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
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(NowPlayingCanvas), new PropertyMetadata(null, OnDependencyPropertyChanged));

    public static readonly DependencyProperty ParallaxContextProperty =
        DependencyProperty.Register(nameof(ParallaxContext), typeof(ParallaxTiltEffect), typeof(NowPlayingCanvas),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    public static readonly DependencyProperty AlbumArtRectProperty =
        DependencyProperty.Register(nameof(AlbumArtRect), typeof(Rect), typeof(NowPlayingCanvas),
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
    private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();
    private readonly FluidBackgroundRenderer _fluidRenderer = new();
    private readonly FogRenderer _fogRenderer = new();
    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

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
    private TimeSpan _songPosition; // ��ǰ����ʱ��

    private TimeSpan _songPositionWithOffset;
    private readonly ISpoutTextureProvider _spoutHook = Ioc.Default.GetRequiredService<ISpoutTextureProvider>();
    private (int Start, int End) _visibleRange;

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

    // ���������ʼ�� X ����
    public double LyricsStartX
    {
        get => (double)GetValue(LyricsStartXProperty);
        set => SetValue(LyricsStartXProperty, value);
    }

    // ���������ʼ Y ����
    public double LyricsStartY
    {
        get => (double)GetValue(LyricsStartYProperty);
        set => SetValue(LyricsStartYProperty, value);
    }

    // �������������
    public double LyricsWidth
    {
        get => (double)GetValue(LyricsWidthProperty);
        set => SetValue(LyricsWidthProperty, value);
    }

    // ����������߶�
    public double LyricsHeight
    {
        get => (double)GetValue(LyricsHeightProperty);
        set => SetValue(LyricsHeightProperty, value);
    }

    // �������͸����
    public double LyricsOpacity
    {
        get => (double)GetValue(LyricsOpacityProperty);
        set => SetValue(LyricsOpacityProperty, value);
    }

    /// <summary>
    ///     �û��ٿ�����ѹ����ľ��루�� 0 ��ʼ�㣩
    /// </summary>
    public double MouseScrollOffset
    {
        get => (double)GetValue(MouseScrollOffsetProperty);
        set => SetValue(MouseScrollOffsetProperty, value);
    }

    /// <summary>
    ///     �û���굱ǰ��λ�ã�����ڸ���������Ͻǣ�
    /// </summary>
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

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsEffectSettings)
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
        else if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.AutoWrap))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.UseInternalLyricsAlignment)) RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
                _ = ReloadCoverBackgroundResourcesAsync();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineOverallSpacingFactor))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineInnerSpacingFactor))
                RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<int> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
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
            else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsOpacity)) RequestRelayout();
        }
        else if (message.Sender == _lyricsWindowStatus?.LyricsEffectSettings)
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
        else if (message.Sender == _lyricsWindowStatus?.LyricsBackgroundSettings)
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
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontWeight))
                RequestRelayout();
    }

    public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
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
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                RequestRelayout();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily)) RequestRelayout();
        }
    }

    public void Receive(PropertyChangedMessage<TextAlignmentType> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
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
                var timelineSyncThreshold =
                    _gsmtcService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;

                // ƫ�� or seek
                if (diff >= timelineSyncThreshold) _songPosition = realPosition;

                // �϶��������ȴ���
                if (diff >= timelineSyncThreshold + 5000) RequestRelayout();
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

                oldValue?.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged -=
                    canvas.LyricsLayerOrder_CollectionChanged;
                newValue?.LyricsStyleSettings.LyricsLayerOrder.CollectionChanged +=
                    canvas.LyricsLayerOrder_CollectionChanged;

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
                if (newValue != oldValue) canvas._isMouseScrollingChanged = true;
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

        if (_lyricsWindowStatus.IsAdaptToEnvironment)
        {
            // ����Ӧɫ
            overlayColor = _immersiveBgColorTransition.Value;
            finalOpacity = _immersiveBgOpacityTransition.Value * lyricsBg.PureColorOverlayOpacity / 100.0;
        }
        else
        {
            // ר��ɫ
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

        TriggerRelayout();

        #region UpdatePlayingLineIndex

        var primaryPlayingIndex =
            _synchronizer.GetCurrentLineIndex(_songPositionWithOffset.TotalMilliseconds,
                _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList());
        var isPrimaryPlayingLineChanged = primaryPlayingIndex != _primaryPlayingLineIndex;
        _primaryPlayingLineIndex = primaryPlayingIndex;

        #endregion

        #region UpdateTargetScrollOffset

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

        _visibleRange = _layoutStrategy.CalculateVisibleRange(
            _renderLyricsLines,
            _canvasScrollTransition.Value + _mouseYScrollTransition.Value, // ��ǰ����λ��
            isVerticalLayoutStrategy ? _renderLyricsStartX : _renderLyricsStartY,
            isVerticalLayoutStrategy ? _renderLyricsWidth : _renderLyricsHeight,
            lyricsStyle.PlayingLineTopOffset / 100.0
        );

        var maxRange = LyricsLayoutStrategyBase.CalculateMaxRange(_renderLyricsLines);

        _animator.UpdateLines(
            _renderLyricsLines?.Select(x => x.ToBaseRenderLyricsLine()).ToList(),
            _isMouseScrolling ? maxRange.Start : _visibleRange.Start,
            _isMouseScrolling ? maxRange.End : _visibleRange.End,
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
            _lyricsRenderer.Update(sender, _spectrumAnalyzer.CurrentBassEnergy,
                lyricsEffect.LyricsBreathingIntensity);
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

        InitSpectrumAnalyzer();
        InitSpoutHook(sender);

        RequestRelayout();
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

        _lyricsRenderer.Dispose();
        _fluidRenderer.Dispose();
        _coverRenderer.Dispose();
        _snowRenderer.Dispose();
        _fogRenderer.Dispose();
        _raindropRenderer.Dispose();
        _spectrumRenderer.Dispose();

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

        if (isInsideLyricsArea) MousePosition = pointerPoint.Position;

        _parallaxContext?.OnPointerMoved(pointerPoint.Position.ToAppPoint(), ActualSize.ToAppSize());
    }

    public void HandlePointerPressed(PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        if (IsPointerInsideLyricsContainer(pointerPoint.Position)) IsMousePressing = true;
    }

    public void HandlePointerReleased(PointerRoutedEventArgs e)
    {
        IsMousePressing = false;
        var pointerPoint = e.GetCurrentPoint(this);

        if (IsPointerInsideLyricsContainer(pointerPoint.Position))
            _ = _gsmtcService.ChangeLyricsLineAsync(CurrentHoveringLineIndex);
    }

    public void HandlePointerWheelChanged(PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);

        if (!IsPointerInsideLyricsContainer(pointerPoint.Position)) return;

        IsMouseScrolling = true;

        var mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        var isVertical = _lyricsWindowStatus?.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

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
        return
            _renderLyricsStartX <= position.X && position.X <= _renderLyricsStartX + _renderLyricsWidth &&
            _renderLyricsStartY <= position.Y && position.Y <= _renderLyricsStartY + _renderLyricsHeight;
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

        if (_renderLyricsOpacity == 1) _lyricsRenderer.Draw(sender, ds);
    }

    private void DrawCoreWithEdgeFeatheringHandled(ICanvasAnimatedControl sender, CanvasDrawingSession ds,
        Rect bounds, Color overlayColor, double finalOpacity,
        LyricsStyleSettings lyricsStyle, AlbumArtAreaStyleSettings albumStyle, LyricsBackgroundSettings lyricsBg)
    {
        if (_lyricsWindowStatus == null) return;

        if (_lyricsWindowStatus.IsEdgeFeatheringEnabled && _edgeFadeMaskRenderer.Brush != null)
            using (ds.CreateLayer(_edgeFadeMaskRenderer.Brush))
            {
                DrawCore(sender, ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg);
            }
        else
            DrawCore(sender, ds, bounds, overlayColor, finalOpacity, lyricsStyle, albumStyle, lyricsBg);
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
            _layoutStrategy =
                _lyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation ==
                LyricsLayoutOrientation.Horizontal
                    ? new HorizontalLyricsLayoutStrategy()
                    : new VerticalLyricsLayoutStrategy();
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
        }
    }

    private void RequestRelayout()
    {
        _ = _layoutDebouncer.RunAsync(() => { _isLayoutChanged = true; });
    }

    private void RequestReloadLyrics()
    {
        _ = _lyricsDebouncer.RunAsync(() => { _isLyricsChanged = true; });
    }

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

    private void ResetPlaybackState()
    {
        _songPosition = TimeSpan.Zero;
    }

    private async Task ReloadCoverBackgroundResourcesAsync()
    {
        if (Canvas == null || Canvas.Device == null) return;

        try
        {
            // ֱ�ӻ�ȡ����Ĵ��ֽ�����
            var imageBytes = _gsmtcService.AlbumArtBytes;
            if (imageBytes == null || imageBytes.Length == 0) return;

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

    /// <summary>
    ///     Ϊ���������ڵ���Ԥ�� UI ��������ʱ��
    /// </summary>
    private void EnsureRenderLyricsLinesPreservedAnimation()
    {
        if (_lyricsWindowStatus == null) return;
        if (_renderLyricsLines == null) return;

        if (!_lyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled &&
            !_lyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled) return;

        var animationPadding = (int)Time.AnimationDuration.TotalMilliseconds;
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
            // ��������һ�䣬ʹ�ø����ܳ���Ϊ�ο�
            var nextLineStartMs = isLastLine ? (int)_gsmtcService.CurrentSongInfo.DurationMs : lines[i + 1].StartMs;

            // ������һ�������Ƿ����㳤��������
            var isLongSyllable = line.PrimaryRenderSyllables.LastOrDefault()?.DurationMs >= longSyllableThreshold;

            // �������һ�������ǳ�����ʱ�����ϵ�����������ʱ�䣨Padding��
            if (line.EndMs.HasValue && isLongSyllable)
            {
                if (isLastLine || line.EndMs > nextLineStartMs)
                {
                    // ���һ��򱳾�/ƽ�и�ʣ�ֱ�Ӽ��ϻ���ʱ��
                    line.EndMs += animationPadding;
                }
                else
                {
                    // ������ʣ����Լ��϶���ʱ�䣬�����ܳ�����һ��Ŀ�ʼʱ��
                    var targetEndMs = line.EndMs.Value + animationPadding;

                    // ���Ʋ�������һ��Ŀ�ʼʱ�䣬����ȷ�������ԭ�������� EndMs ����
                    line.EndMs = Math.Max(line.EndMs.Value, Math.Min(targetEndMs, nextLineStartMs));
                }
            }
        }
    }
}