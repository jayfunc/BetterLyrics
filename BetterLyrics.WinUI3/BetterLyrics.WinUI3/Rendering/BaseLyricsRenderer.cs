using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Rendering
{
    public abstract class BaseLyricsRenderer
    {
        private CanvasTextFormat _textFormat;

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

        public TimeSpan CurrentTime { get; set; } = TimeSpan.Zero;

        public abstract List<LyricsLine> LyricsLines { get; set; }

        private readonly ILyricsViewModel _viewModel;

        public BaseLyricsRenderer(ILyricsViewModel viewModel)
        {
            _viewModel = viewModel;
            _textFormat = new()
            {
                FontSize = viewModel.LyricsFontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontWeight = FontWeights.Bold,
                //FontFamily = "Segoe UI Mono",
            };

            UpdateFontColor();

            WeakReferenceMessenger.Default.Register<BaseLyricsRenderer, ThemeChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateFontColor();
                }
            );

            WeakReferenceMessenger.Default.Register<
                BaseLyricsRenderer,
                LyricsFontColorChangedMessage
            >(
                this,
                (r, m) =>
                {
                    UpdateFontColor();
                }
            );
        }

        public void SetLimitedLineWidth(double width)
        {
            LimitedLineWidth = width;
        }

        public void AddElapsedTime(TimeSpan elapsedTime)
        {
            CurrentTime += elapsedTime;
        }

        private void UpdateFontColor()
        {
            switch (_viewModel.LyricsFontColorType)
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
            for (int i = 0; i < LyricsLines.Count; i++)
            {
                var line = LyricsLines[i];
                if (line.EndPlayingTimestampMs < CurrentTime.TotalMilliseconds)
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
            if (LyricsLines.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, LyricsLines.Count - 1);
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
                LyricsLines.Count > 0
                    && i >= 0
                    && i < LyricsLines.Count
                    && i <= displayEndLineIndex;
                i++
            )
            {
                var line = LyricsLines[i];

                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)LimitedLineWidth,
                    (float)control.Size.Height
                );

                float progressPerChar = 1f / line.Text.Length;

                var position = new Vector2(line.Position.X, line.Position.Y);

                float centerX = position.X;
                float centerY = position.Y + (float)textLayout.LayoutBounds.Height / 2;

                switch (_viewModel.LyricsAlignmentType)
                {
                    case LyricsAlignmentType.Left:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += (float)LimitedLineWidth / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)LimitedLineWidth;
                        break;
                    default:
                        break;
                }

                int startIndex = 0;

                // Set brush
                for (int j = 0; j < textLayout.LineCount; j++)
                {
                    int count = textLayout.LineMetrics[j].CharacterCount;
                    var regions = textLayout.GetCharacterRegions(startIndex, count);
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

                    textLayout.SetBrush(startIndex, count, horizontalFillBrush);
                    startIndex += count;
                }

                // Scale
                ds.Transform =
                    Matrix3x2.CreateScale(line.Scale, new Vector2(centerX, centerY))
                    * Matrix3x2.CreateTranslation(
                        (float)(control.Size.Width - _rightMargin - LimitedLineWidth),
                        _totalYScroll + (float)(control.Size.Height / 2)
                    );
                // _logger.LogDebug(_totalYScroll);

                ds.DrawTextLayout(textLayout, position, Colors.Transparent);

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }

            //ds.DrawText(
            //    $"show range: from {_startVisibleLineIndex} to {_endVisibleLineIndex}, current: {GetCurrentPlayingLineIndex()}",
            //    new Vector2(10, 10),
            //    Colors.Red
            //);
            ds.DrawText($"{LimitedLineWidth}", new Vector2(10, 10), Colors.Red);
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
            await Task.Delay(1);
            _forceToScroll = false;
        }

        public async Task ReLayoutAsync(
            ICanvasAnimatedControl control,
            IList<LyricsLine>? updatedLyricsLines = null
        )
        {
            if (control == null)
                return;

            if (updatedLyricsLines != null)
            {
                LyricsLines = [.. updatedLyricsLines];
            }

            _textFormat.FontSize = _viewModel.LyricsFontSize;

            float y = 0;

            // Init Positions
            for (int i = 0; i < LyricsLines.Count; i++)
            {
                var line = LyricsLines[i];

                // Calculate layout bounds
                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)LimitedLineWidth,
                    (float)control.Size.Height
                );
                line.Position = new Vector2(0, y);

                y +=
                    (float)textLayout.LayoutBounds.Height
                    / textLayout.LineCount
                    * (textLayout.LineCount + _viewModel.LyricsLineSpacingFactor);
            }

            await ForceToScrollToCurrentPlayingLineAsync();
        }

        public void Calculate(ICanvasAnimatedControl control)
        {
            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            CalculateScaleAndOpacity(currentPlayingLineIndex);
            CalculatePosition(control, currentPlayingLineIndex);

            if (_viewModel.IsLyricsDynamicGlowEffectEnabled)
            {
                _lyricsGlowEffectAngle += _lyricsGlowEffectSpeed;
                _lyricsGlowEffectAngle %= MathF.PI * 2;
            }
        }

        private void CalculateScaleAndOpacity(int currentPlayingLineIndex)
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            for (int i = startLineIndex; LyricsLines.Count > 0 && i <= endLineIndex; i++)
            {
                var line = LyricsLines[i];

                bool linePlaying = i == currentPlayingLineIndex;

                var lineEnteringDurationMs = Math.Min(line.DurationMs, _lineEnteringDurationMs);
                var lineExitingDurationMs = _lineExitingDurationMs;
                if (i + 1 <= endLineIndex)
                {
                    lineExitingDurationMs = Math.Min(
                        LyricsLines[i + 1].DurationMs,
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
                        ((float)CurrentTime.TotalMilliseconds - line.StartPlayingTimestampMs)
                        / line.DurationMs;

                    var durationFromStartMs =
                        CurrentTime.TotalMilliseconds - line.StartPlayingTimestampMs;
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
                            CurrentTime.TotalMilliseconds - line.EndPlayingTimestampMs;
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

        private void CalculatePosition(ICanvasAnimatedControl control, int currentPlayingLineIndex)
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
            LyricsLine? currentPlayingLine = LyricsLines?[currentPlayingLineIndex];

            if (currentPlayingLine == null)
            {
                return;
            }

            using var playingTextLayout = new CanvasTextLayout(
                control,
                currentPlayingLine.Text,
                _textFormat,
                (float)LimitedLineWidth,
                (float)control.Size.Height
            );

            var lineScrollingProgress =
                (CurrentTime.TotalMilliseconds - currentPlayingLine.StartPlayingTimestampMs)
                / Math.Min(_lineScrollDurationMs, currentPlayingLine.DurationMs);

            var targetYScrollOffset = (float)(
                -currentPlayingLine.Position.Y
                + LyricsLines![0].Position.Y
                - playingTextLayout.LayoutBounds.Height / 2
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
            for (int i = startLineIndex; i >= 0 && i <= endLineIndex && i < LyricsLines.Count; i++)
            {
                var line = LyricsLines[i];

                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)LimitedLineWidth,
                    (float)control.Size.Height
                );

                if (
                    _totalYScroll
                        + (float)(control.Size.Height / 2)
                        + line.Position.Y
                        + textLayout.LayoutBounds.Height
                    >= 0
                )
                {
                    if (_startVisibleLineIndex == -1)
                    {
                        _startVisibleLineIndex = i;
                    }
                }
                if (
                    _totalYScroll
                        + (float)(control.Size.Height / 2)
                        + line.Position.Y
                        + textLayout.LayoutBounds.Height
                    >= control.Size.Height
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
