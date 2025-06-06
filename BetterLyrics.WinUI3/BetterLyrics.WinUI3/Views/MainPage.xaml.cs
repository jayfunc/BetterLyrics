using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Control;
using Color = Windows.UI.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;
        public SettingsService SettingsService { get; set; }

        private List<LyricsLine> _lyricsLines = [];

        private SoftwareBitmap? _coverSoftwareBitmap = null;
        private uint _coverImagePixelWidth = 0;
        private uint _coverImagePixelHeight = 0;

        private float _coverBitmapRotateAngle = 0f;
        private float _coverScaleFactor = 1;

        private readonly float _coverRotateSpeed = 0.003f;

        private float _lyricsGlowEffectAngle = 0f;
        private readonly float _lyricsGlowEffectSpeed = 0.01f;

        private readonly float _lyricsGlowEffectMinBlurAmount = 0f;
        private readonly float _lyricsGlowEffectMaxBlurAmount = 6f;

        private readonly DispatcherQueueTimer _queueTimer;

        private TimeSpan _currentTime = TimeSpan.Zero;

        private readonly float _defaultOpacity = 0.3f;
        private readonly float _highlightedOpacity = 1.0f;

        private readonly float _defaultScale = 0.95f;
        private readonly float _highlightedScale = 1.0f;

        private readonly int _lineEnteringDurationMs = 800;
        private readonly int _lineExitingDurationMs = 800;
        private readonly int _lineScrollDurationMs = 800;

        private float _lastTotalYScroll = 0.0f;
        private float _totalYScroll = 0.0f;

        private double _lyricsAreaWidth = 0.0f;
        private double _lyricsAreaHeight = 0.0f;

        private readonly double _lyricsCanvasRightMargin = 36;
        private double _lyricsCanvasLeftMargin = 0;
        private double _lyricsCanvasMaxTextWidth = 0;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _forceToScroll = false;

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private Color _lyricsColor;

        private readonly ILogger<MainPage> _logger;

        public MainPage()
        {
            this.InitializeComponent();

            _queueTimer = _dispatcherQueue.CreateTimer();

            _logger = Ioc.Default.GetService<ILogger<MainPage>>()!;
            SettingsService = Ioc.Default.GetService<SettingsService>()!;
            DataContext = Ioc.Default.GetService<MainViewModel>();

            SettingsService.PropertyChanged += SettingsService_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            SetLyricsColor();

            if (SettingsService.IsFirstRun)
            {
                WelcomeTeachingTip.IsOpen = true;
            }
        }

        private async Task ForceToScrollToCurrentPlayingLineAsync()
        {
            _forceToScroll = true;
            await Task.Delay(1);
            _forceToScroll = false;
        }

        private async void SettingsService_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsService.LyricsFontSize):
                case nameof(SettingsService.LyricsLineSpacingFactor):
                    LayoutLyrics();
                    await ForceToScrollToCurrentPlayingLineAsync();
                    break;
                case nameof(SettingsService.IsRebuildingLyricsIndexDatabase):
                    if (!SettingsService.IsRebuildingLyricsIndexDatabase)
                    {
                        CurrentSession_MediaPropertiesChanged(_currentSession, null);
                    }
                    break;
                case nameof(SettingsService.Theme):
                case nameof(SettingsService.LyricsFontColorType):
                case nameof(SettingsService.LyricsFontSelectedAccentColorIndex):
                    await Task.Delay(1);
                    SetLyricsColor();
                    break;
                case nameof(SettingsService.CoverImageRadius):
                    CoverImageGrid.CornerRadius = new CornerRadius(
                        SettingsService.CoverImageRadius / 100f * (CoverImageGrid.ActualHeight / 2)
                    );
                    break;
                default:
                    break;
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ShowLyricsOnly):
                    RootGrid_SizeChanged(null, null);
                    break;
                default:
                    break;
            }
        }

        private void SetLyricsColor()
        {
            switch ((LyricsFontColorType)SettingsService.LyricsFontColorType)
            {
                case LyricsFontColorType.Default:
                    _lyricsColor = ((SolidColorBrush)LyricsCanvas.Foreground).Color;
                    break;
                case LyricsFontColorType.Dominant:
                    _lyricsColor = ViewModel.CoverImageDominantColors[
                        Math.Max(
                            0,
                            Math.Min(
                                ViewModel.CoverImageDominantColors.Count - 1,
                                SettingsService.LyricsFontSelectedAccentColorIndex
                            )
                        )
                    ];
                    break;
                default:
                    break;
            }
        }

        private async void InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
            _sessionManager.SessionsChanged += SessionManager_SessionsChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
        }

        private void CurrentSession_TimelinePropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            TimelinePropertiesChangedEventArgs? args
        )
        {
            if (sender == null)
            {
                _currentTime = TimeSpan.Zero;
                return;
            }

            _currentTime = sender.GetTimelineProperties().Position;
            // _logger.LogDebug(_currentTime);
        }

        /// <summary>
        /// Note: Non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_PlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            PlaybackInfoChangedEventArgs? args
        )
        {
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Normal,
                () =>
                {
                    if (sender == null)
                    {
                        LyricsCanvas.Paused = true;
                        return;
                    }

                    var playbackState = sender.GetPlaybackInfo().PlaybackStatus;
                    _logger.LogDebug(playbackState.ToString());

                    switch (playbackState)
                    {
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing:
                            LyricsCanvas.Paused = true;
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
            );
        }

        private void SessionManager_SessionsChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            SessionsChangedEventArgs? args
        )
        {
            _logger.LogDebug("SessionManager_SessionsChanged");
        }

        private void SessionManager_CurrentSessionChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            CurrentSessionChangedEventArgs? args
        )
        {
            _logger.LogDebug("SessionManager_CurrentSessionChanged");
            // Unregister events associated with the previous session
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -=
                    CurrentSession_TimelinePropertiesChanged;
            }

            // Record and register events for current session
            _currentSession = sender.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged +=
                    CurrentSession_TimelinePropertiesChanged;
            }

            CurrentSession_MediaPropertiesChanged(_currentSession, null);
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            MediaPropertiesChangedEventArgs? args
        )
        {
            _queueTimer.Debounce(
                () =>
                {
                    _logger.LogDebug("CurrentSession_MediaPropertiesChanged");
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps =
                                null;

                            if (_currentSession != null)
                            {
                                try
                                {
                                    mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
                                }
                                catch (Exception) { }
                            }

                            ViewModel.IsAnyMusicSessionExisted = _currentSession != null;

                            ViewModel.AboutToUpdateUI = true;
                            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

                            (
                                _lyricsLines,
                                _coverSoftwareBitmap,
                                _coverImagePixelWidth,
                                _coverImagePixelHeight
                            ) = await ViewModel.SetSongInfoAsync(mediaProps);

                            // Force to show lyrics and scroll to current line even if the music is not playing
                            LyricsCanvas.Paused = false;
                            await ForceToScrollToCurrentPlayingLineAsync();
                            await Task.Delay(1);
                            // Detect and recover the music state
                            CurrentSession_PlaybackInfoChanged(_currentSession, null);
                            CurrentSession_TimelinePropertiesChanged(_currentSession, null);

                            ViewModel.AboutToUpdateUI = false;

                            if (_lyricsLines.Count == 0)
                            {
                                Grid.SetColumnSpan(SongInfoInnerGrid, 3);
                            }
                            else
                            {
                                Grid.SetColumnSpan(SongInfoInnerGrid, 1);
                            }
                        }
                    );
                },
                TimeSpan.FromMilliseconds(AnimationHelper.DebounceDefaultDuration)
            );
        }

        private async void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs? e)
        {
            //_queueTimer.Debounce(async () => {

            _lyricsAreaHeight = LyricsGrid.ActualHeight;
            _lyricsAreaWidth = LyricsGrid.ActualWidth;

            if (SongInfoColumnDefinition.ActualWidth == 0 || ViewModel.ShowLyricsOnly)
            {
                _lyricsCanvasLeftMargin = 36;
            }
            else
            {
                _lyricsCanvasLeftMargin = 36 + SongInfoColumnDefinition.ActualWidth + 36;
            }

            _lyricsCanvasMaxTextWidth =
                _lyricsAreaWidth - _lyricsCanvasLeftMargin - _lyricsCanvasRightMargin;

            LayoutLyrics();
            await ForceToScrollToCurrentPlayingLineAsync();

            //}, TimeSpan.FromMilliseconds(50));
        }

        // Comsumes GPU related resources
        private void LyricsCanvas_Draw(
            ICanvasAnimatedControl sender,
            CanvasAnimatedDrawEventArgs args
        )
        {
            using var ds = args.DrawingSession;

            var r = _lyricsColor.R;
            var g = _lyricsColor.G;
            var b = _lyricsColor.B;

            // Draw (dynamic) cover image as the very first layer
            if (SettingsService.IsCoverOverlayEnabled && _coverSoftwareBitmap != null)
            {
                DrawCoverImage(sender, ds);
            }

            // Lyrics only layer
            using var lyrics = new CanvasCommandList(sender);
            using (var lyricsDs = lyrics.CreateDrawingSession())
            {
                DrawLyrics(sender, lyricsDs, r, g, b);
            }

            using var glowedLyrics = new CanvasCommandList(sender);
            using (var glowedLyricsDs = glowedLyrics.CreateDrawingSession())
            {
                if (SettingsService.IsLyricsGlowEffectEnabled)
                {
                    glowedLyricsDs.DrawImage(
                        new GaussianBlurEffect
                        {
                            Source = lyrics,
                            BlurAmount =
                                MathF.Sin(_lyricsGlowEffectAngle)
                                    * (
                                        _lyricsGlowEffectMaxBlurAmount
                                        - _lyricsGlowEffectMinBlurAmount
                                    )
                                    / 2f
                                + (_lyricsGlowEffectMaxBlurAmount + _lyricsGlowEffectMinBlurAmount)
                                    / 2f,
                            BorderMode = EffectBorderMode.Soft,
                            Optimization = EffectOptimization.Quality,
                        }
                    );
                }
                glowedLyricsDs.DrawImage(lyrics);
            }

            // Mock gradient blurred lyrics layer
            using var combinedBlurredLyrics = new CanvasCommandList(sender);
            using var combinedBlurredLyricsDs = combinedBlurredLyrics.CreateDrawingSession();
            if (SettingsService.LyricsBlurAmount == 0)
            {
                combinedBlurredLyricsDs.DrawImage(glowedLyrics);
            }
            else
            {
                double step = 0.05;
                double overlapFactor = 0;
                for (double i = 0; i <= 0.5 - step; i += step)
                {
                    using var blurredLyrics = new GaussianBlurEffect
                    {
                        Source = glowedLyrics,
                        BlurAmount = (float)(
                            SettingsService.LyricsBlurAmount * (1 - i / (0.5 - step))
                        ),
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    };
                    using var topCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            sender.Size.Height * i,
                            sender.Size.Width,
                            sender.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    using var bottomCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            sender.Size.Height * (1 - i - step * (1 + overlapFactor)),
                            sender.Size.Width,
                            sender.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    combinedBlurredLyricsDs.DrawImage(topCropped);
                    combinedBlurredLyricsDs.DrawImage(bottomCropped);
                }
            }

            // Masked mock gradient blurred lyrics layer
            using var maskedCombinedBlurredLyrics = new CanvasCommandList(sender);
            using (
                var maskedCombinedBlurredLyricsDs =
                    maskedCombinedBlurredLyrics.CreateDrawingSession()
            )
            {
                if (SettingsService.LyricsVerticalEdgeOpacity == 100)
                {
                    maskedCombinedBlurredLyricsDs.DrawImage(combinedBlurredLyrics);
                }
                else
                {
                    using var mask = new CanvasCommandList(sender);
                    using (var maskDs = mask.CreateDrawingSession())
                    {
                        DrawGradientOpacityMask(sender, maskDs, r, g, b);
                    }
                    maskedCombinedBlurredLyricsDs.DrawImage(
                        new AlphaMaskEffect { Source = combinedBlurredLyrics, AlphaMask = mask }
                    );
                }
            }

            // Draw the final composed layer
            ds.DrawImage(maskedCombinedBlurredLyrics);
        }

        private void DrawLyrics(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            byte r,
            byte g,
            byte b
        )
        {
            var (displayStartLineIndex, displayEndLineIndex) =
                GetVisibleLyricsLineIndexBoundaries();

            for (
                int i = displayStartLineIndex;
                _lyricsLines.Count > 0
                    && i >= 0
                    && i < _lyricsLines.Count
                    && i <= displayEndLineIndex;
                i++
            )
            {
                var line = _lyricsLines[i];

                if (line.TextLayout == null)
                {
                    return;
                }

                float progressPerChar = 1f / line.Text.Length;

                var position = line.Position;

                float centerX = position.X;
                float centerY = position.Y + (float)line.TextLayout.LayoutBounds.Height / 2;

                switch ((LyricsAlignmentType)SettingsService.LyricsAlignmentType)
                {
                    case LyricsAlignmentType.Left:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += (float)_lyricsCanvasMaxTextWidth / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)_lyricsCanvasMaxTextWidth;
                        break;
                    default:
                        break;
                }

                int startIndex = 0;

                // Set brush
                for (int j = 0; j < line.TextLayout.LineCount; j++)
                {
                    int count = line.TextLayout.LineMetrics[j].CharacterCount;
                    var regions = line.TextLayout.GetCharacterRegions(startIndex, count);
                    float subLinePlayingProgress = Math.Clamp(
                        (line.PlayingProgress * line.Text.Length - startIndex) / count,
                        0,
                        1
                    );

                    using var horizontalFillBrush = new CanvasLinearGradientBrush(
                        control,
                        [
                            new()
                            {
                                Position = 0,
                                Color = Color.FromArgb((byte)(255 * line.Opacity), r, g, b),
                            },
                            new()
                            {
                                Position =
                                    subLinePlayingProgress * (1 + progressPerChar)
                                    - progressPerChar,
                                Color = Color.FromArgb((byte)(255 * line.Opacity), r, g, b),
                            },
                            new()
                            {
                                Position = subLinePlayingProgress * (1 + progressPerChar),
                                Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b),
                            },
                            new()
                            {
                                Position = 1.5f,
                                Color = Color.FromArgb((byte)(255 * _defaultOpacity), r, g, b),
                            },
                        ]
                    )
                    {
                        StartPoint = new Vector2(
                            (float)(regions[0].LayoutBounds.Left + position.X),
                            0
                        ),
                        EndPoint = new Vector2(
                            (float)(regions[^1].LayoutBounds.Right + position.X),
                            0
                        ),
                    };

                    line.TextLayout.SetBrush(startIndex, count, horizontalFillBrush);
                    startIndex += count;
                }

                // Scale
                ds.Transform =
                    Matrix3x2.CreateScale(line.Scale, new Vector2(centerX, centerY))
                    * Matrix3x2.CreateTranslation(0, _totalYScroll);
                // _logger.LogDebug(_totalYScroll);

                ds.DrawTextLayout(line.TextLayout, position, Colors.Transparent);

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawCoverImage(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            ds.Transform = Matrix3x2.CreateRotation(
                _coverBitmapRotateAngle,
                control.Size.ToVector2() * 0.5f
            );

            using var coverOverlayEffect = new OpacityEffect
            {
                Opacity = SettingsService.CoverOverlayOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = SettingsService.CoverOverlayBlurAmount,
                    Source = new ScaleEffect
                    {
                        InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
                        BorderMode = EffectBorderMode.Hard,
                        Scale = new Vector2(_coverScaleFactor),
                        Source = CanvasBitmap.CreateFromSoftwareBitmap(
                            control,
                            _coverSoftwareBitmap
                        ),
                    },
                },
            };
            ds.DrawImage(
                coverOverlayEffect,
                (float)control.Size.Width / 2 - _coverImagePixelWidth * _coverScaleFactor / 2,
                (float)control.Size.Height / 2 - _coverImagePixelHeight * _coverScaleFactor / 2
            );
            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawGradientOpacityMask(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            byte r,
            byte g,
            byte b
        )
        {
            byte verticalEdgeAlpha = (byte)(255 * SettingsService.LyricsVerticalEdgeOpacity / 100f);
            using var maskBrush = new CanvasLinearGradientBrush(
                control,
                [
                    new() { Position = 0, Color = Color.FromArgb(verticalEdgeAlpha, r, g, b) },
                    new() { Position = 0.5f, Color = Color.FromArgb(255, r, g, b) },
                    new() { Position = 1, Color = Color.FromArgb(verticalEdgeAlpha, r, g, b) },
                ]
            )
            {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height),
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        // Comsumes CPU related resources
        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _currentTime += args.Timing.ElapsedTime;

            if (SettingsService.IsDynamicCoverOverlay)
            {
                _coverBitmapRotateAngle += _coverRotateSpeed;
                _coverBitmapRotateAngle %= MathF.PI * 2;
            }
            if (SettingsService.IsLyricsDynamicGlowEffectEnabled)
            {
                _lyricsGlowEffectAngle += _lyricsGlowEffectSpeed;
                _lyricsGlowEffectAngle %= MathF.PI * 2;
            }

            if (SettingsService.IsCoverOverlayEnabled && _coverSoftwareBitmap != null)
            {
                var diagonal = Math.Sqrt(
                    Math.Pow(_lyricsAreaWidth, 2) + Math.Pow(_lyricsAreaHeight, 2)
                );

                _coverScaleFactor =
                    (float)diagonal / Math.Min(_coverImagePixelWidth, _coverImagePixelHeight);
            }

            if (_lyricsLines.LastOrDefault()?.TextLayout == null)
            {
                LayoutLyrics();
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            UpdateScaleAndOpacity(currentPlayingLineIndex);
            UpdatePosition(currentPlayingLineIndex);
        }

        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < _lyricsLines.Count; i++)
            {
                var line = _lyricsLines[i];
                if (line.EndPlayingTimestampMs < _currentTime.TotalMilliseconds)
                {
                    continue;
                }
                return i;
            }

            return -1;
        }

        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries()
        {
            // _logger.LogDebug($"{_startVisibleLineIndex} {_endVisibleLineIndex}");
            return new Tuple<int, int>(_startVisibleLineIndex, _endVisibleLineIndex);
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (_lyricsLines.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _lyricsLines.Count - 1);
        }

        private void UpdateScaleAndOpacity(int currentPlayingLineIndex)
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            for (int i = startLineIndex; _lyricsLines.Count > 0 && i <= endLineIndex; i++)
            {
                var line = _lyricsLines[i];

                bool linePlaying = i == currentPlayingLineIndex;

                var lineEnteringDurationMs = Math.Min(line.DurationMs, _lineEnteringDurationMs);
                var lineExitingDurationMs = _lineExitingDurationMs;
                if (i + 1 <= endLineIndex)
                {
                    lineExitingDurationMs = Math.Min(
                        _lyricsLines[i + 1].DurationMs,
                        lineExitingDurationMs
                    );
                }

                float lineEnteringProgress = 0.0f;
                float lineExitingProgress = 0.0f;

                bool lineEntering = false;
                bool lineExiting = false;

                float scale = _defaultScale;
                float opacity = _defaultOpacity;

                float playProgress = 0;

                if (linePlaying)
                {
                    line.PlayingState = LyricsPlayingState.Playing;

                    scale = _highlightedScale;
                    opacity = _highlightedOpacity;

                    playProgress =
                        ((float)_currentTime.TotalMilliseconds - line.StartPlayingTimestampMs)
                        / line.DurationMs;

                    var durationFromStartMs =
                        _currentTime.TotalMilliseconds - line.StartPlayingTimestampMs;
                    lineEntering = durationFromStartMs <= lineEnteringDurationMs;
                    if (lineEntering)
                    {
                        lineEnteringProgress = (float)durationFromStartMs / lineEnteringDurationMs;
                        scale =
                            _defaultScale
                            + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity =
                            _defaultOpacity
                            + (_highlightedOpacity - _defaultOpacity) * (float)lineEnteringProgress;
                    }
                }
                else
                {
                    if (i < currentPlayingLineIndex)
                    {
                        line.PlayingState = LyricsPlayingState.Played;
                        playProgress = 1;

                        var durationToEndMs =
                            _currentTime.TotalMilliseconds - line.EndPlayingTimestampMs;
                        lineExiting = durationToEndMs <= lineExitingDurationMs;
                        if (lineExiting)
                        {
                            lineExitingProgress = (float)durationToEndMs / lineExitingDurationMs;
                            scale =
                                _highlightedScale
                                - (_highlightedScale - _defaultScale) * (float)lineExitingProgress;
                            opacity =
                                _highlightedOpacity
                                - (_highlightedOpacity - _defaultOpacity)
                                    * (float)lineExitingProgress;
                        }
                    }
                    else
                    {
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

        private void LayoutLyrics()
        {
            using CanvasTextFormat textFormat = new()
            {
                FontSize = SettingsService.LyricsFontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontWeight = FontWeights.Bold,
                //FontFamily = "Segoe UI Mono",
            };
            float y = (float)_lyricsAreaHeight / 2;

            // Init Positions
            for (int i = 0; i < _lyricsLines.Count; i++)
            {
                var line = _lyricsLines[i];

                // Calculate layout bounds
                line.TextLayout = new CanvasTextLayout(
                    LyricsCanvas.Device,
                    line.Text,
                    textFormat,
                    (float)_lyricsCanvasMaxTextWidth,
                    (float)_lyricsAreaHeight
                );
                line.Position = new Vector2((float)_lyricsCanvasLeftMargin, y);

                y +=
                    (float)line.TextLayout.LayoutBounds.Height
                    / line.TextLayout.LineCount
                    * (line.TextLayout.LineCount + SettingsService.LyricsLineSpacingFactor);
            }
        }

        private void UpdatePosition(int currentPlayingLineIndex)
        {
            if (currentPlayingLineIndex < 0)
            {
                return;
            }

            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0)
            {
                return;
            }

            // Set _scrollOffsetY
            LyricsLine? currentPlayingLine = _lyricsLines?[currentPlayingLineIndex];

            if (currentPlayingLine == null)
            {
                return;
            }

            if (currentPlayingLine.TextLayout == null)
            {
                return;
            }

            var lineScrollingProgress =
                (_currentTime.TotalMilliseconds - currentPlayingLine.StartPlayingTimestampMs)
                / Math.Min(_lineScrollDurationMs, currentPlayingLine.DurationMs);

            var targetYScrollOffset = (float)(
                -currentPlayingLine.Position.Y
                + _lyricsLines![0].Position.Y
                - currentPlayingLine.TextLayout.LayoutBounds.Height / 2
                - _lastTotalYScroll
            );

            var yScrollOffset =
                targetYScrollOffset
                * EasingHelper.SmootherStep((float)Math.Min(1, lineScrollingProgress));

            bool isScrollingNow = lineScrollingProgress <= 1;

            if (isScrollingNow)
            {
                _totalYScroll = _lastTotalYScroll + yScrollOffset;
            }
            else
            {
                if (_forceToScroll && Math.Abs(targetYScrollOffset) >= 1)
                {
                    _totalYScroll = _lastTotalYScroll + targetYScrollOffset;
                }
                _lastTotalYScroll = _totalYScroll;
            }

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update Positions
            for (int i = startLineIndex; i >= 0 && i <= endLineIndex; i++)
            {
                var line = _lyricsLines[i];

                if (_totalYScroll + line.Position.Y + line.TextLayout.LayoutBounds.Height >= 0)
                {
                    if (_startVisibleLineIndex == -1)
                    {
                        _startVisibleLineIndex = i;
                    }
                }
                if (
                    _totalYScroll + line.Position.Y + line.TextLayout.LayoutBounds.Height
                    >= _lyricsAreaHeight
                )
                {
                    if (_endVisibleLineIndex == -1)
                    {
                        _endVisibleLineIndex = i;
                    }
                }
            }

            if (_startVisibleLineIndex != -1 && _endVisibleLineIndex == -1)
            {
                _endVisibleLineIndex = endLineIndex;
            }
        }

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            InitMediaManager();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.SettingsWindow is null)
            {
                var settingsWindow = new MainWindow();
                settingsWindow.Navigate(typeof(SettingsPage));
                App.Current.SettingsWindow = settingsWindow;
            }

            var appWindow = App.Current.SettingsWindow.AppWindow;

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }

            appWindow.Show();
            appWindow.MoveInZOrderAtTop();
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            TopCommandTeachingTip.IsOpen = true;
        }

        private void TopCommandTeachingTip_Closed(
            TeachingTip sender,
            TeachingTipClosedEventArgs args
        )
        {
            BottomCommandTeachingTip.IsOpen = true;
        }

        private void BottomCommandTeachingTip_Closed(
            TeachingTip sender,
            TeachingTipClosedEventArgs args
        )
        {
            LyricsOnlyTeachingTip.IsOpen = true;
        }

        private void LyricsOnlyTeachingTip_Closed(
            TeachingTip sender,
            TeachingTipClosedEventArgs args
        )
        {
            InitDatabaseTeachingTip.IsOpen = true;
        }

        private void InitDatabaseTeachingTip_Closed(
            TeachingTip sender,
            TeachingTipClosedEventArgs args
        )
        {
            SettingsService.IsFirstRun = false;
        }

        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }
    }
}
