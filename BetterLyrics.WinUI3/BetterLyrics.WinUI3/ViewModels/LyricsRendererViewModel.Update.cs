using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            bool isCanvasWidthChanged = _canvasWidth != control.Size.Width;
            bool isDisplayTypeChanged = _displayType != _displayTypeReceived;

            _canvasWidth = (float)control.Size.Width;
            _canvasHeight = (float)control.Size.Height;

            _albumArtY = 36 + (_canvasHeight - 36 * 2) * 3 / 16f;

            _displayType = _displayTypeReceived;

            if (_isPlaying)
            {
                _totalTime += args.Timing.ElapsedTime;
            }

            ElapsedTime = args.Timing.ElapsedTime;

            _immersiveBgTransition.Update(ElapsedTime);
            _albumArtBgTransition.Update(ElapsedTime);
            _songInfoOpacityTransition.Update(ElapsedTime);

            if (IsDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            _albumArtSize = MathF.Min(
                (_canvasHeight - _topMargin - _bottomMargin) * 8.5f / 16,
                (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2);
            _albumArtSize = MathF.Max(0, _albumArtSize);

            _titleY = _albumArtY + _albumArtSize * 1.05f;

            if (isDisplayTypeChanged || isCanvasWidthChanged)
            {
                bool jumpTo = !isDisplayTypeChanged && isCanvasWidthChanged;
                switch (_displayType)
                {
                    case LyricsDisplayType.AlbumArtOnly:
                        _lyricsOpacityTransition.StartTransition(0f, jumpTo);
                        _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                        _albumArtXTransition.StartTransition(_canvasWidth / 2 - _albumArtSize / 2, jumpTo);
                        break;
                    case LyricsDisplayType.LyricsOnly:
                        _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                        _albumArtOpacityTransition.StartTransition(0f, jumpTo);
                        _lyricsXTransition.StartTransition(_leftMargin, jumpTo);
                        _isRelayoutNeeded = true;
                        break;
                    case LyricsDisplayType.SplitView:
                        _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                        _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                        _lyricsXTransition.StartTransition((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2 + _leftMargin + _middleMargin, jumpTo);
                        _albumArtXTransition.StartTransition(_leftMargin + ((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2 - _albumArtSize) / 2, jumpTo);
                        _isRelayoutNeeded = true;
                        break;
                    case LyricsDisplayType.PlaceholderOnly:
                        break;
                    default:
                        break;
                }
            }

            _lyricsXTransition.Update(ElapsedTime);
            _albumArtXTransition.Update(ElapsedTime);
            _lyricsOpacityTransition.Update(ElapsedTime);
            _albumArtOpacityTransition.Update(ElapsedTime);

            if (_lyricsXTransition.IsTransitioning)
            {
                _isRelayoutNeeded = true;
            }

            _maxLyricsWidth = _canvasWidth - _lyricsXTransition.Value - _rightMargin;
            _maxLyricsWidth = Math.Max(_maxLyricsWidth, 0);

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

            _lyricsTextFormat.FontSize = LyricsFontSize;

            float y = 0;

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
                    line.DisplayedText,
                    _lyricsTextFormat,
                    _maxLyricsWidth,
                    _canvasHeight
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
                _canvasYScrollTransition.StartTransition(targetYScrollOffset, true);
            }

            _canvasYScrollTransition.Update(ElapsedTime);

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
                        + _canvasHeight / 2
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
                        + _canvasHeight / 2
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
            ThemeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(_lyricsWindowBgColor);

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

            var currentPlayingLine = _multiLangLyrics
                .SafeGet(_langIndex)
                ?.SafeGet(currentPlayingLineIndex);

            if (currentPlayingLine == null)
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

                float distanceFromPlayingLine = Math.Abs(line.Position.Y - currentPlayingLine.Position.Y);

                float distanceFactor = Math.Clamp(distanceFromPlayingLine / (_canvasHeight / 2), 0, 1);

                if (!line.AngleTransition.IsTransitioning)
                    line.AngleTransition.StartTransition(
                        _isFanLyricsEnabled
                            ? (float)Math.PI
                                * (30f / 180f)
                                * distanceFactor
                                * (i - currentPlayingLineIndex > 0 ? 1 : -1)
                            : 0
                    );

                if (!line.BlurAmountTransition.IsTransitioning)
                    line.BlurAmountTransition.StartTransition(LyricsBlurAmount * distanceFactor);

                if (!line.ScaleTransition.IsTransitioning)
                    line.ScaleTransition.StartTransition(
                    _highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)
                );

                if (!line.OpacityTransition.IsTransitioning)
                    line.OpacityTransition.StartTransition(_defaultOpacity - distanceFactor * _defaultOpacity * (1 - LyricsVerticalEdgeOpacity / 100f));

                // Only calculate highlight opacity for the current line and the two lines around it
                // to avoid unnecessary calculations
                if (!line.HighlightOpacityTransition.IsTransitioning)
                {
                    line.HighlightOpacityTransition.StartTransition(
                        distanceFromPlayingLine == 0 ? 1f : 0f
                    );
                }

                line.AngleTransition.Update(ElapsedTime);
                line.ScaleTransition.Update(ElapsedTime);
                line.BlurAmountTransition.Update(ElapsedTime);
                line.OpacityTransition.Update(ElapsedTime);
                line.HighlightOpacityTransition.Update(ElapsedTime);
            }
        }
    }
}
