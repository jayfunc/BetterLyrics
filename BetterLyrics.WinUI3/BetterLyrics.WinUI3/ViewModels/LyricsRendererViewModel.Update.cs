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

            switch (DisplayType)
            {
                case Enums.LyricsDisplayType.AlbumArtOnly:
                    _lyricsOpacityTransition.StartTransition(0f);
                    break;
                case Enums.LyricsDisplayType.LyricsOnly:
                case Enums.LyricsDisplayType.SplitView:
                    _lyricsOpacityTransition.StartTransition(1f);
                    break;
                case Enums.LyricsDisplayType.PlaceholderOnly:
                    break;
                default:
                    break;
            }

            if (_lyricsOpacityTransition.IsTransitioning)
            {
                _lyricsOpacityTransition.Update(ElapsedTime);
            }

            // 神光角度目标值（左右±15度摆动，周期约4秒）
            double t = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0;
            float targetAngle = (float)(-Math.PI / 2 + Math.Sin(t * Math.PI / 2) * (Math.PI / 12)); // -90°为正上，±15°摆动
            _shenGuangAngleTransition.StartTransition(targetAngle);

            if (_shenGuangAngleTransition.IsTransitioning)
            {
                _shenGuangAngleTransition.Update(ElapsedTime);
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

        private protected void UpdateFontColor()
        {
            ThemeTypeSent =
                Helper.ColorHelper.GetElementThemeFromBackgroundColor(_lyricsWindowBgColor);

            Color fallbackFg = Colors.Transparent;
            switch (ThemeTypeSent)
            {
                case ElementTheme.Light:
                    fallbackFg = _darkFontColor;
                    break;
                case ElementTheme.Dark:
                    fallbackFg = _lightFontColor;
                    break;
                default:
                    break;
            }

            switch (LyricsFontColorType)
            {
                case Enums.LyricsFontColorType.AdaptiveGrayed:
                    _fontColor = fallbackFg;
                    break;
                case Enums.LyricsFontColorType.AdaptiveColored:
                    _fontColor = _adaptiveFontColor ?? fallbackFg;
                    break;
                case Enums.LyricsFontColorType.Custom:
                    _fontColor = _customFontColor ?? fallbackFg;
                    break;
                default:
                    break;
            }
        }

        private void UpdateLinesProps()
        {
            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            int halfVisibleLineCount =
                Math.Max(1, Math.Max(
                    currentPlayingLineIndex - _startVisibleLineIndex,
                    _endVisibleLineIndex - currentPlayingLineIndex
                ));

            if (halfVisibleLineCount < 1)
            {
                return;
            }

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);

                if (line == null)
                {
                    continue;
                }

                int distanceFromPlayingLine = Math.Abs(i - currentPlayingLineIndex);
                if (distanceFromPlayingLine > halfVisibleLineCount)
                {
                    continue;
                }

                float distanceFactor = distanceFromPlayingLine / (float)halfVisibleLineCount;

                line.AngleTransition.StartTransition(
                    _isFanLyricsEnabled
                        ? (float)Math.PI
                            * (30f / 180f)
                            * distanceFactor
                            * (i - currentPlayingLineIndex > 0 ? 1 : -1)
                        : 0
                );
                line.BlurAmountTransition.StartTransition(LyricsBlurAmount * distanceFactor);
                line.ScaleTransition.StartTransition(
                    _highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)
                );
                line.OpacityTransition.StartTransition(_defaultOpacity - distanceFactor * _defaultOpacity * (1 - LyricsVerticalEdgeOpacity / 100f));

                // Only calculate highlight opacity for the current line and the two lines around it
                // to avoid unnecessary calculations
                if (distanceFromPlayingLine <= 1)
                {
                    line.HighlightOpacityTransition.StartTransition(
                        distanceFromPlayingLine == 0 ? 1 : 0
                    );
                }

                if (line.AngleTransition.IsTransitioning)
                {
                    line.AngleTransition.Update(ElapsedTime);
                }
                if (line.ScaleTransition.IsTransitioning)
                {
                    line.ScaleTransition.Update(ElapsedTime);
                }
                if (line.BlurAmountTransition.IsTransitioning)
                {
                    line.BlurAmountTransition.Update(ElapsedTime);
                }
                if (line.OpacityTransition.IsTransitioning)
                {
                    line.OpacityTransition.Update(ElapsedTime);
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
    }
}
