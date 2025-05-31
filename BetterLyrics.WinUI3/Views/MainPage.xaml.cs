using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Color = Windows.UI.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views {
    /// <summary>
    /// Please note that this page was implemented by traditional XAML + C# code-behind, not MVVM.
    /// I tried before but there was a difficulty in accessing UI thread on non-UI threads via MVVM.
    /// </summary>
    public sealed partial class MainPage : Page {

        public MainViewModel ViewModel => (MainViewModel)DataContext;
        public SettingsViewModel SettingsViewModel { get; private set; }

        private CanvasBitmap _canvasCoverBitmap;
        private OpacityEffect _coverBitmapEffect;
        private float _coverBitmapRotateAngle = 0;
        private float _coverScaleFactor = 1;
        private float _coverOverlayBlurAmount;

        private LyricsAlignmentType _lyricsAlignmentType;

        private float _lyricsBlurAmount;

        private bool _isCoverOverlayEnabled;
        private bool _isDynamicCoverOverlay;
        private float _coverOverlayOpacity;

        private float _coverRotateSpeed = 0.003f;

        private CanvasTextFormat _textFormat = new CanvasTextFormat() {
            FontSize = 28,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            FontWeight = FontWeights.Bold,
            FontFamily = "Segoe UI Mono",
        };

        private int _animationDurationMs = 200;

        private DispatcherQueueTimer _queueTimer;

        private TimeSpan _currentTime = TimeSpan.Zero;

        private float _defaultOpacity = 0.3f;
        private float _highlightedOpacity = 1.0f;

        private float _defaultScale = 0.95f;
        private float _highlightedScale = 1.0f;

        private int _lineEnteringDurationMs = 800;
        private int _lineExitingDurationMs = 800;
        private int _lineScrollDurationMs = 800;

        private float _scrollOffsetY = 0.0f;

        private double _lyricsAreaWidth = 0.0f;
        private double _lyricsAreaHeight = 0.0f;

        private double _lyricsCanvasLeftMargin = 0;
        private double _lyricsCanvasRightMargin = 36;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private ColorThief _colorThief = new();

        private List<LyricsLine> _lyricsLines = [];

        private Color _lyricsColor;

        public MainPage() {
            this.InitializeComponent();

            ActualThemeChanged += MainPage_ActualThemeChanged;

            _coverBitmapEffect = new OpacityEffect {
                Source = new GaussianBlurEffect {
                    BlurAmount = _coverOverlayBlurAmount,
                    Source = new ScaleEffect {
                        InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
                        BorderMode = EffectBorderMode.Hard
                    }
                }
            };

            DataContext = Ioc.Default.GetService<MainViewModel>();
            ViewModel.RefreshMusicMetadataIndexDatabase();

            SettingsViewModel = Ioc.Default.GetService<SettingsViewModel>();
            SettingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;

            // Backdrop
            _isCoverOverlayEnabled = SettingsViewModel.IsCoverOverlayEnabled;
            _isDynamicCoverOverlay = SettingsViewModel.IsDynamicCoverOverlay;
            _coverOverlayOpacity = (float)(SettingsViewModel.CoverOverlayOpacity / 100.0);
            _coverOverlayBlurAmount = SettingsViewModel.CoverOverlayBlurAmount;

            // Lyrics
            _lyricsAlignmentType = (LyricsAlignmentType)SettingsViewModel.LyricsAlignmentType;
            _lyricsBlurAmount = SettingsViewModel.LyricsBlurAmount;

            _queueTimer = _dispatcherQueue.CreateTimer();
        }

        private void MainPage_ActualThemeChanged(FrameworkElement sender, object args) {
            SetLyricsColor();
        }

        private void SetLyricsColor() {
            _lyricsColor = ((SolidColorBrush)LyricsCanvas.Foreground).Color;
        }

        private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                // Backdrop
                case nameof(SettingsViewModel.IsCoverOverlayEnabled):
                    _isCoverOverlayEnabled = SettingsViewModel.IsCoverOverlayEnabled;
                    break;
                case nameof(SettingsViewModel.IsDynamicCoverOverlay):
                    _isDynamicCoverOverlay = SettingsViewModel.IsDynamicCoverOverlay;
                    break;
                case nameof(SettingsViewModel.CoverOverlayOpacity):
                    _coverOverlayOpacity = (float)(SettingsViewModel.CoverOverlayOpacity / 100.0);
                    break;
                case nameof(SettingsViewModel.CoverOverlayBlurAmount):
                    _coverOverlayBlurAmount = SettingsViewModel.CoverOverlayBlurAmount;
                    break;
                // Lyrics
                case nameof(SettingsViewModel.LyricsAlignmentType):
                    _lyricsAlignmentType = (LyricsAlignmentType)SettingsViewModel.LyricsAlignmentType;
                    ReLayoutLyricsCanvas();
                    break;
                case nameof(SettingsViewModel.LyricsBlurAmount):
                    _lyricsBlurAmount = SettingsViewModel.LyricsBlurAmount;
                    break;
                default:
                    break;
            }
        }

        private async void InitMediaManager() {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
            _sessionManager.SessionsChanged += SessionManager_SessionsChanged;

            _currentSession = _sessionManager.GetCurrentSession();

            if (_currentSession != null) {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
                await UpdateUIMediaInfoAsync();
                await DetectPlayingStatusAsync();
            } else {
                await UpdateUIMediaInfoAsync();
            }
        }

        private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => {
                await DetectPlayingStatusAsync();
            });
        }

        /// <summary>
        /// Note: Non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => {
                await DetectPlayingStatusAsync();
            });
        }

        private void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
            Debug.WriteLine("SessionManager_SessionsChanged");
        }

        private async void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
            Debug.WriteLine("SessionManager_CurrentSessionChanged");
            // Unregister events associated with the previous session  
            if (_currentSession != null) {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
            }

            // Record and register events for current session
            _currentSession = sender.GetCurrentSession();

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () => {
                await UpdateUIMediaInfoAsync();
            });

            if (_currentSession != null) {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
            }
        }

        private async Task DetectPlayingStatusAsync() {
            if (_currentSession == null) {
                await UpdateUIMediaInfoAsync();
                return;
            }

            _currentTime = _currentSession.GetTimelineProperties().Position;

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            UpdateScaleAndOpacity(currentPlayingLineIndex);
            UpdatePosition(currentPlayingLineIndex, true);

            var playbackStatus = _currentSession.GetPlaybackInfo().PlaybackStatus;

            switch (playbackStatus) {
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                    await UpdateUIMediaInfoAsync();
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened:
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing:
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                    LyricsCanvas.Paused = true;
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                    LyricsCanvas.Paused = false;
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                    LyricsCanvas.Paused = true;
                    break;
                default:
                    break;
            }
        }

        private async Task UpdateUIMediaInfoAsync() {
            ViewModel.AboutToUpdateUI = true;

            //await Task.Delay(_animationDurationMs);

            CoverImage.Source = null;
            LyricsCanvas.Paused = true;
            _startVisibleLineIndex = _endVisibleLineIndex = -1;
            _lyricsLines = [];
            _currentTime = TimeSpan.Zero;

            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;

            if (_currentSession != null) {
                mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
            }

            ViewModel.Title = mediaProps?.Title;
            ViewModel.Artist = mediaProps?.Artist;

            byte[]? imgByteFromTag = null;

            var track = ViewModel.GetMusicMetadata(mediaProps?.Title, mediaProps?.Artist);

            if (track != null) {

                // Get picture from tag
                if (track.EmbeddedPictures.Count > 0) {
                    imgByteFromTag = track.EmbeddedPictures[0].PictureData;
                }

                // Get lyrics
                _lyricsLines = ViewModel.GetLyrics(track);

            }

            if (_lyricsLines.Count == 0) {
                Grid.SetColumnSpan(SongInfoStackPanel, 3);
            } else {
                Grid.SetColumnSpan(SongInfoStackPanel, 1);
            }

            ReLayoutLyricsCanvas();

            IRandomAccessStream? stream = null;
            var coverImgStreamFromMediaProps = mediaProps?.Thumbnail;

            // Set stream for cover image
            if (coverImgStreamFromMediaProps == null) {
                if (imgByteFromTag != null) {
                    stream = await ImageHelper.GetStreamFromBytesAsync(imgByteFromTag);
                }
            } else {
                stream = await coverImgStreamFromMediaProps.OpenReadAsync();
            }

            // Set cover image and dominant colors
            if (stream == null) {
                for (int i = 0; i < 3; i++) {
                    ViewModel.CoverImageDominantColors[i] = Colors.Transparent;
                }
            } else {
                _canvasCoverBitmap = await CanvasBitmap.LoadAsync(LyricsCanvas, stream);

                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                CoverImage.Source = bitmapImage;

                stream.Seek(0);

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var quantizedColors = await _colorThief.GetPalette(decoder, 3);
                for (int i = 0; i < 3; i++) {
                    QuantizedColor quantizedColor = quantizedColors[i];
                    ViewModel.CoverImageDominantColors[i] = Color.FromArgb(
                        quantizedColor.Color.A, quantizedColor.Color.R, quantizedColor.Color.G, quantizedColor.Color.B);
                }
            }

            LyricsCanvas.Paused = false;

            await Task.Delay(1);
            _currentTime = TimeSpan.Zero;

            ViewModel.AboutToUpdateUI = false;
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
            Debug.WriteLine("CurrentSession_MediaPropertiesChanged");
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () => {
                await UpdateUIMediaInfoAsync();
            });

        }

        private void CreateGlowEffect(TextBlock textBlock) {
            // Get 'Compositor' for lyricsLineCharTextBlock
            var compositor = ElementCompositionPreview.GetElementVisual(textBlock).Compositor;

            // Create DropShadow
            var dropShadow = compositor.CreateDropShadow();
            dropShadow.Color = (Color)Resources["SystemBaseHighColor"];
            dropShadow.BlurRadius = 5;
            dropShadow.Opacity = 0;
            dropShadow.Mask = textBlock.GetAlphaMask();

            // Create SpriteVisual
            var spriteVisual = compositor.CreateSpriteVisual();
            spriteVisual.Size = new Vector2(
                (float)textBlock.ActualWidth,
                (float)textBlock.ActualHeight);
            spriteVisual.Shadow = dropShadow;
            ((ContainerVisual)ElementCompositionPreview.GetElementVisual(textBlock)).Children.InsertAtBottom(spriteVisual);

            // Create opacity animation for compositor
            var animation = compositor.CreateScalarKeyFrameAnimation();
            //animation.InsertKeyFrame(0.0f, 0.0f);
            animation.InsertKeyFrame(0.5f, 1.0f);
            animation.InsertKeyFrame(1.0f, 0.0f);
            animation.Duration = TimeSpan.FromMilliseconds(1);

            // Store dropShadow and animation into Tag
            var tag = new List<object> {
                dropShadow,
                animation
            };
            textBlock.Tag = tag;
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            //_queueTimer.Debounce(async () => {

            _lyricsAreaHeight = LyricsGrid.ActualHeight;
            _lyricsAreaWidth = LyricsGrid.ActualWidth;

            if (SongInfoColumnDefinition.ActualWidth == 0) {
                _lyricsCanvasLeftMargin = 36;
            } else {
                _lyricsCanvasLeftMargin = 36 + SongInfoColumnDefinition.ActualWidth + 36;
            }

            ReLayoutLyricsCanvas();

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            UpdatePosition(currentPlayingLineIndex, true);

            //}, TimeSpan.FromMilliseconds(50));
        }

        private void ReLayoutLyricsCanvas() {
            float x = (float)_lyricsCanvasLeftMargin; // Left margin
            float y = (float)_lyricsAreaHeight / 2; // Top margin
            float maxWidth = (float)(_lyricsAreaWidth - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin);

            // Init Positions
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var line = _lyricsLines[i];

                // Calculate layout bounds
                using var layout = new CanvasTextLayout(LyricsCanvas.Device, line.Text, _textFormat, maxWidth, (float)_lyricsAreaHeight);
                var bounds = layout.LayoutBounds;

                var centerX = x;
                var centerY = y;

                switch (_lyricsAlignmentType) {
                    case LyricsAlignmentType.Left:
                        break;
                    case LyricsAlignmentType.Center:
                        centerX += (float)bounds.Width / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        centerX += (float)bounds.Width / 2;
                        break;
                    default:
                        break;
                }

                line.CenterPosition = new Vector2(centerX, (float)centerY);
                line.Position = line.PositionBeforeScrolling = new Vector2(x, y);
                line.LayoutBounds = bounds;

                y += (float)bounds.Height + _textFormat.FontSize;

            }

        }

        private void LyricsCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args) {
            using var ds = args.DrawingSession;

            var r = _lyricsColor.R;
            var g = _lyricsColor.G;
            var b = _lyricsColor.B;

            using var maskLayer = new CanvasCommandList(sender);
            using (var maskDs = maskLayer.CreateDrawingSession()) {
                DrawGradientOpacityMask(sender, maskDs, r, g, b);
            }

            using var lyricsLayer = new CanvasCommandList(sender);
            using (var lyricsDs = lyricsLayer.CreateDrawingSession()) {
                DrawLyrics(sender, lyricsDs, r, g, b);
            }

            // Create lyrics ICanvasImage resource
            using var combinedLayer = new CanvasCommandList(sender);
            using (var combinedDs = combinedLayer.CreateDrawingSession()) {
                // Draw (dynamic) cover image as the 1st layer
                if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {
                    DrawCoverImage(sender, combinedDs, _isDynamicCoverOverlay);
                }
                using var alphaMaskedLyrics = new AlphaMaskEffect {
                    Source = lyricsLayer,
                    AlphaMask = maskLayer
                };
                // Draw masked lyrics as the 2nd layer
                combinedDs.DrawImage(alphaMaskedLyrics);
            }

            // Draw combined lyrics as the 1st layer
            ds.DrawImage(combinedLayer);

            float step = 0.1f;

            // Draw blurs as the 2nd, 3rd, ... layer
            if (_lyricsBlurAmount != 0) {
                for (float i = 0; i <= 0.4; i += step) {
                    using var maskBrush = new CanvasLinearGradientBrush(sender, [
                        new() { Position = i, Color =  Color.FromArgb(255, r, g, b)},
                        new() { Position = i + step, Color =  Color.FromArgb(0, r, g, b)},
                        new() { Position = 1 - i - step, Color = Color.FromArgb(0, r, g, b)},
                        new() { Position = 1 - i, Color =  Color.FromArgb(255, r, g, b)},
                    ]) {
                        StartPoint = new Vector2(0, 0),
                        EndPoint = new Vector2(0, (float)sender.Size.Height)
                    };
                    using (ds.CreateLayer(maskBrush)) {
                        using var blurredLyrics = new GaussianBlurEffect {
                            Source = combinedLayer,
                            BlurAmount = _lyricsBlurAmount * (1 - i / 0.4f),
                            Optimization = EffectOptimization.Quality,
                            BorderMode = EffectBorderMode.Hard,
                        };
                        ds.DrawImage(blurredLyrics);
                    }
                }
            }

        }

        private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds, byte r, byte g, byte b) {

            var (displayStartLineIndex, displayEndLineIndex) = GetVisibleLyricsLineIndexBoundaries();

            for (int i = displayStartLineIndex; _lyricsLines.Count > 0 && i >= 0 && i <= displayEndLineIndex; i++) {

                var line = _lyricsLines[i];

                float progressPerChar = 1f / line.Text.Length;

                var position = line.Position;

                float centerX = position.X;
                float centerY = (float)(position.Y + line.LayoutBounds.Height / 2);

                using var layout = new CanvasTextLayout(control, line.Text, _textFormat, (float)(control.Size.Width - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin), (float)_lyricsAreaHeight);

                switch (_lyricsAlignmentType) {
                    case LyricsAlignmentType.Left:
                        layout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        layout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        break;
                    case LyricsAlignmentType.Right:
                        layout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        break;
                    default:
                        break;
                }

                var adjustedPosition = new Vector2(position.X, position.Y - (float)_lyricsAreaHeight / 2);

                int startIndex = 0;

                // Set brush
                for (int j = 0; j < layout.LineCount; j++) {

                    int count = layout.LineMetrics[j].CharacterCount;
                    var regions = layout.GetCharacterRegions(startIndex, count);
                    float subLinePlayingProgress = Math.Clamp((line.PlayingProgress * line.Text.Length - startIndex) / count, 0, 1);

                    using var horizontalFillBrush = new CanvasLinearGradientBrush(control, [
                        new() { Position = 0, Color = Color.FromArgb((byte)(255 * line.Opacity), r, g, b) },
                        new() { Position = subLinePlayingProgress * (1 + progressPerChar) - progressPerChar, Color = Color.FromArgb((byte)(255 * line.Opacity), r, g, b) },
                        new() { Position = subLinePlayingProgress * (1 + progressPerChar), Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b) },
                        new() { Position = 1.5f, Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b) },
                    ]) {
                        StartPoint = new Vector2((float)(regions[0].LayoutBounds.Left + line.Position.X), 0),
                        EndPoint = new Vector2((float)(regions[^1].LayoutBounds.Right + line.Position.X), 0)
                    };

                    layout.SetBrush(startIndex, count, horizontalFillBrush);
                    startIndex += count;

                }

                // Scale
                ds.Transform = Matrix3x2.CreateScale(line.Scale, new Vector2(centerX, centerY));

                ds.DrawTextLayout(layout, adjustedPosition, Colors.Transparent);

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }

        }

        private void DrawCoverImage(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool dynamic) {
            if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {

                if (dynamic) {
                    ds.Transform = Matrix3x2.CreateRotation(_coverBitmapRotateAngle, control.Size.ToVector2() * 0.5f);
                }
                ds.DrawImage(
                _coverBitmapEffect,
                    (float)control.Size.Width / 2 - _canvasCoverBitmap.SizeInPixels.Width * _coverScaleFactor / 2,
                    (float)control.Size.Height / 2 - _canvasCoverBitmap.SizeInPixels.Height * _coverScaleFactor / 2);
                if (dynamic) {
                    ds.Transform = Matrix3x2.Identity;
                }
            }
        }

        private static void DrawGradientOpacityMask(ICanvasAnimatedControl control, CanvasDrawingSession ds, byte r, byte g, byte b) {
            using var maskBrush = new CanvasLinearGradientBrush(control, [
                new() { Position = 0, Color =  Color.FromArgb(0, r, g, b)},
                new() { Position = 0.5f, Color = Color.FromArgb(255, r, g, b)},
                new() { Position = 1, Color =  Color.FromArgb(0, r, g, b)},
            ]) {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height)
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        private void LyricsCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args) {
            _currentTime += args.Timing.ElapsedTime;
            _coverBitmapRotateAngle += _coverRotateSpeed;

            if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {

                var diagonal = Math.Sqrt(Math.Pow(_lyricsAreaWidth, 2) + Math.Pow(_lyricsAreaHeight, 2));

                _coverScaleFactor = (float)diagonal / Math.Min(
                    _canvasCoverBitmap.SizeInPixels.Width,
                    _canvasCoverBitmap.SizeInPixels.Height);

                _coverBitmapEffect.Opacity = _coverOverlayOpacity;
                var blurEffect = (_coverBitmapEffect.Source as GaussianBlurEffect)!;
                blurEffect.BlurAmount = _coverOverlayBlurAmount;
                var scaleEffect = (blurEffect.Source as ScaleEffect)!;
                scaleEffect.Source = _canvasCoverBitmap;
                scaleEffect.Scale = new Vector2(_coverScaleFactor);
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            UpdateScaleAndOpacity(currentPlayingLineIndex);
            UpdatePosition(currentPlayingLineIndex);
        }

        private int GetCurrentPlayingLineIndex() {
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var line = _lyricsLines[i];
                if (line.EndTimestampMs < _currentTime.TotalMilliseconds) {
                    continue;
                }
                return i;
            }

            return -1;

        }

        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries() {
            //Debug.WriteLine($"{_startVisibleLineIndex} {_endVisibleLineIndex}");
            return new Tuple<int, int>(_startVisibleLineIndex, _endVisibleLineIndex);
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries() {
            return new Tuple<int, int>(0, _lyricsLines.Count - 1);
        }

        private void UpdateScaleAndOpacity(int currentPlayingLineIndex) {

            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            for (int i = startLineIndex; _lyricsLines.Count > 0 && i <= endLineIndex; i++) {

                var line = _lyricsLines[i];

                bool linePlaying = i == currentPlayingLineIndex;

                var lineEnteringDurationMs = Math.Min(line.DurationMs, _lineEnteringDurationMs);
                var lineExitingDurationMs = _lineExitingDurationMs;
                if (i + 1 <= endLineIndex) {
                    lineExitingDurationMs = Math.Min(_lyricsLines[i + 1].DurationMs, lineExitingDurationMs);
                }

                float lineEnteringProgress = 0.0f;
                float lineExitingProgress = 0.0f;

                bool lineEntering = false;
                bool lineExiting = false;

                float scale = _defaultScale;
                float opacity = _defaultOpacity;

                float playProgress = 0;

                if (linePlaying) {
                    line.PlayingState = LyricsPlayingState.Playing;

                    scale = _highlightedScale;
                    opacity = _highlightedOpacity;

                    playProgress = ((float)_currentTime.TotalMilliseconds - line.StartTimestampMs) / line.DurationMs;

                    if (playProgress >= 0.99) {
                        Debug.WriteLine($"{line.Text} 播放完毕");
                    }

                    var durationFromStartMs = _currentTime.TotalMilliseconds - line.StartTimestampMs;
                    lineEntering = durationFromStartMs <= lineEnteringDurationMs;
                    if (lineEntering) {
                        lineEnteringProgress = (float)durationFromStartMs / lineEnteringDurationMs;
                        scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity = _defaultOpacity + (_highlightedOpacity - _defaultOpacity) * (float)lineEnteringProgress;
                    }

                } else {
                    if (i < currentPlayingLineIndex) {
                        line.PlayingState = LyricsPlayingState.Played;
                        playProgress = 1;

                        var durationToEndMs = _currentTime.TotalMilliseconds - line.EndTimestampMs;
                        lineExiting = durationToEndMs <= lineExitingDurationMs;
                        if (lineExiting) {

                            lineExitingProgress = (float)durationToEndMs / lineExitingDurationMs;
                            scale = _highlightedScale - (_highlightedScale - _defaultScale) * (float)lineExitingProgress;
                            opacity = _highlightedOpacity - (_highlightedOpacity - _defaultOpacity) * (float)lineExitingProgress;
                        }

                    } else {
                        line.PlayingState = LyricsPlayingState.NotPlayed;
                    }
                }

                line.EnteringProgress = lineEnteringProgress;
                line.ExitingProgress = lineExitingProgress;

                line.Scale = scale;
                line.Opacity = opacity;

                line.PlayingProgress = playProgress;

            }
        }

        private void UpdatePosition(int currentPlayingLineIndex, bool alwaysScrollToCurrentPlayingLine = false) {

            if (currentPlayingLineIndex < 0) {
                return;
            }

            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            // Set _scrollOffsetY
            var currentPlayingLine = _lyricsLines[currentPlayingLineIndex];

            var lineScrollingProgress =
                (_currentTime.TotalMilliseconds - currentPlayingLine.StartTimestampMs) /
                Math.Min(_lineScrollDurationMs, currentPlayingLine.DurationMs);

            _scrollOffsetY =
                (float)(_lyricsAreaHeight / 2 - currentPlayingLine.PositionBeforeScrolling.Y) *
                EasingHelper.SmootherStep(Math.Min(1, (float)lineScrollingProgress));

            bool isScrollingNow = lineScrollingProgress <= 1;

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update Positions
            for (int i = startLineIndex; i >= 0 && i <= endLineIndex; i++) {

                var line = _lyricsLines[i];

                if (alwaysScrollToCurrentPlayingLine || isScrollingNow) {
                    line.Position = new Vector2(
                        line.PositionBeforeScrolling.X,
                        line.PositionBeforeScrolling.Y + _scrollOffsetY);
                } else {
                    line.PositionBeforeScrolling = line.Position;
                }

                if (line.Position.Y + line.LayoutBounds.Height >= 0) {
                    if (_startVisibleLineIndex == -1) {
                        _startVisibleLineIndex = i;
                    }
                }
                if (line.Position.Y + line.LayoutBounds.Height >= _lyricsAreaHeight) {
                    if (_endVisibleLineIndex == -1) {
                        _endVisibleLineIndex = i;
                    }
                }
            }

            if (_startVisibleLineIndex != -1 && _endVisibleLineIndex == -1) {
                _endVisibleLineIndex = endLineIndex;
            }

        }

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e) {
            SetLyricsColor();
            InitMediaManager();
        }

    }
}
