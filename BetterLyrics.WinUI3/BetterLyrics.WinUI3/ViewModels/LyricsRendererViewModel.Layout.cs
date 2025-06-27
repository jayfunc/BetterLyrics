using System;
using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="args">The args<see cref="CanvasAnimatedUpdateEventArgs"/></param>
        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            if (_isPlaying)
            {
                TotalTime += args.Timing.ElapsedTime;
            }

            ElapsedTime = args.Timing.ElapsedTime;

            if (_immersiveBgTransition.IsTransitioning)
            {
                _immersiveBgTransition.Update(ElapsedTime);
            }

            if (_albumArtBgTransition.IsTransitioning)
            {
                _albumArtBgTransition.Update(ElapsedTime);
            }

            if (IsDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            if (_maxLyricsWidthTransition.IsTransitioning)
            {
                _maxLyricsWidthTransition.Update(ElapsedTime);
                _isRelayoutNeeded = true;
            }

            if (_isRelayoutNeeded)
            {
                ReLayout(control);
                _isRelayoutNeeded = false;
                UpdateCanvasYScrollOffset(control, false);
            }
            else
            {
                UpdateCanvasYScrollOffset(control, true);
            }

            UpdateLinesProps();
        }

        /// <summary>
        /// The UpdateCanvasYScrollOffset
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        private void UpdateCanvasYScrollOffset(ICanvasAnimatedControl control, bool withAnimation)
        {
            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0)
            {
                return;
            }

            // Set _scrollOffsetY
            LyricsLine? currentPlayingLine = _multiLangLyrics
                .SafeGet(_langIndex)
                ?.SafeGet(currentPlayingLineIndex);

            var playingTextLayout = currentPlayingLine?.CanvasTextLayout;

            if (currentPlayingLine == null || playingTextLayout == null)
            {
                return;
            }

            float targetYScrollOffset =
                (float?)(
                    -currentPlayingLine.Position.Y
                    + _multiLangLyrics.SafeGet(_langIndex)?[0].Position.Y
                    - playingTextLayout.LayoutBounds.Height / 2
                ) ?? 0f;

            if (withAnimation && !_canvasYScrollTransition.IsTransitioning)
            {
                _canvasYScrollTransition.StartTransition(targetYScrollOffset);
            }
            else if (!withAnimation)
            {
                _canvasYScrollTransition.JumpTo(targetYScrollOffset);
            }

            if (_canvasYScrollTransition.IsTransitioning)
            {
                _canvasYScrollTransition.Update(ElapsedTime);
            }

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update visible line indices
            for (int i = startLineIndex; i <= endLineIndex; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);

                if (line == null || line.CanvasTextLayout == null)
                {
                    continue;
                }

                var textLayout = line.CanvasTextLayout;

                if (
                    _canvasYScrollTransition.Value
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
                    _canvasYScrollTransition.Value
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

        /// <summary>
        /// The UpdateFontColor
        /// </summary>
        private protected void UpdateFontColor()
        {
            Color fallback = Colors.Transparent;
            switch (Theme)
            {
                case ElementTheme.Default:
                    switch (Application.Current.RequestedTheme)
                    {
                        case ApplicationTheme.Light:
                            fallback = _darkFontColor;
                            break;
                        case ApplicationTheme.Dark:
                            fallback = _lightFontColor;
                            break;
                        default:
                            break;
                    }
                    break;
                case ElementTheme.Light:
                    fallback = _darkFontColor;
                    break;
                case ElementTheme.Dark:
                    fallback = _lightFontColor;
                    break;
                default:
                    break;
            }

            switch (LyricsFontColorType)
            {
                case Enums.LyricsFontColorType.Default:
                    _fontColor = fallback;
                    break;
                case Enums.LyricsFontColorType.Dominant:
                    _fontColor = _albumArtAccentColor ?? fallback;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// The UpdateLinesProps
        /// </summary>
        /// <param name="source">The source<see cref="List{LyricsLine}?"/></param>
        /// <param name="defaultOpacity">The defaultOpacity<see cref="float"/></param>
        private void UpdateLinesProps()
        {
            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            int halfVisibleLineCount =
                Math.Max(
                    currentPlayingLineIndex - _startVisibleLineIndex,
                    _endVisibleLineIndex - currentPlayingLineIndex
                ) + 1;

            if (halfVisibleLineCount < 1)
            {
                return;
            }

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);

                if (line == null)
                {
                    return;
                }

                int distanceFromPlayingLine = Math.Abs(i - currentPlayingLineIndex);
                if (distanceFromPlayingLine > halfVisibleLineCount)
                {
                    return;
                }

                float distanceZoomFactor = distanceFromPlayingLine / (float)halfVisibleLineCount;

                line.BlurAmountTransition.StartTransition(LyricsBlurAmount * distanceZoomFactor);
                line.ScaleTransition.StartTransition(
                    _highlightedScale - distanceZoomFactor * (_highlightedScale - _defaultScale)
                );
                // Only calculate highlight opacity for the current line and the two lines around it
                // to avoid unnecessary calculations
                if (distanceFromPlayingLine <= 1)
                {
                    line.HighlightOpacityTransition.StartTransition(
                        distanceFromPlayingLine == 0 ? 1 : 0
                    );
                }

                if (line.ScaleTransition.IsTransitioning)
                {
                    line.ScaleTransition.Update(ElapsedTime);
                }
                if (line.BlurAmountTransition.IsTransitioning)
                {
                    line.BlurAmountTransition.Update(ElapsedTime);
                }
                // Only update highlight opacity for the current line and the two lines around it
                if (distanceFromPlayingLine <= 1)
                {
                    if (line.HighlightOpacityTransition.IsTransitioning)
                    {
                        line.HighlightOpacityTransition.Update(ElapsedTime);
                    }
                }
            }
        }

        /// <summary>
        /// Reassigns positions (x,y) to lyrics lines based on the current control size and font size
        /// </summary>
        /// <param name="control"></param>
        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            _textFormat.FontSize = LyricsFontSize;

            float y = _topMargin;

            // Init Positions
            for (int i = 0; i < _multiLangLyrics.SafeGet(_langIndex)?.Count; i++)
            {
                var line = _multiLangLyrics[_langIndex].SafeGet(i);

                if (line == null)
                {
                    continue;
                }

                if (line.CanvasTextLayout != null)
                {
                    line.CanvasTextLayout.Dispose();
                    line.CanvasTextLayout = null;
                }

                // Calculate layout bounds
                line.CanvasTextLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)_maxLyricsWidthTransition.Value,
                    (float)control.Size.Height
                );

                line.Position = new Vector2(0, y);

                y +=
                    (float)line.CanvasTextLayout.LayoutBounds.Height
                    / line.CanvasTextLayout.LineCount
                    * (line.CanvasTextLayout.LineCount + LyricsLineSpacingFactor);
            }
        }
    }
}
