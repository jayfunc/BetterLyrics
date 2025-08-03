using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        private bool _isCanvasWidthChanged = false;
        private bool _isCanvasHeightChanged = false;

        private bool _isDisplayTypeChanged = false;
        private bool _isLyricsLayoutOrientationChanged = false;

        private bool _isPlayingLineChanged = false;
        private bool _isVisibleLinesBoundaryChanged = false;

        private bool _isDebugOverlayEnabledChanged = false;

        private bool _albumArtChanged = false;
        private bool _isCoverAcrylicEffectAmountChanged = false;

        private bool _isAlbumArtCornerRadiusChanged = true;

        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            _elapsedTime = args.Timing.ElapsedTime;

            if (_isPlaying)
            {
                TotalTime += _elapsedTime;
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

            if (_isDebugOverlayEnabledChanged)
            {
                if (_isDebugOverlayEnabled)
                {
                    _drawFrameStopwatch = Stopwatch.StartNew();
                }
                else
                {
                    _drawFrameStopwatch?.Stop();
                    _drawFrameStopwatch = null;
                }
                _isDebugOverlayEnabledChanged = false;
            }

            if (_isDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            if (_isCanvasWidthChanged)
            {
                if (_canvasWidth < 450)
                {
                    _lyricsLayoutOrientation = LyricsLayoutOrientation.Vertical;
                }
                else
                {
                    _lyricsLayoutOrientation = LyricsLayoutOrientation.Horizontal;
                }
            }

            if (_isCanvasWidthChanged || _isCanvasHeightChanged)
            {
                _isCoverAcrylicEffectAmountChanged = true;
            }

            if (_isDisplayTypeChanged || _isCanvasWidthChanged || _isCanvasHeightChanged)
            {
                bool jumpTo = !_isDisplayTypeChanged && (_isCanvasWidthChanged || _isCanvasHeightChanged);
                switch (_lyricsLayoutOrientation)
                {
                    case LyricsLayoutOrientation.Horizontal:
                        _albumArtSize = MathF.Min((_canvasHeight - _topMargin - _bottomMargin) * 8.5f / 16, (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2);
                        _albumArtSize = MathF.Max(0, _albumArtSize);
                        _albumArtYTransition.StartTransition((_canvasHeight - _albumArtSize * 1.05f - _titleTextFormat.FontSize - _artistTextFormat.FontSize) / 2, jumpTo);
                        _titleYTransition.StartTransition(_albumArtYTransition.TargetValue + _albumArtSize * 1.05f, jumpTo);
                        _lyricsYTransition.StartTransition(0, jumpTo);
                        switch (_displayType)
                        {
                            case LyricsDisplayType.AlbumArtOnly:
                                _lyricsOpacityTransition.StartTransition(0f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtXTransition.StartTransition(_canvasWidth / 2 - _albumArtSize / 2, jumpTo);
                                _titleXTransition.StartTransition(_albumArtXTransition.TargetValue, jumpTo);
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
                                _titleXTransition.StartTransition(_albumArtXTransition.TargetValue, jumpTo);
                                break;
                            default:
                                break;
                        }
                        break;
                    case LyricsLayoutOrientation.Vertical:
                        _albumArtSize = 64;
                        _lyricsXTransition.StartTransition(_leftMargin, jumpTo);
                        _albumArtXTransition.StartTransition(_leftMargin, jumpTo);
                        _titleXTransition.StartTransition(_leftMargin + _albumArtSize * 1.2f, jumpTo);
                        switch (_displayType)
                        {
                            case LyricsDisplayType.AlbumArtOnly:
                                _lyricsOpacityTransition.StartTransition(0f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtYTransition.StartTransition((_canvasHeight - _albumArtSize) / 2, jumpTo);
                                _titleYTransition.StartTransition(_albumArtYTransition.TargetValue, jumpTo);
                                break;
                            case LyricsDisplayType.LyricsOnly:
                                _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(0f, jumpTo);
                                _lyricsYTransition.StartTransition(0, jumpTo);
                                break;
                            case LyricsDisplayType.SplitView:
                                _albumArtYTransition.StartTransition(_topMargin, jumpTo);
                                _titleYTransition.StartTransition(_topMargin, jumpTo);
                                _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _lyricsYTransition.StartTransition(_albumArtSize, jumpTo);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            if (_isAlbumArtCornerRadiusChanged)
            {
                UpdateLastFgImageEffect(control);
                UpdateFgImageEffect(control);
                _isAlbumArtCornerRadiusChanged = false;
            }

            // 背景图切换计算
            // 将当前背景图放到 _lastAlbumArtSwBitmap 中 并设置不透明度为 1 
            // 将新的背景图放到 _albumArtSwBitmap 中 并设置不透明度为 0
            // 这样可以实现背景图的连贯渐变效果
            if (_albumArtChanged || _isCanvasHeightChanged || _isCanvasWidthChanged ||
                _lyricsBgBrightnessTransition.IsTransitioning ||
                _albumArtBgTransition.IsTransitioning)
            {
                // 必须先在此处重置动画
                if (_albumArtChanged)
                {
                    _albumArtBgTransition.Reset(0f);
                    _albumArtBgTransition.StartTransition(1f);
                }
                // 更新 last
                if (_albumArtChanged)
                {
                    if (_lastAlbumArtSwBitmap != null)
                    {
                        _lastAlbumArtCanvasBitmap?.Dispose();
                        _lastAlbumArtCanvasBitmap = null;
                        _lastAlbumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _lastAlbumArtSwBitmap);
                    }
                }
                UpdateLastFgImageEffect(control);
                UpdateLastBgImageEffect();
                // 更新 current
                if (_albumArtChanged)
                {
                    if (_albumArtSwBitmap != null)
                    {
                        _albumArtCanvasBitmap?.Dispose();
                        _albumArtCanvasBitmap = null;
                        _albumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _albumArtSwBitmap);
                    }
                }
                UpdateFgImageEffect(control);
                UpdateBgImageEffect();
                // 更新叠加的背景效果
                UpdateAlbumArtBgEffect(control);
            }

            if (_isCoverAcrylicEffectAmountChanged)
            {
                UpdateCoverAcrylicOverlay(control);
                UpdateAlbumArtBgEffect(control);

                _isCoverAcrylicEffectAmountChanged = false;
            }

            _albumArtChanged = false;

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

            _titleXTransition.Update(_elapsedTime);
            _titleYTransition.Update(_elapsedTime);
            _lyricsXTransition.Update(_elapsedTime);
            _lyricsYTransition.Update(_elapsedTime);
            _albumArtXTransition.Update(_elapsedTime);
            _albumArtYTransition.Update(_elapsedTime);
            _lyricsOpacityTransition.Update(_elapsedTime);
            _albumArtOpacityTransition.Update(_elapsedTime);
            _immersiveBgOpacityTransition.Update(_elapsedTime);
            _immersiveBgColorTransition.Update(_elapsedTime);
            _albumArtAccentColorTransition.Update(_elapsedTime);
            _albumArtBgTransition.Update(_elapsedTime);
            _lyricsBgBrightnessTransition.Update(_elapsedTime);
            _songInfoOpacityTransition.Update(_elapsedTime);
        }

        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            if (_isDockMode)
            {
                _lyricsTextFormat.FontSize = _lyricsDockFontSize;
            }
            else if (_isDesktopMode)
            {
                _lyricsTextFormat.FontSize = _lyricsDesktopFontSize;
            }
            else
            {
                _lyricsTextFormat.FontSize = _lyricsStandardFontSize;
            }

            float y = 0;

            // Init Positions
            for (int i = 0; i < _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.Count; i++)
            {
                var line = _lyricsDataArr[_langIndex].LyricsLines.ElementAtOrDefault(i);

                if (line == null)
                {
                    continue;
                }

                line.UpdateTextLayout(control, _lyricsTextFormat, _maxLyricsWidth, _canvasHeight, _isDockMode ? TextAlignmentType.Center : _lyricsAlignmentType);
                line.UpdateTextGeometry();
                line.Position = new Vector2(0, y);
                line.UpdateCenterPosition(_maxLyricsWidth, _isDockMode ? TextAlignmentType.Center : _lyricsAlignmentType);
                line.UpdateFontEffect(control, _isDesktopMode, _strokeFontColor, _lyricsFontStrokeWidth, _bgFontColor);

                if (line.CanvasTextLayout == null)
                {
                    continue;
                }

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
                LyricsLine? currentPlayingLine = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

                if (currentPlayingLine == null) return;

                var playingTextLayout = currentPlayingLine?.CanvasTextLayout;

                if (playingTextLayout == null) return;

                float? targetYScrollOffset = (float?)(-currentPlayingLine!.Position.Y + _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines[0].Position.Y - playingTextLayout.LayoutBounds.Height / 2);

                if (!targetYScrollOffset.HasValue) return;

                _canvasYScrollTransition.StartTransition(targetYScrollOffset.Value, !withAnimation);
            }

            _canvasYScrollTransition.Update(_elapsedTime);

            // Update visible line indices
            var lines = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines;
            if (lines == null || lines.Count == 0) return;

            float offset = _canvasYScrollTransition.Value + _canvasHeight / 2;
            int startVisibleLineIndex = FindFirstVisibleLine(lines, offset);
            int endVisibleLineIndex = FindLastVisibleLine(lines, offset, _canvasHeight);

            if (startVisibleLineIndex != -1 && endVisibleLineIndex == -1)
            {
                endVisibleLineIndex = endLineIndex;
            }

            _isVisibleLinesBoundaryChanged = _startVisibleLineIndex != startVisibleLineIndex || _endVisibleLineIndex != endVisibleLineIndex;

            _startVisibleLineIndex = startVisibleLineIndex;
            _endVisibleLineIndex = endVisibleLineIndex;
        }

        private int FindFirstVisibleLine(IList<LyricsLine> lines, float offset)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                var layout = line.CanvasTextLayout;
                if (layout == null) break;
                float value = offset + line.Position.Y + (float)layout.LayoutBounds.Height;
                if (value >= 0)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }
            return result;
        }

        private int FindLastVisibleLine(IList<LyricsLine> lines, float offset, float canvasHeight)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                var layout = line.CanvasTextLayout;
                if (layout == null) break;
                float value = offset + line.Position.Y + (float)layout.LayoutBounds.Height;
                if (value >= canvasHeight)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }
            return result;
        }

        private void UpdateColorConfig()
        {
            if (_isDesktopMode || _isDockMode)
            {
                ThemeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(_environmentalColor);
            }
            else
            {
                ThemeTypeSent = _lyricsBgTheme;
            }

            float brightness;
            Color grayedEnvironmentalColor = Colors.Transparent;

            bool isLight = ThemeTypeSent switch
            {
                ElementTheme.Default => Application.Current.RequestedTheme == ApplicationTheme.Light,
                ElementTheme.Light => true,
                ElementTheme.Dark => false,
                _ => false
            };

            if (isLight)
            {
                _adaptiveGrayedFontColor = _darkColor;
                brightness = 0.7f;
                grayedEnvironmentalColor = _lightColor;
                _albumArtAccentColorTransition.StartTransition(_albumArtLightAccentColor);
            }
            else
            {
                _adaptiveGrayedFontColor = _lightColor;
                brightness = 0.3f;
                grayedEnvironmentalColor = _darkColor;
                _albumArtAccentColorTransition.StartTransition(_albumArtDarkAccentColor);
            }

            _lyricsBgBrightnessTransition.StartTransition(brightness);

            if (_isDesktopMode || _isDockMode)
            {
                _adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(_environmentalColor);
            }
            else
            {
                if (isLight)
                {
                    _adaptiveColoredFontColor = _albumArtDarkAccentColor;
                }
                else
                {
                    _adaptiveColoredFontColor = _albumArtLightAccentColor;
                }
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

            _isLayoutChanged = true;
        }

        private void UpdateLinesProps()
        {
            var currentPlayingLine = _lyricsDataArr
                .ElementAtOrDefault(_langIndex)
                ?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null) return;

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(i);

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

        private void UpdateImmersiveBackgroundOpacity()
        {
            float targetOpacity;
            if (_isDesktopMode)
            {
                if (_isLyricsWindowLocked)
                {
                    targetOpacity = 0;
                }
                else
                {
                    if (_isMouseWithinWindow)
                    {
                        targetOpacity = 1f;
                    }
                    else
                    {
                        targetOpacity = 0f;
                    }
                }
            }
            else
            {
                targetOpacity = 1f;
            }
            _immersiveBgOpacityTransition.StartTransition(targetOpacity);
        }

        private void UpdateCoverAcrylicOverlay(ICanvasAnimatedControl control)
        {
            if (_coverAcrylicEffectAmount > 0)
            {
                var ret = NoiseOverlayHelper.GenerateNoiseBitmapBGRA((int)_canvasWidth, (int)_canvasHeight);
                _coverAcrylicNoiseCanvasBitmap?.Dispose();
                _coverAcrylicNoiseCanvasBitmap = null;
                _coverAcrylicNoiseCanvasBitmap = CanvasBitmap.CreateFromBytes(
                    control,
                    ret,
                    (int)_canvasWidth,
                    (int)_canvasHeight,
                   Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized
                );
            }
        }
    }
}
