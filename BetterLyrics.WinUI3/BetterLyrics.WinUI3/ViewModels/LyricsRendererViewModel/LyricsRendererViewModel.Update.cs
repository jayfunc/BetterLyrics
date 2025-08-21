using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private bool _isCanvasWidthChanged = false;
        private bool _isCanvasHeightChanged = false;

        private bool _isDisplayTypeChanged = false;
        private bool _isLyricsLayoutOrientationChanged = false;

        private bool _isPlayingLineChanged = false;
        private bool _isVisibleLinesBoundaryChanged = false;

        private bool _isDebugOverlayEnabledChanged = true;

        private bool _albumArtChanged = false;
        private bool _isCoverAcrylicEffectAmountChanged = false;

        private bool _isAlbumArtCornerRadiusChanged = true;
        private bool _isAlbumArtShadowAmountChanged = false;

        private bool _isAlbumArtBgOpacityChanged = false;
        private bool _isAlbumArtBgBlurAmountChanged = false;

        private bool _isAlbumArtBgEffectChanged = false;
        private bool _isAlbumArtEffectChanged = false;

        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            _elapsedTime = args.Timing.ElapsedTime;

            if (IsPlaying)
            {
                TotalTime += _elapsedTime;
                _totalPlayingTime += _elapsedTime;
                if (_isLastFMTrackEnabled && !_isLastFMTracked && SongInfo?.Duration != null && SongInfo.Duration > 0 && _totalPlayingTime.TotalSeconds >= SongInfo.Duration * 0.5)
                {
                    _isLastFMTracked = true;
                    _lastFMService.TrackAsync(SongInfo);
                }
            }

            // 检测播放行变更
            var playingLineIndex = GetCurrentPlayingLineIndex();
            _isPlayingLineChanged = _playingLineIndex != playingLineIndex;
            _playingLineIndex = playingLineIndex;

            // 检测画布宽度变更
            _isCanvasWidthChanged = _canvasWidth != control.Size.Width;
            _canvasWidth = control.Size.Width;

            // 检测画布高度变更
            _isCanvasHeightChanged = _canvasHeight != control.Size.Height;
            _canvasHeight = control.Size.Height;

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

            _rotateAngle += _coverRotateBaseSpeed * _settingsService.AppSettings.LyricsBackgroundSettings.CoverOverlaySpeed / 100.0;
            _rotateAngle %= Math.PI * 2;

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
                        _albumArtSize = Math.Min((_canvasHeight - _topMargin - _bottomMargin) * 8.5 / 16.0, (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0);
                        _albumArtSize = Math.Max(0, _albumArtSize);
                        _albumArtYTransition.StartTransition((_canvasHeight - _albumArtSize * 1.05 - _titleTextFormat.FontSize - _artistTextFormat.FontSize) / 2.0, jumpTo);
                        _titleYTransition.StartTransition(_albumArtYTransition.TargetValue + _albumArtSize * 1.05, jumpTo);
                        _lyricsYTransition.StartTransition(0, jumpTo);
                        switch (_liveStatesService.LiveStates.CurrentLyricsDisplayType)
                        {
                            case LyricsDisplayType.AlbumArtOnly:
                                _lyricsOpacityTransition.StartTransition(0f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtXTransition.StartTransition(_canvasWidth / 2.0 - _albumArtSize / 2.0, jumpTo);
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
                                _lyricsXTransition.StartTransition((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 + _leftMargin + _middleMargin, jumpTo);
                                _albumArtXTransition.StartTransition(_leftMargin + ((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 - _albumArtSize) / 2.0, jumpTo);
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
                        _titleXTransition.StartTransition(_leftMargin + _albumArtSize * 1.2, jumpTo);
                        switch (_liveStatesService.LiveStates.CurrentLyricsDisplayType)
                        {
                            case LyricsDisplayType.AlbumArtOnly:
                                _lyricsOpacityTransition.StartTransition(0f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtYTransition.StartTransition((_canvasHeight - _albumArtSize) / 2.0, jumpTo);
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
                _isDisplayTypeChanged = false;
            }

            // 先重置这两个的变化状态
            _isAlbumArtEffectChanged = false;
            _isAlbumArtBgEffectChanged = false;

            if (_isAlbumArtCornerRadiusChanged || _isAlbumArtShadowAmountChanged)
            {
                DisposeAlbumArtRenderTarget();
                UpdateAlbumArtEffect(control);
                _isAlbumArtEffectChanged = true;
                if (_isAlbumArtCornerRadiusChanged)
                {
                    _isAlbumArtCornerRadiusChanged = false;
                }
                if (_isAlbumArtShadowAmountChanged)
                {
                    _isAlbumArtShadowAmountChanged = false;
                }
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
                // 更新 last 和 current
                if (_albumArtChanged)
                {
                    if (_lastAlbumArtSwBitmap != null)
                    {
                        _lastAlbumArtCanvasBitmap?.Dispose();
                        _lastAlbumArtCanvasBitmap = null;
                        _lastAlbumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _lastAlbumArtSwBitmap);
                    }
                    if (_albumArtSwBitmap != null)
                    {
                        _albumArtCanvasBitmap?.Dispose();
                        _albumArtCanvasBitmap = null;
                        _albumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _albumArtSwBitmap);
                    }
                }
                // 更新叠加的背景效果
                DisposeAlbumArtRenderTarget();
                UpdateAlbumArtEffect(control);
                _isAlbumArtEffectChanged = true;

                DisposeAlbumArtBgRenderTarget();
                UpdateAlbumArtBgEffect(control);
                _isAlbumArtBgEffectChanged = true;
            }

            if (_isCoverAcrylicEffectAmountChanged)
            {
                UpdateCoverAcrylicOverlay(control);

                DisposeAlbumArtBgRenderTarget();
                UpdateAlbumArtBgEffect(control);
                _isAlbumArtBgEffectChanged = true;

                _isCoverAcrylicEffectAmountChanged = false;
            }

            if (_isAlbumArtBgOpacityChanged)
            {
                DisposeAlbumArtBgRenderTarget();
                UpdateAlbumArtBgEffect(control);
                _isAlbumArtBgEffectChanged = true;

                _isAlbumArtBgOpacityChanged = false;
            }

            if (_isAlbumArtBgBlurAmountChanged)
            {
                DisposeAlbumArtBgRenderTarget();
                UpdateAlbumArtBgEffect(control);
                _isAlbumArtBgEffectChanged = true;

                _isAlbumArtBgBlurAmountChanged = false;
            }

            _albumArtChanged = false;

            if (!_isAlbumArtEffectChanged && _albumArtEffect != null)
            {
                UpdateAlbumArtRenderTarget(control);
                DisposeAlbumArtEffect();
            }

            if (!_isAlbumArtBgEffectChanged && _albumArtBgEffect != null)
            {
                UpdateAlbumArtBgRenderTarget(control);
                DisposeAlbumArtBgEffect();
            }

            if (_isCanvasHeightChanged || _isCanvasWidthChanged || _lyricsXTransition.IsTransitioning)
            {
                _maxLyricsWidth = _canvasWidth - _lyricsXTransition.Value - _rightMargin;
                _maxLyricsWidth = Math.Max(_maxLyricsWidth, 0);
                _isLayoutChanged = true;
            }

            if (_isLayoutChanged)
            {
                ReLayout(control);
            }

            if (_isLayoutChanged || _isPlayingLineChanged)
            {
                UpdateCanvasTargetYScrollOffset();
                _canvasYScrollTransition.StartTransition(_canvasTargetYScrollOffset, _isLayoutChanged);
            }

            UpdateVisibleLinesBoundary();

            UpdateVisibleLinesProps(control);

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
            _canvasYScrollTransition.Update(_elapsedTime);
        }

        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            _lyricsTextFormat.FontSize = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFontSize;
            _lyricsTextFormat.FontWeight = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFontWeight.ToFontWeight();
            _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFontFamily;

            _canvasYScrollTransition.SetDuration(_liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollDuration / 1000.0);
            _canvasYScrollTransition.SetEasingType(_liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollEasingType);

            double y = 0;

            // Init Positions
            for (int i = 0; i < _currentLyricsData?.LyricsLines.Count; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);

                if (line == null)
                {
                    continue;
                }

                line.Position = new Vector2(0, (float)y);
                line.RecreateTextLayout(control, _lyricsTextFormat, _maxLyricsWidth, _canvasHeight, _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsAlignmentType);
                line.UpdateCenterPosition(_maxLyricsWidth, _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsAlignmentType);

                line.RecreateTextGeometry();

                y +=
                    (double)line.CanvasTextLayout!.LayoutBounds.Height
                    / line.CanvasTextLayout.LineCount
                    * (line.CanvasTextLayout.LineCount + _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsLineSpacingFactor);
            }
        }

        private void UpdateCanvasTargetYScrollOffset()
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0) return;

            // Set _scrollOffsetY

            LyricsLine? currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null) return;

            var playingTextLayout = currentPlayingLine?.CanvasTextLayout;

            if (playingTextLayout == null) return;

            double? targetYScrollOffset = -currentPlayingLine!.Position.Y + _currentLyricsData?.LyricsLines[0].Position.Y - playingTextLayout.LayoutBounds.Height / 2.0;

            if (!targetYScrollOffset.HasValue) return;

            _canvasTargetYScrollOffset = targetYScrollOffset.Value;
        }

        private void UpdateVisibleLinesBoundary()
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            var lines = _currentLyricsData?.LyricsLines;
            if (lines == null || lines.Count == 0) return;

            double offset = _canvasYScrollTransition.Value + _canvasHeight / 2;
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

        private int FindFirstVisibleLine(IList<LyricsLine> lines, double offset)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                var layout = line.CanvasTextLayout;
                if (layout == null) break;
                double value = offset + line.Position.Y + (double)layout.LayoutBounds.Height;
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

        private int FindLastVisibleLine(IList<LyricsLine> lines, double offset, double canvasHeight)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                var layout = line.CanvasTextLayout;
                if (layout == null) break;
                double value = offset + line.Position.Y + (double)layout.LayoutBounds.Height;
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
            if (_liveStatesService.LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode ||
                _liveStatesService.LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode)
            {
                ThemeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(_environmentalColor);
            }
            else
            {
                ThemeTypeSent = _settingsService.AppSettings.LyricsBackgroundSettings.LyricsBackgroundTheme;
            }

            double brightness;

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
                _grayedEnvironmentalColor = _lightColor;
                _albumArtAccentColorTransition.StartTransition(_albumArtLightAccentColor);
            }
            else
            {
                _adaptiveGrayedFontColor = _lightColor;
                brightness = 0.3f;
                _grayedEnvironmentalColor = _darkColor;
                _albumArtAccentColorTransition.StartTransition(_albumArtDarkAccentColor);
            }

            _lyricsBgBrightnessTransition.StartTransition(brightness);

            if (_liveStatesService.LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode ||
                _liveStatesService.LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
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

            switch (_liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _bgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _bgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _bgFontColor = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    break;
            }

            switch (_liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _fgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _fgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _fgFontColor = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    break;
            }

            switch (_liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _strokeFontColor = _grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _strokeFontColor = _environmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    _strokeFontColor = _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsCustomStrokeFontColor;
                    break;
                default:
                    break;
            }

            _isLayoutChanged = true;
        }

        private void UpdateVisibleLinesProps(ICanvasAnimatedControl control)
        {
            var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null) return;

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex + 1; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);

                if (line == null) continue;

                if (_isLayoutChanged || _isPlayingLineChanged)
                {
                    int lineCountDelta = i - _playingLineIndex;
                    int absoluteLineCountDelta = Math.Abs(lineCountDelta);
                    double distanceFromPlayingLine = Math.Abs(line.Position.Y - currentPlayingLine.Position.Y);
                    double distanceFactor = Math.Clamp(distanceFromPlayingLine / (_canvasHeight / 2), 0, 1);

                    line.AngleTransition.StartTransition(_liveStatesService.LiveStates.CurrentLyricsEffectSettings.IsFanLyricsEnabled
                            ? Math.PI
                                * (30.0 / 180.0)
                                * distanceFactor
                                * (i > _playingLineIndex ? 1 : -1)
                            : 0
                    );

                    line.BlurAmountTransition.StartTransition(_liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsBlurAmount * distanceFactor);
                    line.ScaleTransition.StartTransition(_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale));
                    line.OpacityTransition.StartTransition(
                        _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsBgFontOpacity / 100.0 -
                        distanceFactor * _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsBgFontOpacity / 100.0 *
                        (1 - _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsVerticalEdgeOpacity / 100.0));
                    line.HighlightOpacityTransition.StartTransition(i == _playingLineIndex ? 1f : 0f);

                    double yScrollDuration;
                    double yScrollDelay;

                    if (lineCountDelta < 0)
                    {
                        yScrollDuration =
                            _canvasYScrollTransition.DurationSeconds +
                            distanceFactor * (_liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollTopDuration / 1000.0 - _canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollTopDelay / 1000.0;
                    }
                    else if (lineCountDelta == 0)
                    {
                        yScrollDuration = _canvasYScrollTransition.DurationSeconds;
                        yScrollDelay = 0;
                    }
                    else
                    {
                        yScrollDuration =
                            _canvasYScrollTransition.DurationSeconds +
                            distanceFactor * (_liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollBottomDuration / 1000.0 - _canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsScrollBottomDelay / 1000.0;
                    }

                    line.YOffsetTransition.SetEasingType(_canvasYScrollTransition.EasingType ?? EasingType.Linear);
                    line.YOffsetTransition.SetDuration(yScrollDuration);
                    line.YOffsetTransition.SetDelay(yScrollDelay);
                    line.YOffsetTransition.StartTransition(_canvasTargetYScrollOffset, _isLayoutChanged);
                }

                line.AngleTransition.Update(_elapsedTime);
                line.ScaleTransition.Update(_elapsedTime);
                line.BlurAmountTransition.Update(_elapsedTime);
                line.OpacityTransition.Update(_elapsedTime);
                line.HighlightOpacityTransition.Update(_elapsedTime);
                line.YOffsetTransition.Update(_elapsedTime);
            }
        }

        private void UpdateImmersiveBackgroundOpacity()
        {
            double targetOpacity;
            if (_liveStatesService.LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
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
            if (_settingsService.AppSettings.LyricsBackgroundSettings.CoverAcrylicEffectAmount > 0)
            {
                var ret = ImageHelper.GenerateNoiseBGRA((int)_canvasWidth, (int)_canvasHeight);
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

        private void UpdateTimelineSyncThreshold()
        {
            var current = _mediaSessionsService.GetCurrentMediaSourceProviderInfo();
            _timelineSyncThreshold = current?.TimelineSyncThreshold ?? 0;
        }

        private void UpdatePositionOffset()
        {
            var current = _mediaSessionsService.GetCurrentMediaSourceProviderInfo();
            _positionOffset = TimeSpan.FromMilliseconds(current?.PositionOffset ?? 0);
        }

        private void UpdateIsLastFMTrackEnabled()
        {
            var current = _mediaSessionsService.GetCurrentMediaSourceProviderInfo();
            _isLastFMTrackEnabled = current?.IsLastFMTrackEnabled ?? false;
        }
    }
}
