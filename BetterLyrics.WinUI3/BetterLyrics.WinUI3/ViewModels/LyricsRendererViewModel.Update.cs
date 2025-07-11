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
        private bool _isCanvasWidthChanged = false;
        private bool _isCanvasHeightChanged = false;

        private bool _isDisplayTypeChanged = false;
        
        private bool _isPlayingLineChanged = false;
        private bool _isVisibleLinesBoundaryChanged = false;

        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            _elapsedTime = args.Timing.ElapsedTime;

            if (_isPlaying)
            {
                _totalTime += _elapsedTime;
            }

            var playingLineIndex = GetCurrentPlayingLineIndex();

            _isCanvasWidthChanged = _canvasWidth != control.Size.Width;
            _isCanvasHeightChanged = _canvasHeight != control.Size.Height;
            _isDisplayTypeChanged = _displayType != _displayTypeReceived;
            _isPlayingLineChanged = _playingLineIndex != playingLineIndex;

            _canvasWidth = (float)control.Size.Width;
            _canvasHeight = (float)control.Size.Height;
            _displayType = _displayTypeReceived;
            _playingLineIndex = playingLineIndex;

            _immersiveBgOpacityTransition.Update(_elapsedTime);
            _immersiveBgTransition.Update(_elapsedTime);
            _albumArtBgTransition.Update(_elapsedTime);
            _lyricsBgBrightnessTransition.Update(_elapsedTime);
            _songInfoOpacityTransition.Update(_elapsedTime);

            if (_isDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            if (_isCanvasHeightChanged)
            {
                _albumArtY = 36 + (_canvasHeight - 36 * 2) * 3 / 16f;
            }

            if (_isCanvasWidthChanged || _isCanvasHeightChanged)
            {
                _albumArtSize = MathF.Min(
                    (_canvasHeight - _topMargin - _bottomMargin) * 8.5f / 16,
                    (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2);
                _albumArtSize = MathF.Max(0, _albumArtSize);

                _titleY = _albumArtY + _albumArtSize * 1.05f;
            }

            if (_isDisplayTypeChanged || _isCanvasWidthChanged)
            {
                bool jumpTo = !_isDisplayTypeChanged && _isCanvasWidthChanged;
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
                        break;
                    case LyricsDisplayType.SplitView:
                        _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                        _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                        _lyricsXTransition.StartTransition((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2 + _leftMargin + _middleMargin, jumpTo);
                        _albumArtXTransition.StartTransition(_leftMargin + ((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2 - _albumArtSize) / 2, jumpTo);
                        break;
                    case LyricsDisplayType.PlaceholderOnly:
                        break;
                    default:
                        break;
                }
            }

            _lyricsXTransition.Update(_elapsedTime);
            _albumArtXTransition.Update(_elapsedTime);
            _lyricsOpacityTransition.Update(_elapsedTime);
            _albumArtOpacityTransition.Update(_elapsedTime);

            if (_isCanvasWidthChanged || _lyricsXTransition.IsTransitioning)
            {
                _maxLyricsWidth = _canvasWidth - _lyricsXTransition.Value - _rightMargin;
                _maxLyricsWidth = Math.Max(_maxLyricsWidth, 0);
                _isLayoutChanged = true;
            }

            if (_isLayoutChanged)
            {
                ReLayout(control);
                UpdateCanvasYScrollOffset(control, true, false);
            }
            else
            {
                UpdateCanvasYScrollOffset(control, false, true);
            }

            UpdateLinesProps();

            _isLayoutChanged = false;
        }

        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            _lyricsTextFormat.FontSize = _lyricsFontSize;

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
                    * (line.CanvasTextLayout.LineCount + _lyricsLineSpacingFactor);
            }
        }

        private void UpdateCanvasYScrollOffset(ICanvasAnimatedControl control, bool forceScroll, bool withAnimation)
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0) return;

            // Set _scrollOffsetY

            if ((!_isPlayingLineChanged && forceScroll) || _isPlayingLineChanged)
            {
                LyricsLine? currentPlayingLine = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(_playingLineIndex);

                if (currentPlayingLine == null) return;

                var playingTextLayout = currentPlayingLine?.CanvasTextLayout;

                if (playingTextLayout == null) return;

                float? targetYScrollOffset = (float?)(-currentPlayingLine!.Position.Y + _multiLangLyrics.SafeGet(_langIndex)?[0].Position.Y - playingTextLayout.LayoutBounds.Height / 2);

                if (!targetYScrollOffset.HasValue) return;

                _canvasYScrollTransition.StartTransition(targetYScrollOffset.Value, !withAnimation);
            }

            _canvasYScrollTransition.Update(_elapsedTime);

            int startVisibleLineIndex = -1;
            int endVisibleLineIndex = -1;

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
                    if (startVisibleLineIndex == -1)
                    {
                        startVisibleLineIndex = i;
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
                    if (endVisibleLineIndex == -1)
                    {
                        endVisibleLineIndex = i;
                    }
                }
            }

            if (startVisibleLineIndex != -1 && endVisibleLineIndex == -1)
            {
                endVisibleLineIndex = endLineIndex;
            }

            _isVisibleLinesBoundaryChanged = _startVisibleLineIndex != startVisibleLineIndex || _endVisibleLineIndex != endVisibleLineIndex;

            _startVisibleLineIndex = startVisibleLineIndex;
            _endVisibleLineIndex = endVisibleLineIndex;
        }

        private void UpdateFontColor()
        {
            if (_isDesktopMode || _isDockMode)
            {
                ThemeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(_environmentalColor);
            }
            else
            {
                ThemeTypeSent = _lyricsBgTheme;
            }

            float brightness = 0f;

            Color grayedEnvironmentalColor = Colors.Transparent;

            switch (ThemeTypeSent)
            {
                case ElementTheme.Default:
                    switch (Application.Current.RequestedTheme)
                    {
                        case ApplicationTheme.Light:
                            _adaptiveGrayedFontColor = _darkColor;
                            brightness = 0.7f;
                            break;
                        case ApplicationTheme.Dark:
                            _adaptiveGrayedFontColor = _lightColor;
                            brightness = 0.3f;
                            break;
                        default:
                            break;
                    }
                    break;
                case ElementTheme.Light:
                    _adaptiveGrayedFontColor = _darkColor;
                    brightness = 0.7f;
                    break;
                case ElementTheme.Dark:
                    _adaptiveGrayedFontColor = _lightColor;
                    brightness = 0.3f;
                    break;
                default:
                    break;
            }

            if (_adaptiveGrayedFontColor == _lightColor)
            {
                grayedEnvironmentalColor = _darkColor;
            } else if (_adaptiveGrayedFontColor == _darkColor)
            {
                grayedEnvironmentalColor = _lightColor;
            }

            _lyricsBgBrightnessTransition.StartTransition(brightness);

            if (_isDesktopMode || _isDockMode)
            {
                _adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(_environmentalColor);
            }
            else
            {
                _adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(_albumArtAccentColor?.WithBrightness(brightness) ?? Colors.Transparent);
            }

            switch (_lyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _bgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _bgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _bgFontColor = _customBgFontColor ?? _adaptiveGrayedFontColor;
                    break;
                default:
                    break;
            }

            switch (_lyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _fgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _fgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _fgFontColor = _customFgFontColor ?? _adaptiveGrayedFontColor;
                    break;
                default:
                    break;
            }

            switch (_lyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _strokeFontColor = grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _strokeFontColor = _environmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    _strokeFontColor = _customStrokeFontColor ?? _environmentalColor;
                    break;
                default:
                    break;
            }
        }

        private void UpdateLinesProps()
        {
            var currentPlayingLine = _multiLangLyrics
                .SafeGet(_langIndex)
                ?.SafeGet(_playingLineIndex);

            if (currentPlayingLine == null) return;

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);

                if (line == null) continue;

                if (_isLayoutChanged || _isVisibleLinesBoundaryChanged || _isPlayingLineChanged)
                {
                    float distanceFromPlayingLine = Math.Abs(line.Position.Y - currentPlayingLine.Position.Y);
                    float distanceFactor = Math.Clamp(distanceFromPlayingLine / (_canvasHeight / 2), 0, 1);

                    line.AngleTransition.StartTransition(_isFanLyricsEnabled
                            ? (float)Math.PI
                                * (30f / 180f)
                                * distanceFactor
                                * (i > _playingLineIndex ? 1 : -1)
                            : 0
                    );

                    line.BlurAmountTransition.StartTransition(_lyricsBlurAmount * distanceFactor);
                    line.ScaleTransition.StartTransition(_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale));
                    line.OpacityTransition.StartTransition(_defaultOpacity - distanceFactor * _defaultOpacity * (1 - _lyricsVerticalEdgeOpacity / 100f));
                    line.HighlightOpacityTransition.StartTransition(i == _playingLineIndex ? 1f : 0f);
                }

                line.AngleTransition.Update(_elapsedTime);
                line.ScaleTransition.Update(_elapsedTime);
                line.BlurAmountTransition.Update(_elapsedTime);
                line.OpacityTransition.Update(_elapsedTime);
                line.HighlightOpacityTransition.Update(_elapsedTime);
            }
        }
    }
}
