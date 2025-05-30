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

        private LyricsAlignmentType _lyricsAlignmentType;

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
                    BlurAmount = 100f,
                    Source = new ScaleEffect {
                        InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
                        BorderMode = EffectBorderMode.Soft
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
            _lyricsAlignmentType = (LyricsAlignmentType)SettingsViewModel.LyricsAlignmentType;

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
                case nameof(SettingsViewModel.IsCoverOverlayEnabled):
                    _isCoverOverlayEnabled = SettingsViewModel.IsCoverOverlayEnabled;
                    break;
                case nameof(SettingsViewModel.IsDynamicCoverOverlay):
                    _isDynamicCoverOverlay = SettingsViewModel.IsDynamicCoverOverlay;
                    break;
                case nameof(SettingsViewModel.CoverOverlayOpacity):
                    _coverOverlayOpacity = (float)(SettingsViewModel.CoverOverlayOpacity / 100.0);
                    break;
                case nameof(SettingsViewModel.LyricsAlignmentType):
                    _lyricsAlignmentType = (LyricsAlignmentType)SettingsViewModel.LyricsAlignmentType;
                    ReLayoutLyricsCanvas();
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

            float AddLyricsSubLine(LyricsLine lyricsLine, string subLineText, float y, int endLineCharIndex) {
                // Calculate layout bounds
                var layout = new CanvasTextLayout(LyricsCanvas.Device, subLineText, _textFormat, (float)_lyricsAreaWidth, (float)_lyricsAreaHeight);
                var bounds = layout.LayoutBounds;

                var centerX = 0.0;
                var centerY = bounds.Top + bounds.Height / 2;

                float x = (float)_lyricsCanvasLeftMargin;
                switch (_lyricsAlignmentType) {
                    case LyricsAlignmentType.Left:
                        centerX = x;
                        break;
                    case LyricsAlignmentType.Center:
                        x += (float)(_lyricsAreaWidth - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin - bounds.Width) / 2;
                        centerX = x + bounds.Width / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        x += (float)(_lyricsAreaWidth - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin - bounds.Width);
                        centerX = x + bounds.Width;
                        break;
                    default:
                        break;
                }

                lyricsLine.LyricsSubLines.Add(new LyricsLineChild {
                    Text = subLineText,
                    CenterPosition = new Vector2((float)centerX, (float)centerY),
                    PositionBeforeScrolling = new Vector2(x, y),
                    Position = new Vector2(x, y),
                    LayoutBounds = bounds,
                    StartTimestampMs = lyricsLine.LyricsLineChars[endLineCharIndex - subLineText.Length + 1].StartTimestampMs,
                    EndTimestampMs = lyricsLine.LyricsLineChars[endLineCharIndex].EndTimestampMs,
                });

                return (float)bounds.Height;
            }

            float y = (float)_lyricsAreaHeight / 2; // Top margin

            // Init Positions
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var lyricsLine = _lyricsLines[i];

                lyricsLine.LyricsSubLines.Clear();
                string subLineText = "";

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {

                    var lineChar = lyricsLine.LyricsLineChars[j];

                    string testSubLineText = subLineText + lineChar.Text;

                    // Calculate test layout bounds
                    var testLayout = new CanvasTextLayout(LyricsCanvas.Device, testSubLineText, _textFormat, (float)_lyricsAreaWidth, (float)_lyricsAreaHeight);
                    float testSubLineWidth = (float)testLayout.LayoutBounds.Width;

                    // Line break by exceeding UI edge (for the current char)
                    if (_lyricsCanvasLeftMargin + testSubLineWidth + _lyricsCanvasRightMargin > _lyricsAreaWidth) {

                        var boundsHeight = AddLyricsSubLine(lyricsLine, subLineText, y, j - 1);

                        subLineText = lineChar.Text;

                        y += boundsHeight * 1.1f;
                    } else {
                        subLineText = testSubLineText;
                    }

                    // Line break by design (for the next char)
                    if (j == lyricsLine.LyricsLineChars.Count - 1) {

                        if (subLineText != string.Empty) {

                            var boundsHeight = AddLyricsSubLine(lyricsLine, subLineText, y, j);

                            y += boundsHeight * 1.8f;

                        }

                    }
                }

            }

        }

        private void LyricsCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args) {
            using var ds = args.DrawingSession;

            var r = _lyricsColor.R;
            var g = _lyricsColor.G;
            var b = _lyricsColor.B;

            // Draw (dynamic) cover image as the 1st layer
            if (_isCoverOverlayEnabled && _canvasCoverBitmap != null) {
                DrawCoverImage(sender, ds, _isDynamicCoverOverlay);
            }

            // Draw lyrics as the 2nd layer
            using var lyricsLayer = new CanvasCommandList(sender);
            using var lyricsDs = lyricsLayer.CreateDrawingSession();
            DrawLyrics(sender, lyricsDs, r, g, b);

            // Draw gradient opacity mask as the 3rd layer
            using var maskCommandList = new CanvasCommandList(sender);
            using var maskDs = maskCommandList.CreateDrawingSession();
            DrawGradientOpacityMask(sender, maskDs, r, g, b);

            var blurredLyrics = new GaussianBlurEffect {
                Source = lyricsLayer,
                BlurAmount = 0f,
                BorderMode = EffectBorderMode.Soft,
            };

            var alphaMaskedLyrics = new AlphaMaskEffect {
                Source = blurredLyrics,
                AlphaMask = maskCommandList
            };

            ds.DrawImage(alphaMaskedLyrics);

        }

        private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds, byte r, byte g, byte b) {

            var (displayStartLineIndex, displayEndLineIndex) = GetVisibleLyricsLineIndexBoundaries();

            for (int i = displayStartLineIndex; _lyricsLines.Count > 0 && i >= 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsSubLines.Count; j++) {
                    var lyricsLineChild = lyricsLine.LyricsSubLines[j];

                    float progressPerChar = 1f / lyricsLineChild.Text.Length;

                    using var horizontalFillBrush = new CanvasLinearGradientBrush(control, [
                        new() { Position = 0, Color = Color.FromArgb((byte)(255 * lyricsLineChild.Opacity), r, g, b) },
                        new() { Position = lyricsLineChild.PlayingProgress * (1 + progressPerChar) - progressPerChar, Color = Color.FromArgb((byte)(255 * lyricsLineChild.Opacity), r, g, b) },
                        new() { Position = lyricsLineChild.PlayingProgress * (1 + progressPerChar), Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b) },
                        new() { Position = 1.5f, Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b) },
                    ]) {
                        StartPoint = new Vector2(lyricsLineChild.Position.X, 0),
                        EndPoint = new Vector2(lyricsLineChild.Position.X + (float)lyricsLineChild.LayoutBounds.Width, 0)
                    };

                    var position = lyricsLineChild.Position;

                    // Scale
                    ds.Transform = Matrix3x2.CreateScale(lyricsLineChild.Scale, lyricsLineChild.CenterPosition);

                    //ds.DrawText(lyricsLineChild.Text, position, horizontalFillBrush, _textFormat);
                    using var layout = new CanvasTextLayout(control, lyricsLineChild.Text, _textFormat, (float)(control.Size.Width - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin), (float)_lyricsAreaHeight);
                    var adjustedPosition = new Vector2(position.X, position.Y - (float)_lyricsAreaHeight / 2);
                    ds.DrawTextLayout(layout, adjustedPosition, horizontalFillBrush);

                    // Reset scale
                    ds.Transform = Matrix3x2.Identity;
                }
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
                ds.Transform = Matrix3x2.Identity;
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

                var line = _lyricsLines[i];
                if (line.EndTimestampMs < _currentTime.TotalMilliseconds) {
                    continue;
                }
                return i;
            }
            return -1;

        }

        private int GetCurrentPlayingSubLineIndex(int currentPlayingLineIndex) {

            if (currentPlayingLineIndex < 0) {
                return -1;
            }

            for (int i = 0; i < _lyricsLines[currentPlayingLineIndex].LyricsSubLines.Count; i++) {

                var subLine = _lyricsLines[currentPlayingLineIndex].LyricsSubLines[i];
                if (subLine.EndTimestampMs < _currentTime.TotalMilliseconds) {
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

                var lineEnteringDurationMs = Math.Min(lyricsLine.DurationMs, _lineEnteringDurationMs);
                var lineExitingDurationMs = _lineExitingDurationMs;
                if (i + 1 <= displayEndLineIndex) {
                    lineExitingDurationMs = Math.Min(_lyricsLines[i + 1].DurationMs, lineExitingDurationMs);
                }

                float lineEnteringProgress = 0.0f;
                float lineExitingProgress = 0.0f;

                bool lineEntering = false;
                bool lineExiting = false;

                float scale = _defaultScale;
                float opacity = _defaultOpacity;

                if (linePlaying) {
                    lyricsLine.PlayingState = LyricsPlayingState.Playing;

                    scale = _highlightedScale;
                    opacity = _highlightedOpacity;

                    var durationFromStartMs = _currentTime.TotalMilliseconds - lyricsLine.StartTimestampMs;
                    lineEntering = durationFromStartMs <= lineEnteringDurationMs;
                    if (lineEntering) {
                        lineEnteringProgress = (float)durationFromStartMs / lineEnteringDurationMs;
                        scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity = _defaultOpacity + (_highlightedOpacity - _defaultOpacity) * (float)lineEnteringProgress;
                    }

                } else {
                    if (i < currentPlayingLineIndex) {
                        lyricsLine.PlayingState = LyricsPlayingState.Played;

                        var durationToEndMs = _currentTime.TotalMilliseconds - lyricsLine.EndTimestampMs;
                        lineExiting = durationToEndMs <= lineExitingDurationMs;
                        if (lineExiting) {

                            lineExitingProgress = (float)durationToEndMs / lineExitingDurationMs;
                            scale = _highlightedScale - (_highlightedScale - _defaultScale) * (float)lineExitingProgress;
                            opacity = _highlightedOpacity - (_highlightedOpacity - _defaultOpacity) * (float)lineExitingProgress;
                        }

                    } else {
                        lyricsLine.PlayingState = LyricsPlayingState.NotPlayed;
                    }
                }

                lyricsLine.EnteringProgress = lineEnteringProgress;
                lyricsLine.ExitingProgress = lineExitingProgress;

                for (int j = 0; j < lyricsLine.LyricsSubLines.Count; j++) {

                    var lyricsLineChild = lyricsLine.LyricsSubLines[j];

                    // Play progress
                    float playProgress = 0;

                    // Child going to be played in the future
                    if (_currentTime.TotalMilliseconds < lyricsLineChild.StartTimestampMs) {
                        lyricsLineChild.PlayingState = LyricsPlayingState.NotPlayed;

                        // Child playing right now 
                    } else if (lyricsLineChild.StartTimestampMs <= _currentTime.TotalMilliseconds &&
                        _currentTime.TotalMilliseconds <= lyricsLineChild.EndTimestampMs) {
                        lyricsLineChild.PlayingState = LyricsPlayingState.Playing;
                        playProgress =
                            ((float)_currentTime.TotalMilliseconds - lyricsLineChild.StartTimestampMs) / lyricsLineChild.DurationMs;

                        // Child already have played
                    } else {
                        lyricsLineChild.PlayingState = LyricsPlayingState.Played;
                        playProgress = 1;
                    }

                    lyricsLineChild.Scale = scale;
                    lyricsLineChild.Opacity = opacity;
                    lyricsLineChild.PlayingProgress = playProgress;
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

            var currentPlayingSubLineIndex = GetCurrentPlayingSubLineIndex(currentPlayingLineIndex);

            if (currentPlayingSubLineIndex < 0) {
                return;
            }

            // Set _scrollOffsetY
            var currentPlayingLyricsSubLine = _lyricsLines[currentPlayingLineIndex].LyricsSubLines[currentPlayingSubLineIndex];

            var lineScrollingProgress =
                (_currentTime.TotalMilliseconds - currentPlayingLyricsSubLine.StartTimestampMs) /
                Math.Min(_lineScrollDurationMs, currentPlayingLyricsSubLine.DurationMs);

            _scrollOffsetY =
                (float)(_lyricsAreaHeight / 2 - currentPlayingLyricsSubLine.PositionBeforeScrolling.Y) *
                EasingHelper.SmootherStep(Math.Min(1, (float)lineScrollingProgress));

            bool isScrollingNow = lineScrollingProgress <= 1;

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update Positions
            for (int i = displayStartLineIndex; i >= 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsSubLines.Count; j++) {

                    var lyricsLineChild = lyricsLine.LyricsSubLines[j];

                    if (alwaysScrollToCurrentPlayingLine || isScrollingNow) {
                        lyricsLineChild.Position = new Vector2(
                            lyricsLineChild.PositionBeforeScrolling.X,
                            lyricsLineChild.PositionBeforeScrolling.Y + _scrollOffsetY);
                    } else {
                        lyricsLineChild.PositionBeforeScrolling = lyricsLineChild.Position;
                    }

                    if (lyricsLineChild.Position.Y + lyricsLineChild.LayoutBounds.Height >= 0) {
                        if (_startVisibleLineIndex == -1) {
                            _startVisibleLineIndex = i;
                        }
                    }
                    if (lyricsLineChild.Position.Y + lyricsLineChild.LayoutBounds.Height >= _lyricsAreaHeight) {
                        if (_endVisibleLineIndex == -1) {
                            _endVisibleLineIndex = i;
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
