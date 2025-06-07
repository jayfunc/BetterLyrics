using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Text;
using Windows.UI;

namespace BetterLyrics.WinUI3.Rendering
{
    public class PureLyricsRenderer
    {
        private readonly SettingsService _settingsService;

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

        private readonly double _rightMargin = 36;

        public double LimitedLineWidth { get; set; } = 0;
        public double CanvasWidth { get; set; } = 0;
        public double CanvasHeight { get; set; } = 0;

        public TimeSpan CurrentTime { get; set; }

        public List<LyricsLine> LyricsLines { get; set; } = [];

        public PureLyricsRenderer()
        {
            _settingsService = Ioc.Default.GetService<SettingsService>()!;
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

        public void Draw(
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
                LyricsLines.Count > 0
                    && i >= 0
                    && i < LyricsLines.Count
                    && i <= displayEndLineIndex;
                i++
            )
            {
                var line = LyricsLines[i];

                if (line.TextLayout == null)
                {
                    return;
                }

                float progressPerChar = 1f / line.Text.Length;

                var position = line.Position;

                float centerX = position.X;
                float centerY = position.Y + (float)line.TextLayout.LayoutBounds.Height / 2;

                switch ((LyricsAlignmentType)_settingsService.LyricsAlignmentType)
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

        public async Task ForceToScrollToCurrentPlayingLineAsync()
        {
            _forceToScroll = true;
            await Task.Delay(1);
            _forceToScroll = false;
        }

        public async Task ReLayoutAsync(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            float leftMargin = (float)(CanvasWidth - LimitedLineWidth - _rightMargin);

            using CanvasTextFormat textFormat = new()
            {
                FontSize = _settingsService.LyricsFontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontWeight = FontWeights.Bold,
                //FontFamily = "Segoe UI Mono",
            };
            float y = (float)CanvasHeight / 2;

            // Init Positions
            for (int i = 0; i < LyricsLines.Count; i++)
            {
                var line = LyricsLines[i];

                // Calculate layout bounds
                line.TextLayout = new CanvasTextLayout(
                    control.Device,
                    line.Text,
                    textFormat,
                    (float)LimitedLineWidth,
                    (float)CanvasHeight
                );
                line.Position = new Vector2(leftMargin, y);

                y +=
                    (float)line.TextLayout.LayoutBounds.Height
                    / line.TextLayout.LineCount
                    * (line.TextLayout.LineCount + _settingsService.LyricsLineSpacingFactor);
            }

            await ForceToScrollToCurrentPlayingLineAsync();
        }

        public void CalculateScaleAndOpacity(int currentPlayingLineIndex)
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

        public void CalculatePosition(ICanvasAnimatedControl control, int currentPlayingLineIndex)
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

            if (currentPlayingLine.TextLayout == null)
            {
                return;
            }

            var lineScrollingProgress =
                (CurrentTime.TotalMilliseconds - currentPlayingLine.StartPlayingTimestampMs)
                / Math.Min(_lineScrollDurationMs, currentPlayingLine.DurationMs);

            var targetYScrollOffset = (float)(
                -currentPlayingLine.Position.Y
                + LyricsLines![0].Position.Y
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
                var line = LyricsLines[i];

                if (_totalYScroll + line.Position.Y + line.TextLayout.LayoutBounds.Height >= 0)
                {
                    if (_startVisibleLineIndex == -1)
                    {
                        _startVisibleLineIndex = i;
                    }
                }
                if (
                    _totalYScroll + line.Position.Y + line.TextLayout.LayoutBounds.Height
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
