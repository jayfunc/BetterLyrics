using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Rendering
{
    public class LyricsRenderer
    {
        private readonly LyricsViewModel _viewModel;

        public ICanvasAnimatedControl? Control { private get; set; } = null;
        private Color _fontColor;

        private readonly float _defaultOpacity = 0.3f;
        private readonly float _highlightedOpacity = 1.0f;

        private readonly float _defaultScale = 0.95f;
        private readonly float _highlightedScale = 1.0f;

        private readonly int _lineEnteringDurationMs = 800;
        private readonly int _lineExitingDurationMs = 800;
        private readonly int _lineScrollDurationMs = 800;

        private float _lastTotalYScroll = 0.0f;
        private float _totalYScroll = 0.0f;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _forceToScroll = false;

        private float _lyricsGlowEffectAngle = 0f;
        private readonly float _lyricsGlowEffectSpeed = 0.01f;

        private readonly float _lyricsGlowEffectMinBlurAmount = 0f;
        private readonly float _lyricsGlowEffectMaxBlurAmount = 6f;

        private readonly double _rightMargin = 36;

        public double LimitedLineWidth { get; set; } = 0;
        public double CanvasWidth { get; set; } = 0;
        public double CanvasHeight { get; set; } = 0;

        private TimeSpan _currentTime = TimeSpan.Zero;

        private List<LyricsLine> _lyricsLines = [];

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public LyricsRenderer(LyricsViewModel settingsViewModel)
        {
            _viewModel = settingsViewModel;

            UpdateFontColor();

            WeakReferenceMessenger.Default.Register<LyricsRenderer, PlayingPositionChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () =>
                        {
                            _currentTime = m.Value;
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<LyricsRenderer, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            _lyricsLines = m.Value?.LyricsLines ?? [];
                            await ForceToScrollToCurrentPlayingLineAsync();
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<LyricsRenderer, ThemeChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateFontColor();
                }
            );

            WeakReferenceMessenger.Default.Register<LyricsRenderer, LyricsFontColorChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateFontColor();
                }
            );

            WeakReferenceMessenger.Default.Register<LyricsRenderer, LyricsRelayoutRequestedMessage>(
                this,
                async (r, m) =>
                {
                    await ReLayoutAsync();
                }
            );
        }

        public void AddElapsedTime(TimeSpan elapsedTime)
        {
            _currentTime += elapsedTime;
        }

        private void UpdateFontColor()
        {
            switch ((LyricsFontColorType)_viewModel.LyricsFontColorType)
            {
                case LyricsFontColorType.Default:
                    _fontColor = (
                        App.Current.Resources["ControlFillColorDefaultBrush"] as SolidColorBrush
                    )!.Color;
                    break;
                case LyricsFontColorType.Dominant:
                    _fontColor = _viewModel.CoverImageDominantColors[
                        Math.Max(
                            0,
                            Math.Min(
                                _viewModel.CoverImageDominantColors.Count - 1,
                                _viewModel.LyricsFontSelectedAccentColorIndex
                            )
                        )
                    ];
                    break;
                default:
                    break;
            }
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

        private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var (displayStartLineIndex, displayEndLineIndex) =
                GetVisibleLyricsLineIndexBoundaries();

            var r = _fontColor.R;
            var g = _fontColor.G;
            var b = _fontColor.B;

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

                switch ((LyricsAlignmentType)_viewModel.LyricsAlignmentType)
                {
                    case LyricsAlignmentType.Left:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += (float)LimitedLineWidth / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        line.TextLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)LimitedLineWidth;
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

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            // Lyrics only layer
            using var lyrics = new CanvasCommandList(control);
            using (var lyricsDs = lyrics.CreateDrawingSession())
            {
                DrawLyrics(control, lyricsDs);
            }

            using var glowedLyrics = new CanvasCommandList(control);
            using (var glowedLyricsDs = glowedLyrics.CreateDrawingSession())
            {
                if (_viewModel.IsLyricsGlowEffectEnabled)
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
            using var combinedBlurredLyrics = new CanvasCommandList(control);
            using var combinedBlurredLyricsDs = combinedBlurredLyrics.CreateDrawingSession();
            if (_viewModel.LyricsBlurAmount == 0)
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
                        BlurAmount = (float)(_viewModel.LyricsBlurAmount * (1 - i / (0.5 - step))),
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    };
                    using var topCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            control.Size.Height * i,
                            control.Size.Width,
                            control.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    using var bottomCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            control.Size.Height * (1 - i - step * (1 + overlapFactor)),
                            control.Size.Width,
                            control.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    combinedBlurredLyricsDs.DrawImage(topCropped);
                    combinedBlurredLyricsDs.DrawImage(bottomCropped);
                }
            }

            // Masked mock gradient blurred lyrics layer
            using var maskedCombinedBlurredLyrics = new CanvasCommandList(control);
            using (
                var maskedCombinedBlurredLyricsDs =
                    maskedCombinedBlurredLyrics.CreateDrawingSession()
            )
            {
                if (_viewModel.LyricsVerticalEdgeOpacity == 100)
                {
                    maskedCombinedBlurredLyricsDs.DrawImage(combinedBlurredLyrics);
                }
                else
                {
                    using var mask = new CanvasCommandList(control);
                    using (var maskDs = mask.CreateDrawingSession())
                    {
                        DrawGradientOpacityMask(control, maskDs);
                    }
                    maskedCombinedBlurredLyricsDs.DrawImage(
                        new AlphaMaskEffect { Source = combinedBlurredLyrics, AlphaMask = mask }
                    );
                }
            }

            // Draw the final composed layer
            ds.DrawImage(maskedCombinedBlurredLyrics);
        }

        private void DrawGradientOpacityMask(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds
        )
        {
            byte verticalEdgeAlpha = (byte)(255 * _viewModel.LyricsVerticalEdgeOpacity / 100f);
            using var maskBrush = new CanvasLinearGradientBrush(
                control,
                [
                    new() { Position = 0, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                    new() { Position = 0.5f, Color = Color.FromArgb(255, 0, 0, 0) },
                    new() { Position = 1, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                ]
            )
            {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height),
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        public async Task ForceToScrollToCurrentPlayingLineAsync()
        {
            _forceToScroll = true;
            await Task.Delay(100);
            _forceToScroll = false;
        }

        public async Task ReLayoutAsync()
        {
            if (Control == null)
                return;

            float leftMargin = (float)(CanvasWidth - LimitedLineWidth - _rightMargin);

            using CanvasTextFormat textFormat = new()
            {
                FontSize = _viewModel.LyricsFontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontWeight = FontWeights.Bold,
                //FontFamily = "Segoe UI Mono",
            };
            float y = (float)CanvasHeight / 2;

            // Init Positions
            for (int i = 0; i < _lyricsLines.Count; i++)
            {
                var line = _lyricsLines[i];

                // Calculate layout bounds
                line.TextLayout = new CanvasTextLayout(
                    Control.Device,
                    line.Text,
                    textFormat,
                    (float)LimitedLineWidth,
                    (float)CanvasHeight
                );
                line.Position = new Vector2(leftMargin, y);

                y +=
                    (float)line.TextLayout.LayoutBounds.Height
                    / line.TextLayout.LineCount
                    * (line.TextLayout.LineCount + _viewModel.LyricsLineSpacingFactor);
            }

            await ForceToScrollToCurrentPlayingLineAsync();
        }

        public async Task CalculateAsync()
        {
            if (_lyricsLines.LastOrDefault()?.TextLayout == null)
            {
                await ReLayoutAsync();
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            CalculateScaleAndOpacity(currentPlayingLineIndex);
            CalculatePosition(currentPlayingLineIndex);

            if (_viewModel.IsLyricsDynamicGlowEffectEnabled)
            {
                _lyricsGlowEffectAngle += _lyricsGlowEffectSpeed;
                _lyricsGlowEffectAngle %= MathF.PI * 2;
            }
        }

        private void CalculateScaleAndOpacity(int currentPlayingLineIndex)
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

        private void CalculatePosition(int currentPlayingLineIndex)
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
                    >= Control.Size.Height
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
    }
}
