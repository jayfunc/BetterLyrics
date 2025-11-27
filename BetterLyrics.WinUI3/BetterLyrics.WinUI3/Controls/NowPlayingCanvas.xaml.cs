// 2025/6/23 by Zhe Fang

using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Logic;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Renderer;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class NowPlayingCanvas : UserControl,
        IRecipient<PropertyChangedMessage<Color>>,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<TimeSpan>>,
        IRecipient<PropertyChangedMessage<LyricsData?>>,
        IRecipient<PropertyChangedMessage<LyricsWindowStatus>>
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ILastFMService _lastFMService = Ioc.Default.GetRequiredService<ILastFMService>();

        private readonly LyricsRenderer _lyricsRenderer = new();
        private readonly FluidBackgroundRenderer _fluidRenderer = new();
        private readonly PureColorBackgroundRenderer _pureColorRenderer = new();
        private readonly SnowRenderer _snowRenderer = new();
        private readonly FogRenderer _fogRenderer = new();
        private readonly SpectrumRenderer _spectrumRenderer = new();

        private readonly LyricsSynchronizer _synchronizer = new();
        private readonly LyricsLayoutManager _layoutManager = new();
        private readonly LyricsAnimator _animator = new();
        private readonly LyricsThemeManager _themeManager;

        private readonly SpectrumAnalyzer _spectrumAnalyzer = new();

        private Color _environmentalColor = Colors.Transparent;
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
            easingType: EasingType.EaseInOutQuad
        );

        private TimeSpan _songPosition; // 当前歌曲时刻
        private TimeSpan _totalPlayedTime; // 当前歌曲播放总时长（包括来来回回重复播放的时间）
        private bool _isLastFMTracked = false;

        private double _renderLyricsStartX = 0;
        private double _renderLyricsStartY = 0;
        private double _renderLyricsWidth = 0;
        private double _renderLyricsHeight = 0;
        private double _renderLyricsOpacity = 0;

        private bool _isLayoutChanged = true;
        private int _playingLineIndex;
        private (int Start, int End) _visibleRange;
        private double _canvasTargetScrollOffset;
        private LyricsThemeColors _currentThemeColors;

        public TimeSpan SongPosition => _songPosition;

        // 歌词区域起始横 X 坐标
        public double LyricsStartX
        {
            get { return (double)GetValue(LyricsStartXProperty); }
            set { SetValue(LyricsStartXProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartXProperty =
            DependencyProperty.Register(nameof(LyricsStartX), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnLayoutPropChanged));

        // 歌词区域起始 Y 坐标
        public double LyricsStartY
        {
            get { return (double)GetValue(LyricsStartYProperty); }
            set { SetValue(LyricsStartYProperty, value); }
        }

        public static readonly DependencyProperty LyricsStartYProperty =
            DependencyProperty.Register(nameof(LyricsStartY), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnLayoutPropChanged));

        // 歌词区域最大宽度
        public double LyricsWidth
        {
            get { return (double)GetValue(LyricsWidthProperty); }
            set { SetValue(LyricsWidthProperty, value); }
        }

        public static readonly DependencyProperty LyricsWidthProperty =
            DependencyProperty.Register(nameof(LyricsWidth), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnLayoutPropChanged));

        // 歌词区域最大高度
        public double LyricsHeight
        {
            get { return (double)GetValue(LyricsHeightProperty); }
            set { SetValue(LyricsHeightProperty, value); }
        }

        public static readonly DependencyProperty LyricsHeightProperty =
            DependencyProperty.Register(nameof(LyricsHeight), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnLayoutPropChanged));

        // 歌词区域不透明度
        public double LyricsOpacity
        {
            get { return (double)GetValue(LyricsOpacityProperty); }
            set { SetValue(LyricsOpacityProperty, value); }
        }

        public static readonly DependencyProperty LyricsOpacityProperty =
            DependencyProperty.Register(nameof(LyricsOpacity), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0, OnLayoutPropChanged));

        public NowPlayingCanvas()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<Color>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<BitmapImage?>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<TimeSpan>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsData?>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsWindowStatus>>(this);

            _themeManager = new(_mediaSessionsService);
        }

        private static void OnLayoutPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NowPlayingCanvas canvas)
            {
                if (e.Property == LyricsStartXProperty)
                {
                    canvas._renderLyricsStartX = Convert.ToDouble(e.NewValue);
                }
                else if (e.Property == LyricsStartYProperty)
                {
                    canvas._renderLyricsStartY = Convert.ToDouble(e.NewValue);
                }
                else if (e.Property == LyricsWidthProperty)
                {
                    canvas._renderLyricsWidth = Convert.ToDouble(e.NewValue);
                }
                else if (e.Property == LyricsHeightProperty)
                {
                    canvas._renderLyricsHeight = Convert.ToDouble(e.NewValue);
                }
                else if (e.Property == LyricsOpacityProperty)
                {
                    canvas._renderLyricsOpacity = Convert.ToDouble(e.NewValue);
                }

                canvas.TriggerRelayout();
            }
        }

        // ====

        private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            var bounds = new Rect(0, 0, sender.Size.Width, sender.Size.Height);

            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            var albumArtLayout = status.AlbumArtLayoutSettings;
            var lyricsBg = status.LyricsBackgroundSettings;
            var lyricsStyle = status.LyricsStyleSettings;
            var lyricsEffect = status.LyricsEffectSettings;

            var lyricsData = _mediaSessionsService.CurrentLyricsData;
            double songDuration = _mediaSessionsService.CurrentSongInfo?.DurationMs ?? 0;
            bool isForceWordByWord = _settingsService.AppSettings.GeneralSettings.IsForceWordByWordEffect;

            double fixedSongPositionMs = _songPosition.TotalMilliseconds + (_mediaSessionsService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0);

            Color overlayColor;
            double finalOpacity;

            if (status.IsAdaptToEnvironment)
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

            _fluidRenderer.Opacity = lyricsBg.FluidOverlayOpacity;
            _fluidRenderer.IsEnabled = lyricsBg.IsFluidOverlayEnabled;
            _fluidRenderer.Draw(sender, args.DrawingSession);

            _snowRenderer.Draw(sender, args.DrawingSession);

            _fogRenderer.Draw(sender, args.DrawingSession);

            _lyricsRenderer.Draw(
                control: sender,
                ds: args.DrawingSession,
                lyricsData: _mediaSessionsService.CurrentLyricsData,
                playingLineIndex: _playingLineIndex,
                startVisibleIndex: _visibleRange.Start,
                endVisibleIndex: _visibleRange.End,
                canvasHeight: sender.Size.Height,
                lyricsX: _renderLyricsStartX,
                lyricsWidth: _renderLyricsWidth,
                windowStatus: status,
                strokeColor: _currentThemeColors.StrokeFontColor,
                bgColor: _currentThemeColors.BgFontColor,
                getPlaybackState: (lineIndex) =>
                {
                    if (lyricsData == null) return new LinePlaybackState();

                    var line = lyricsData.LyricsLines.ElementAtOrDefault(lineIndex);
                    if (line == null) return new LinePlaybackState();

                    var nextLine = lyricsData.LyricsLines.ElementAtOrDefault(lineIndex + 1);

                    return _synchronizer.GetLinePlayingProgress(
                        fixedSongPositionMs,
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
                    barCount: _spectrumAnalyzer?.BarCount ?? 0,
                    isEnabled: lyricsBg.IsSpectrumOverlayEnabled,
                    placement: lyricsBg.SpectrumPlacement,
                    canvasWidth: sender.Size.Width,
                    canvasHeight: sender.Size.Height,
                    fillColor: _currentThemeColors.BgFontColor
                );
            }

        }

        private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            var lyricsBg = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings;
            var lyricsEffect = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings;

            TimeSpan elapsedTime = args.Timing.ElapsedTime;

            _accentColor1Transition.Update(elapsedTime);
            _accentColor2Transition.Update(elapsedTime);
            _accentColor3Transition.Update(elapsedTime);
            _accentColor4Transition.Update(elapsedTime);

            _immersiveBgOpacityTransition.Update(elapsedTime);
            _immersiveBgColorTransition.Update(elapsedTime);

            UpdatePlaybackState(elapsedTime);

            #region UpdatePlayingLineIndex

            int newPlayingIndex = _synchronizer.GetCurrentLineIndex(_songPosition.TotalMilliseconds, _mediaSessionsService.CurrentLyricsData);
            bool isPlayingLineChanged = newPlayingIndex != _playingLineIndex;
            _playingLineIndex = newPlayingIndex;

            #endregion

            #region UpdateTargetScrollOffset

            var targetScroll = _layoutManager.CalculateTargetScrollOffset(_mediaSessionsService.CurrentLyricsData, _playingLineIndex);
            if (targetScroll.HasValue) _canvasTargetScrollOffset = targetScroll.Value;

            _canvasYScrollTransition.StartTransition(_canvasTargetScrollOffset);
            _canvasYScrollTransition.Update(elapsedTime);

            #endregion

            _visibleRange = _layoutManager.CalculateVisibleRange(
                _mediaSessionsService.CurrentLyricsData?.LyricsLines,
                _canvasYScrollTransition.Value, // 当前滚动位置
                sender.Size.Height
            );

            _animator.UpdateVisibleLines(
                _mediaSessionsService.CurrentLyricsData,
                _visibleRange.Start,
                _visibleRange.End,
                _playingLineIndex,
                sender.Size.Height,
                _canvasTargetScrollOffset,
                _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings,
                _canvasYScrollTransition,
                elapsedTime,
                isPlayingLineChanged || _isLayoutChanged
            );

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
                _spectrumAnalyzer.BarCount = 64;
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
            Canvas.RemoveFromVisualTree();
            Canvas = null;

            _fluidRenderer.Dispose();
            _snowRenderer.Dispose();
            _fogRenderer.Dispose();
            _spectrumRenderer.Dispose();
            DisposeAnalyzer();
        }

        private async void Canvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(_fluidRenderer.LoadResourcesAsync().AsAsyncAction());
            _snowRenderer.LoadResources();
            _fogRenderer.LoadResources();

            TriggerRelayout();
        }

        // ====

        private void UpdateColorConfig()
        {
            var themeColors = _themeManager.UpdateColors(
                _liveStatesService.LiveStates.LyricsWindowStatus,
                _environmentalColor,
                _accentColor1Transition,
                _accentColor2Transition,
                _accentColor3Transition,
                _accentColor4Transition
            );

            _currentThemeColors = themeColors;

            _accentColor1Transition.StartTransition(_currentThemeColors.AccentColor1);
            _accentColor2Transition.StartTransition(_currentThemeColors.AccentColor2);
            _accentColor3Transition.StartTransition(_currentThemeColors.AccentColor3);
            _accentColor4Transition.StartTransition(_currentThemeColors.AccentColor4);
        }

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
            if (_layoutManager == null || _mediaSessionsService.CurrentLyricsData == null) return;

            _layoutManager.MeasureAndArrange(
                resourceCreator: Canvas,
                lyricsData: _mediaSessionsService.CurrentLyricsData,
                status: _liveStatesService.LiveStates.LyricsWindowStatus,
                appSettings: _settingsService.AppSettings,
                canvasWidth: Canvas.Size.Width,
                canvasHeight: Canvas.Size.Height,
                lyricsWidth: _renderLyricsWidth
            );

            _isLayoutChanged = true;
        }

        private void UpdatePlaybackState(TimeSpan elapsedTime)
        {
            if (_mediaSessionsService.CurrentIsPlaying)
            {
                _songPosition += elapsedTime;
                _totalPlayedTime += elapsedTime;
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
            _totalPlayedTime = TimeSpan.Zero;
            _totalPlayedTime = TimeSpan.Zero;
            _isLastFMTracked = false;
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (_mediaSessionsService.CurrentSongInfo == null
                || _mediaSessionsService.CurrentLyricsData == null
                || _mediaSessionsService.CurrentLyricsData.LyricsLines.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _mediaSessionsService.CurrentLyricsData.LyricsLines.Count - 1);
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.BackdropAccentColor))
                {
                    var newColor = message.NewValue;

                    _immersiveBgColorTransition.StartTransition(newColor);
                    _environmentalColor = newColor;

                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapImage))
                {
                    UpdateColorConfig();
                }
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
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (IsLoaded)
                        {
                            TriggerRelayout();
                        }
                    });
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsWindowStatus> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsWindowStatus))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (IsLoaded)
                        {
                            TriggerRelayout();
                        }
                    });
                }
            }
        }

    }
}
