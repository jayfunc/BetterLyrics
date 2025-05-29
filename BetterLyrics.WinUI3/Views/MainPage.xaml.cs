using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.Graphics.Canvas.Text;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Microsoft.UI.Composition;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Graphics.Effects;
using BetterLyrics.WinUI3.Helper;
using Color = Windows.UI.Color;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using System.Collections.ObjectModel;
using Windows.Media.Control;
using Windows.Storage.Streams;
using ATL;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI;
using static System.Collections.Specialized.BitVector32;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas;
using System.Reflection.Metadata;
using Windows.Foundation;
using System.Reflection;

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

        private int _lineEnteringDurationMs = 1000;
        private int _lineExitingDurationMs = 1000;
        private int _lineScrollDurationMs = 1000;

        private float _scrollOffsetY = 0.0f;

        private double _lyricsAreaWidth = 0.0f;
        private double _lyricsAreaHeight = 0.0f;

        private double _lyricsCanvasLeftMargin = 0;

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
                    BlurAmount = 100f,
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

            _isCoverOverlayEnabled = SettingsViewModel.IsCoverOverlayEnabled;
            _isDynamicCoverOverlay = SettingsViewModel.IsDynamicCoverOverlay;
            _coverOverlayOpacity = (float)(SettingsViewModel.CoverOverlayOpacity / 100.0);

            _queueTimer = _dispatcherQueue.CreateTimer();
        }

        private void MainPage_ActualThemeChanged(FrameworkElement sender, object args) {
            SetLyricsColor();
        }

        private void SetLyricsColor() {
            _lyricsColor = ((SolidColorBrush)LyricsCanvas.Foreground).Color;
            Debug.WriteLine(_lyricsColor.ToString());
        }

        private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SettingsViewModel.IsCoverOverlayEnabled)) {
                _isCoverOverlayEnabled = SettingsViewModel.IsCoverOverlayEnabled;
            } else if (e.PropertyName == nameof(SettingsViewModel.IsDynamicCoverOverlay)) {
                _isDynamicCoverOverlay = SettingsViewModel.IsDynamicCoverOverlay;
            } else if (e.PropertyName == nameof(SettingsViewModel.CoverOverlayOpacity)) {
                _coverOverlayOpacity = (float)(SettingsViewModel.CoverOverlayOpacity / 100.0);
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
            var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();
            UpdateScaleAndOpacity(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex);
            UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex, true);

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
            var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();
            UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex, true);

            //}, TimeSpan.FromMilliseconds(50));
        }

        private void ReLayoutLyricsCanvas() {
            float x = (float)_lyricsCanvasLeftMargin;
            float y = (float)_lyricsAreaHeight / 2; // Top margin

            // Init Positions
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {

                    var lineChar = lyricsLine.LyricsLineChars[j];

                    // Calculate layout bounds
                    var layout = new CanvasTextLayout(LyricsCanvas.Device, lineChar.Text, _textFormat, (float)_lyricsAreaWidth, (float)_lyricsAreaHeight);
                    var bounds = layout.LayoutBounds;
                    lineChar.LayoutBounds = bounds;

                    // Line break by exceeding UI edge (for the current char)
                    float charWidth = (float)bounds.Width;
                    if (charWidth == 0) {
                        // Handle white-spaces
                        charWidth = (float)bounds.Height / 4;
                    }

                    if (x + charWidth + 36 > _lyricsAreaWidth) {
                        x = (float)_lyricsCanvasLeftMargin;
                        y += (float)bounds.Height * 1.1f;
                    }

                    var centerX = (float)_lyricsCanvasLeftMargin + bounds.Width / 2;
                    var centerY = bounds.Top + bounds.Height / 2;
                    lineChar.CenterPosition = new Vector2((float)centerX, (float)centerY);

                    lineChar.PositionBeforeScrolling = lineChar.Position = new Vector2(x, y);

                    // For th next char
                    x += charWidth;

                    // Line break by design (for the next char)
                    if (j == lyricsLine.LyricsLineChars.Count - 1) {
                        x = (float)_lyricsCanvasLeftMargin;
                        y += (float)bounds.Height * 1.8f;
                    }
                }
            }

        }

        private void LyricsCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args) {
            using var ds = args.DrawingSession;

            var r = _lyricsColor.R;
            var g = _lyricsColor.G;
            var b = _lyricsColor.B;

            if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {

                if (_isDynamicCoverOverlay) {
                    ds.Transform = Matrix3x2.CreateRotation(_coverBitmapRotateAngle, sender.Size.ToVector2() * 0.5f);
                }
                ds.DrawImage(
                    _coverBitmapEffect,
                    (float)sender.Size.Width / 2 - _canvasCoverBitmap.SizeInPixels.Width * _coverScaleFactor / 2,
                    (float)sender.Size.Height / 2 - _canvasCoverBitmap.SizeInPixels.Height * _coverScaleFactor / 2);
                ds.Transform = Matrix3x2.Identity;
            }

            var (displayStartLineIndex, displayEndLineIndex) = GetVisibleLyricsLineIndexBoundaries();

            for (int i = displayStartLineIndex; _lyricsLines.Count > 0 && i >= 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {
                    var lineChar = lyricsLine.LyricsLineChars[j];

                    using var fadeInOutBrush = new CanvasLinearGradientBrush(sender, [
                        new() { Position = 0, Color =  Color.FromArgb(0, r, g, b)},
                        new() { Position = 0.5f, Color = Color.FromArgb((byte)(255 * lineChar.Opacity), r, g, b)},
                        new() { Position = 1, Color =  Color.FromArgb(0, r, g, b)},
                    ]) {
                        StartPoint = new Vector2(0.5f, 0),
                        EndPoint = new Vector2(0.5f, (float)sender.Size.Height)
                    };

                    using var horizontalFillBrush = new CanvasLinearGradientBrush(sender, [
                        new() { Position = -0.05f, Color = Color.FromArgb((byte)(255 * _highlightedOpacity), r, g, b)},
                        new() { Position = lineChar.PlayingProgress * 1.5f - 0.5f, Color = Color.FromArgb((byte)(255 * _highlightedOpacity), r, g, b)},
                        new() { Position = lineChar.PlayingProgress * 1.5f, Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b)},
                        new() { Position = 1.5f, Color =  Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b)},
                    ]) {
                        StartPoint = new Vector2(lineChar.Position.X, 0),
                        EndPoint = new Vector2(lineChar.Position.X + (float)lineChar.LayoutBounds.Width, 0)
                    };

                    var position = lineChar.Position;

                    // Scale
                    ds.Transform = Matrix3x2.CreateScale(lineChar.Scale, lineChar.CenterPosition);

                    switch (lyricsLine.PlayingState) {
                        case LyricsPlayingState.Playing:
                            switch (lineChar.PlayingState) {
                                case LyricsPlayingState.NotPlayed:
                                    ds.DrawText(lineChar.Text, position, fadeInOutBrush, _textFormat);
                                    break;
                                case LyricsPlayingState.Playing:
                                    ds.DrawText(lineChar.Text, position, horizontalFillBrush, _textFormat);
                                    break;
                                case LyricsPlayingState.Played:
                                    ds.DrawText(lineChar.Text, position, _lyricsColor, _textFormat);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case LyricsPlayingState.NotPlayed:
                        case LyricsPlayingState.Played:
                        default:
                            ds.DrawText(lineChar.Text, position, fadeInOutBrush, _textFormat);
                            break;
                    }

                    // Reset scale
                    ds.Transform = Matrix3x2.Identity;
                }
            }

        }

        private void LyricsCanvas_Update(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args) {
            _currentTime += args.Timing.ElapsedTime;
            _coverBitmapRotateAngle += _coverRotateSpeed;

            if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {

                var diagonal = Math.Sqrt(Math.Pow(_lyricsAreaWidth, 2) + Math.Pow(_lyricsAreaHeight, 2));

                _coverScaleFactor = (float)diagonal / Math.Min(
                    _canvasCoverBitmap.SizeInPixels.Width,
                    _canvasCoverBitmap.SizeInPixels.Height);

                _coverBitmapEffect.Opacity = _coverOverlayOpacity;
                var scaleEffect = ((_coverBitmapEffect.Source as GaussianBlurEffect)!.Source as ScaleEffect)!;
                scaleEffect.Source = _canvasCoverBitmap;
                scaleEffect.Scale = new Vector2(_coverScaleFactor);
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();

            UpdateScaleAndOpacity(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex);

            UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex);
        }

        private int GetCurrentPlayingLineIndex() {
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var lyricsLine = _lyricsLines[i];
                if (lyricsLine.EndTimestampMs < _currentTime.TotalMilliseconds) {
                    continue;
                }
                return i;
            }
            return -1;

        }

        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries() {
            return new Tuple<int, int>(_startVisibleLineIndex, _endVisibleLineIndex);
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries() {
            return new Tuple<int, int>(0, _lyricsLines.Count - 1);
        }

        private void UpdateScaleAndOpacity(int displayStartLineIndex, int displayEndLineIndex, int currentPlayingLineIndex) {
            for (int i = displayStartLineIndex; i >= 0 && _lyricsLines.Count > 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                bool linePlaying = i == currentPlayingLineIndex;

                float lineEnteringProgress = 0.0f;

                if (linePlaying) {
                    var lineEnteringDurationMs = Math.Min(lyricsLine.DurationMs, _lineEnteringDurationMs);
                    lineEnteringProgress =
                        (float)Math.Min(1, (_currentTime.TotalMilliseconds - lyricsLine.StartTimestampMs) / lineEnteringDurationMs);
                    lyricsLine.PlayingState = LyricsPlayingState.Playing;
                } else {
                    if (i < currentPlayingLineIndex) {
                        lyricsLine.PlayingState = LyricsPlayingState.Played;
                    } else {
                        lyricsLine.PlayingState = LyricsPlayingState.NotPlayed;
                    }
                }

                lyricsLine.EnteringProgress = lineEnteringProgress;

                bool lineExiting =
                    lyricsLine.StartTimestampMs + _lineExitingDurationMs <= _currentTime.TotalMilliseconds &&
                    _currentTime.TotalMilliseconds <= lyricsLine.EndTimestampMs + _lineExitingDurationMs;

                float lineExitingProgress = 0.0f;

                if (lineExiting) {
                    lineExitingProgress =
                        (float)Math.Min(1, (_currentTime.TotalMilliseconds - lyricsLine.EndTimestampMs) / _lineExitingDurationMs);
                }

                lyricsLine.ExitingProgress = lineExitingProgress;

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {

                    var lineChar = lyricsLine.LyricsLineChars[j];

                    // Scale, opacity, and play progress
                    float scale;
                    float opacity;
                    float playProgress = 0;

                    // Chars going to be played in the future
                    if (_currentTime.TotalMilliseconds < lineChar.StartTimestampMs) {
                        lineChar.PlayingState = LyricsPlayingState.NotPlayed;
                        if (linePlaying) {
                            scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                            opacity = _defaultOpacity;
                        } else {
                            scale = _defaultScale;
                            opacity = _defaultOpacity;
                        }
                        // Chars playing right now 
                    } else if (lineChar.StartTimestampMs <= _currentTime.TotalMilliseconds &&
                        _currentTime.TotalMilliseconds <= lineChar.EndTimestampMs) {

                        lineChar.PlayingState = LyricsPlayingState.Playing;
                        playProgress =
                            ((float)_currentTime.TotalMilliseconds - lineChar.StartTimestampMs) / lineChar.DurationMs;
                        scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity = _defaultOpacity + (_highlightedOpacity - _defaultOpacity) * (float)playProgress;

                        // Chars already have played
                    } else {
                        lineChar.PlayingState = LyricsPlayingState.Played;
                        if (linePlaying) {
                            scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                            opacity = _highlightedOpacity;
                        } else {
                            if (lineExiting) {
                                scale = _highlightedScale - (_highlightedScale - _defaultScale) * (float)lineExitingProgress;
                                opacity = _highlightedOpacity - (_highlightedOpacity - _defaultOpacity) * (float)lineExitingProgress;
                            } else {
                                scale = _defaultScale;
                                opacity = _defaultOpacity;
                            }
                        }
                    }

                    lineChar.Scale = scale;
                    lineChar.Opacity = opacity;
                    lineChar.PlayingProgress = playProgress;
                }
            }
        }

        private void UpdatePosition(
            int displayStartLineIndex,
            int displayEndLineIndex,
            int currentPlayingLineIndex,
            bool alwaysScrollToCurrentPlayingLine = false) {

            if (currentPlayingLineIndex < 0) {
                return;
            }

            // Set _scrollOffsetY
            var currentPlayingLyricsLine = _lyricsLines[currentPlayingLineIndex];

            var lineScrollingProgress =
                (_currentTime.TotalMilliseconds - currentPlayingLyricsLine.StartTimestampMs) /
                Math.Min(_lineScrollDurationMs, currentPlayingLyricsLine.DurationMs);

            _scrollOffsetY =
                ((float)_lyricsAreaHeight / 2 -
                (currentPlayingLyricsLine.LyricsLineChars[0].PositionBeforeScrolling.Y + currentPlayingLyricsLine.LyricsLineChars[^1].PositionBeforeScrolling.Y) / 2) * EasingHelper.SmootherStep(Math.Min(1, (float)lineScrollingProgress));

            bool isScrollingNow = lineScrollingProgress <= 1;

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update Positions
            for (int i = displayStartLineIndex; i >= 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {

                    var lineChar = lyricsLine.LyricsLineChars[j];

                    if (alwaysScrollToCurrentPlayingLine || isScrollingNow) {
                        lineChar.Position = new Vector2(
                            lineChar.PositionBeforeScrolling.X,
                            lineChar.PositionBeforeScrolling.Y + _scrollOffsetY);
                    } else {
                        lineChar.PositionBeforeScrolling = lineChar.Position;
                    }

                    if (lineChar.Position.Y >= 0) {
                        if (_startVisibleLineIndex == -1) {
                            _startVisibleLineIndex = i;
                        }
                        if (lineChar.Position.Y <= _lyricsAreaHeight) {
                            if (_endVisibleLineIndex == -1) {
                                _endVisibleLineIndex = i;
                            } else if (_endVisibleLineIndex < i) {
                                _endVisibleLineIndex = i;
                            }
                        }
                    }

                }

            }

        }

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e) {
            SetLyricsColor();
            InitMediaManager();
        }

    }
}
