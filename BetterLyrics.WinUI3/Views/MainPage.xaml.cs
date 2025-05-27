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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views {
    /// <summary>
    /// Please note that this page was implemented by traditional XAML + C# code-behind, not MVVM.
    /// I tried before but there was a difficulty in accessing UI thread on non-UI threads via MVVM.
    /// </summary>
    public sealed partial class MainPage : Page {

        public MainViewModel ViewModel => (MainViewModel)DataContext;

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

        private int _lineEnteringDurationMs = 300;
        private int _lineExitingDurationMs = 300;
        private int _lineScrollDurationMs = 300;

        private float _scrollOffsetY = 0.0f;

        private double _lyricsAreaWidth = 0.0f;
        private double _lyricsAreaHeight = 0.0f;

        private double _lyricsCanvasLeftMargin = 0;

        private int _startLineIndex = -1;
        private int _endLineIndex = -1;

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private ColorThief _colorThief = new();

        private Color _startGradientColor = Colors.Transparent;
        private Color _endGradientColor = Colors.Transparent;

        private List<LyricsLine> _lyricsLines = [];

        private List<string> _localMusicFolderPaths = ["D:/Musics"];

        private Color _systemBaseHighColor = (Color)App.Current.Resources["SystemBaseHighColor"];

        public MainPage() {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<MainViewModel>();

            _queueTimer = _dispatcherQueue.CreateTimer();

            InitMediaManager();
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
                    _currentTime = _currentSession.GetTimelineProperties().Position;
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

            await Task.Delay(_animationDurationMs);

            CoverImage.Source = null;
            LyricsCanvas.Paused = true;
            _startLineIndex = _endLineIndex = -1;
            _lyricsLines.Clear();
            _currentTime = TimeSpan.Zero;

            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;

            if (_currentSession != null) {
                mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
            }

            ViewModel.Title = mediaProps?.Title;
            ViewModel.Artist = mediaProps?.Artist;

            byte[]? imgByteFromTag = null;

            bool finishSearching = false;

            foreach (var musicFolderPath in _localMusicFolderPaths) {
                foreach (var file in Directory.GetFiles(musicFolderPath)) {
                    var fileExtension = Path.GetExtension(file);
                    if (fileExtension != ".mp3" && fileExtension != ".flac") {
                        continue;
                    }
                    var track = new Track(file);

                    if (track.Title == mediaProps?.Title && track.Artist == mediaProps?.Artist) {
                        // Get picture from tag
                        if (track.EmbeddedPictures.Count > 0) {
                            imgByteFromTag = track.EmbeddedPictures[0].PictureData;
                        }

                        // Get lyrics
                        var lyricsPhrases = track.Lyrics.SynchronizedLyrics;
                        for (int i = 0; i < lyricsPhrases.Count; i++) {
                            var lyricsPhrase = lyricsPhrases[i];
                            int lyricsPhraseStartTimestampMs = lyricsPhrase.TimestampMs;
                            int lyricsPhraseEndTimestampMs = 0;

                            if (i + 1 < lyricsPhrases.Count) {
                                lyricsPhraseEndTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                            } else {
                                lyricsPhraseEndTimestampMs = (int)track.DurationMs;
                            }

                            var lyricsLine = new LyricsLine {
                                StartTimestampMs = lyricsPhraseStartTimestampMs,
                                EndTimestampMs = lyricsPhraseEndTimestampMs,
                                IsPlaying = false,
                                Text = lyricsPhrase.Text,
                            };
                            lyricsLine.DurationMs = lyricsLine.EndTimestampMs - lyricsLine.StartTimestampMs;
                            lyricsLine.AverageDurationPerCharMs = lyricsLine.DurationMs / lyricsLine.Text.Length;

                            List<LyricsLineChar> lyricsLineChars = [];
                            var lyricsLineCharStartTimestampMs = lyricsPhraseStartTimestampMs;

                            foreach (var ch in lyricsLine.Text) {

                                var lyricsLineChar = new LyricsLineChar {
                                    Text = (ch == ' ' ? (char)160 : ch).ToString(),
                                    StartTimestampMs = lyricsLineCharStartTimestampMs,
                                };
                                lyricsLineChar.EndTimestampMs = lyricsLineChar.StartTimestampMs + lyricsLine.AverageDurationPerCharMs;
                                lyricsLineChar.DurationMs = lyricsLineChar.EndTimestampMs - lyricsLineChar.StartTimestampMs;
                                lyricsLineChars.Add(lyricsLineChar);

                                lyricsLineCharStartTimestampMs += lyricsLine.AverageDurationPerCharMs;
                            }

                            lyricsLine.LyricsLineChars = [.. lyricsLineChars];
                            _lyricsLines.Add(lyricsLine);
                        }

                        await SyncLyricsCanvasSizeAsync();

                        ReLayoutLyricsCanvas();

                        int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
                        var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();
                        UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex, true);

                        finishSearching = true;
                        break;

                    }
                }

                if (finishSearching) break;
            }

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
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                CoverImage.Source = bitmapImage;

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var quantizedColors = await _colorThief.GetPalette(decoder, 3);
                for (int i = 0; i < 3; i++) {
                    QuantizedColor quantizedColor = quantizedColors[i];
                    ViewModel.CoverImageDominantColors[i] = Color.FromArgb(
                        quantizedColor.Color.A, quantizedColor.Color.R, quantizedColor.Color.G, quantizedColor.Color.B);
                }
            }

            _startGradientColor = ViewModel.StartGraidentColor = ViewModel.CoverImageDominantColors[0];
            _endGradientColor = ViewModel.EndGraidentColor = ViewModel.CoverImageDominantColors[1];

            //CreateBlurEffect(RootGrid);
            LyricsCanvas.Paused = false;
            ViewModel.AboutToUpdateUI = false;
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
            _queueTimer.Debounce(() => {

                Debug.WriteLine("CurrentSession_MediaPropertiesChanged");
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () => {
                    await UpdateUIMediaInfoAsync();
                });

            }, TimeSpan.FromMilliseconds(200));
        }

        private void CreateBlurEffect(FrameworkElement element, IRandomAccessStream stream, double amount = 10.0) {
            var effect = new GaussianBlurEffect {
                Name = "Blur",
                BlurAmount = (float)amount,
                Source = new CompositionEffectSourceParameter("source")
            };

            var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
            var factory = compositor.CreateEffectFactory(effect as IGraphicsEffect, ["Blur.BlurAmount"]); // Cast to IGraphicsEffect
            var brush = factory.CreateBrush();

            var surface = LoadedImageSurface.StartLoadFromStream(stream);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface);

            brush.SetSourceParameter("source", surfaceBrush);
            //brush.SetSourceParameter("source", compositor.CreateBackdropBrush());

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = brush;
            visual.Size = new Vector2((float)element.ActualWidth, (float)element.ActualHeight);

            ElementCompositionPreview.SetElementChildVisual(element, visual);
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

        private async Task SyncLyricsCanvasSizeAsync() {
            bool oldIsSmallScreenMode = ViewModel.IsSmallScreenMode;

            if (RootGrid.ActualWidth <= 708 && _lyricsLines.Count > 0) {
                ViewModel.IsSmallScreenMode = true;
            } else {
                ViewModel.IsSmallScreenMode = false;
            }

            await Task.Delay((int)(_animationDurationMs * 1.5));

            if (_lyricsLines.Count == 0) {
                SongInfoStackPanel.Width = RootGrid.ActualWidth - 36 * 2;
            } else {
                SongInfoStackPanel.Width = (RootGrid.ActualWidth - 36 * 3) / 2;
            }

            if (ViewModel.IsSmallScreenMode) {
                _lyricsCanvasLeftMargin = 36;
            } else {
                _lyricsCanvasLeftMargin = 36 + SongInfoStackPanel.Width + 36;
            }

            _lyricsAreaHeight = LyricsGrid.ActualHeight;
            _lyricsAreaWidth = LyricsGrid.ActualWidth;

        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            _queueTimer.Debounce(async () => {

                await SyncLyricsCanvasSizeAsync();

                ReLayoutLyricsCanvas();

                int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
                var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();
                UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex, true);

            }, TimeSpan.FromMilliseconds(50));
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
            //var renderTarget = new CanvasRenderTarget(sender, (float)_lyricsAreaWidth, (float)_lyricsAreaHeight, sender.Dpi);
            //var shadowEffect = new ShadowEffect {
            //    Source = renderTarget,
            //    BlurAmount = 10.0f,
            //    ShadowColor = Colors.Red
            //};

            var ds = args.DrawingSession;
            //ds.DrawImage(shadowEffect);

            var r = _systemBaseHighColor.R;
            var g = _systemBaseHighColor.G;
            var b = _systemBaseHighColor.B;

            var coverGradientBrush = new CanvasLinearGradientBrush(sender, [
                new() { Position = 0, Color = _startGradientColor},
                new() { Position = 1, Color =  _endGradientColor},
            ]) {
                StartPoint = new Vector2(0.5f, 0),
                EndPoint = new Vector2(0.5f, (float)sender.Size.Height)
            };

            var center = sender.Size.ToVector2() * 0.5f;
            var radiusX = (float)sender.Size.Width / 2;
            var radiusY = (float)sender.Size.Height / 2;

            ds.FillRectangle(0, 0, (float)sender.Size.Width, (float)sender.Size.Height, coverGradientBrush);

            var (displayStartLineIndex, displayEndLineIndex) = GetVisibleLyricsLineIndexBoundaries();

            for (int i = displayStartLineIndex; _lyricsLines.Count > 0 && i >= 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {
                    var lineChar = lyricsLine.LyricsLineChars[j];

                    var fadeOutGradientBrush = new CanvasLinearGradientBrush(sender, [
                        new() { Position = 0, Color =  Color.FromArgb(0, r, g, b)},
                        new() { Position = 0.5f, Color = Color.FromArgb((byte)(255 * lineChar.Opacity), r, g, b)},
                        new() { Position = 1, Color =  Color.FromArgb(0, r, g, b)},
                    ]) {
                        StartPoint = new Vector2(0.5f, 0),
                        EndPoint = new Vector2(0.5f, (float)sender.Size.Height)
                    };

                    var position = lineChar.Position;

                    // Scale
                    ds.Transform = Matrix3x2.CreateScale(lineChar.Scale, lineChar.CenterPosition);

                    ds.DrawText(lineChar.Text, position, fadeOutGradientBrush, _textFormat);

                    // Reset scale
                    ds.Transform = Matrix3x2.Identity;
                }
            }

        }

        private int GetCurrentPlayingLineIndex() {
            for (int i = 0; i < _lyricsLines.Count; i++) {

                var lyricsLine = _lyricsLines[i];

                bool linePlaying =
                    lyricsLine.StartTimestampMs <= _currentTime.TotalMilliseconds &&
                    _currentTime.TotalMilliseconds <= lyricsLine.EndTimestampMs;

                if (linePlaying) {
                    return i;
                }
            }

            return -1;

        }

        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries() {
            return new Tuple<int, int>(_startLineIndex, _endLineIndex);
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries() {
            return new Tuple<int, int>(0, _lyricsLines.Count - 1);
        }

        private void UpdateScaleAndOpacity(int displayStartLineIndex, int displayEndLineIndex, int currentPlayingLineIndex) {
            for (int i = displayStartLineIndex; _lyricsLines.Count > 0 && i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                bool linePlaying = i == currentPlayingLineIndex;

                float lineEnteringProgress = 0.0f;

                if (linePlaying) {
                    var lineEnteringDurationMs = Math.Min(lyricsLine.DurationMs, _lineEnteringDurationMs);
                    lineEnteringProgress =
                        (float)Math.Min(1, (_currentTime.TotalMilliseconds - lyricsLine.StartTimestampMs) / lineEnteringDurationMs);
                }

                bool lineExiting =
                    lyricsLine.StartTimestampMs + _lineExitingDurationMs <= _currentTime.TotalMilliseconds &&
                    _currentTime.TotalMilliseconds <= lyricsLine.EndTimestampMs + _lineExitingDurationMs;

                float lineExitingProgress = 0.0f;

                if (lineExiting) {
                    lineExitingProgress =
                        (float)Math.Min(1, (_currentTime.TotalMilliseconds - lyricsLine.EndTimestampMs) / _lineExitingDurationMs);

                }

                for (int j = 0; j < lyricsLine.LyricsLineChars.Count; j++) {

                    var lineChar = lyricsLine.LyricsLineChars[j];

                    float scale;
                    float opacity;
                    // Chars going to be played in the future
                    if (_currentTime.TotalMilliseconds < lineChar.StartTimestampMs) {

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

                        float playProgress =
                            ((float)_currentTime.TotalMilliseconds - lineChar.StartTimestampMs) / lineChar.DurationMs;
                        scale = _defaultScale + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity = _defaultOpacity + (_highlightedOpacity - _defaultOpacity) * (float)playProgress;

                        // Chars already have played
                    } else {

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
                }
            }
        }

        private void UpdatePosition(
            int displayStartLineIndex,
            int displayEndLineIndex,
            int currentPlayingLineIndex,
            bool alwaysScrollToCurrentPlayingLine = false) {

            bool isScrollingNow = false;

            // Set _scrollOffsetY
            for (int i = displayStartLineIndex; i <= displayEndLineIndex; i++) {

                var lyricsLine = _lyricsLines[i];

                if (i == currentPlayingLineIndex) {
                    var lineScrollingProgress =
                        (float)Math.Min(1,
                        (_currentTime.TotalMilliseconds - lyricsLine.StartTimestampMs) /
                        Math.Min(_lineScrollDurationMs, lyricsLine.DurationMs));

                    _scrollOffsetY =
                        ((float)_lyricsAreaHeight / 2 - lyricsLine.LyricsLineChars[0].PositionBeforeScrolling.Y) * lineScrollingProgress;

                    isScrollingNow = lineScrollingProgress < 1;

                    break;
                }
            }

            _startLineIndex = _endLineIndex = -1;

            // Update Positions
            for (int i = displayStartLineIndex; i <= displayEndLineIndex; i++) {

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
                        if (_startLineIndex == -1) {
                            _startLineIndex = i;
                        }
                        if (lineChar.Position.Y <= _lyricsAreaHeight) {
                            if (_endLineIndex == -1) {
                                _endLineIndex = i;
                            } else if (_endLineIndex < i) {
                                _endLineIndex = i;
                            }
                        }
                    }

                }

            }

        }

        private void LyricsCanvas_Update(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args) {
            _currentTime += args.Timing.ElapsedTime;

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            var (displayStartLineIndex, displayEndLineIndex) = GetMaxLyricsLineIndexBoundaries();

            UpdateScaleAndOpacity(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex);

            UpdatePosition(displayStartLineIndex, displayEndLineIndex, currentPlayingLineIndex);

            // Consumes too much resources, bad performance
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => {

                if (ViewModel.AboutToUpdateUI) {
                    return;
                }

                //var t = _currentTime.TotalMilliseconds % 30000;
                //double progress = t / 30000;

                //int segment = (int)(progress * 3);
                //double localT = (progress * 3) - segment;

                //Color startFrom = ViewModel.CoverImageDominantColors[segment % 3];
                //Color startTo = ViewModel.CoverImageDominantColors[(segment + 1) % 3];
                //Color currentStart = Helper.ColorHelper.LerpColor(startFrom, startTo, localT);
                //ViewModel.StartGraidentColor = _startGradientColor = currentStart;

                //Color endFrom = ViewModel.CoverImageDominantColors[(segment + 2) % 3];
                //Color endTo = ViewModel.CoverImageDominantColors[segment % 3];
                //Color currentEnd = Helper.ColorHelper.LerpColor(endFrom, endTo, localT);
                //ViewModel.EndGraidentColor = _endGradientColor = currentEnd;

            });
        }

    }
}
