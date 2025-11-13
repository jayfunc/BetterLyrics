using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private bool _isLayoutChanged = true;

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

        private bool _isSongTitleVisibilityChanged = false;
        private bool _isSongArtistVisibilityChanged = false;
        private bool _isSongAlbumVisibilityChanged = false;

        private bool _isSongTitleChanged = false;
        private bool _isSongArtistChanged = false;
        private bool _isSongAlbumChanged = false;

        private bool _isSongInfoFontSizeChanged = false;
        private bool _isSongInfoAlignmentTypeChanged = false;

        private bool _isAlbumArtSizeChanged = false;

        private bool _isSpectrumOverlayEnabledChanged = true;
        private bool _isFluidOverlayEnabledChanged = true;

        private bool _isLyrics3DMatrixChanged = true;

        private bool _isDeviceChanged = true;

        private bool _isLyricsXChanged = true;

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

            //if (IsScrolling)
            //{
            //    _scrollTime += ScrollDeltaTime;
            //    _elapsedTime = ScrollDeltaTime;
            //    TotalTime = _scrollTime;
            //    IsScrolling = false;
            //}

            //_effect?.Properties["iTime"] = Convert.ToSingle(TotalTime.TotalSeconds);

            if (_isDeviceChanged || _isFluidOverlayEnabledChanged)
            {
                if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsFluidOverlayEnabled)
                {
                    RecreateFluidEffect(control);
                }
                else
                {
                    DisposeFluidEffect();
                }

                _isFluidOverlayEnabledChanged = false;
            }

            if (_fluidEffect != null)
            {
                var effectTime = Convert.ToSingle(_fluidEffect.Properties["iTime"]);
                effectTime += Convert.ToSingle(_elapsedTime.TotalSeconds);
                _fluidEffect.Properties["iTime"] = effectTime;

                if (_albumArtAccentColor1Transition.IsTransitioning)
                {
                    _fluidEffect.Properties["color1"] = _albumArtAccentColor1Transition.Value.ToVector3RGB();
                }
                if (_albumArtAccentColor2Transition.IsTransitioning)
                {
                    _fluidEffect.Properties["color2"] = _albumArtAccentColor2Transition.Value.ToVector3RGB();
                }
                if (_albumArtAccentColor3Transition.IsTransitioning)
                {
                    _fluidEffect.Properties["color3"] = _albumArtAccentColor3Transition.Value.ToVector3RGB();
                }
                if (_albumArtAccentColor4Transition.IsTransitioning)
                {
                    _fluidEffect.Properties["color4"] = _albumArtAccentColor4Transition.Value.ToVector3RGB();
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

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverOverlaySpeed > 0)
            {
                _rotateAngle += _coverRotateBaseSpeed * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverOverlaySpeed / 100.0;
                _rotateAngle %= Math.PI * 2;
            }

            if (_isSpectrumOverlayEnabledChanged)
            {
                if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsSpectrumOverlayEnabled)
                {
                    _spectrumAnalyzer?.StartCapture();
                }
                else
                {
                    _spectrumAnalyzer?.StopCapture();
                }

                _isSpectrumOverlayEnabledChanged = false;
            }

            if (_spectrumAnalyzer?.IsCapturing == true)
            {
                _spectrumAnalyzer?.UpdateSmoothSpectrum();
            }

            //if (_isCanvasWidthChanged)
            //{
            //    if (_canvasWidth < 500)
            //    {
            //        _lyricsLayoutOrientation = LyricsLayoutOrientation.Vertical;
            //    }
            //    else
            //    {
            //        _lyricsLayoutOrientation = LyricsLayoutOrientation.Horizontal;
            //    }
            //}

            if (_isDeviceChanged || _isCanvasWidthChanged || _isCanvasHeightChanged)
            {
                UpdateSongInfoFontSize();

                _isCoverAcrylicEffectAmountChanged = true;
                _isLyrics3DMatrixChanged = true;

                _fluidEffect?.Properties["Width"] = (float)control.ConvertDipsToPixels((float)_canvasWidth, CanvasDpiRounding.Round);
                _fluidEffect?.Properties["Height"] = (float)control.ConvertDipsToPixels((float)_canvasHeight, CanvasDpiRounding.Round);

                _topMargin = _bottomMargin = _leftMargin = _middleMargin = _rightMargin = Math.Max(_canvasWidth, _canvasHeight) / 30.0;
            }

            if (_isDeviceChanged || _isDisplayTypeChanged || _isLyricsLayoutOrientationChanged || _isAlbumArtSizeChanged ||
                _isSongInfoFontSizeChanged || _isSongInfoAlignmentTypeChanged ||
                _isSongTitleVisibilityChanged || _isSongArtistVisibilityChanged || _isSongAlbumVisibilityChanged ||
                _isSongTitleChanged || _isSongArtistChanged || _isSongAlbumChanged ||
                _isCanvasWidthChanged || _isCanvasHeightChanged ||
                _isAlbumArtSizeChanged)
            {
                UpdateAlbumArtSize();
                UpdateMaxSongInfoWidth();

                UpdateSongTitle(control);
                UpdateSongArtist(control);
                UpdateSongAlbum(control);

                UpdateSongInfoHeight();

                bool jumpTo = !_isDisplayTypeChanged && !_isLyricsLayoutOrientationChanged &&
                    (_isCanvasWidthChanged || _isCanvasHeightChanged);

                switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
                {
                    case LyricsLayoutOrientation.Horizontal:
                        _albumArtYTransition.StartTransition((_canvasHeight - _albumArtSize - _songInfoHeight) / 2.0, jumpTo);
                        _titleYTransition.StartTransition(_albumArtYTransition.TargetValue + _albumArtSize * 1.05, jumpTo);
                        _lyricsYTransition.StartTransition(0, jumpTo);
                        switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
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
                                _lyricsX = _leftMargin;
                                break;
                            case LyricsDisplayType.SplitView:
                                _lyricsOpacityTransition.StartTransition(1f, jumpTo);
                                _albumArtOpacityTransition.StartTransition(1f, jumpTo);
                                _lyricsX = (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 + _leftMargin + _middleMargin;
                                _albumArtXTransition.StartTransition(_leftMargin + ((_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 - _albumArtSize) / 2.0, jumpTo);
                                _titleXTransition.StartTransition(_albumArtXTransition.TargetValue, jumpTo);
                                break;
                            default:
                                break;
                        }
                        break;
                    case LyricsLayoutOrientation.Vertical:
                        _lyricsX = _leftMargin;
                        _albumArtXTransition.StartTransition(_leftMargin, jumpTo);
                        _titleXTransition.StartTransition(_leftMargin + _albumArtSize * 1.2, jumpTo);
                        switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
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
                                _lyricsYTransition.StartTransition(_topMargin, jumpTo);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                _isDisplayTypeChanged = false;

                _isSongInfoFontSizeChanged = false;
                _isSongInfoAlignmentTypeChanged = false;

                _isSongTitleVisibilityChanged = false;
                _isSongArtistVisibilityChanged = false;
                _isSongAlbumVisibilityChanged = false;

                _isLyrics3DMatrixChanged = true;
                _isLyricsXChanged = true;
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

            // 背景图变动计算
            // 将当前背景图放到 _lastAlbumArtSwBitmap 中 并设置不透明度为 1 
            // 将新的背景图放到 _albumArtSwBitmap 中 并设置不透明度为 0
            // 这样可以实现背景图的连贯渐变效果
            if (_isDeviceChanged || _albumArtChanged || _isLyricsLayoutOrientationChanged || _isAlbumArtSizeChanged ||
                _isCanvasHeightChanged || _isCanvasWidthChanged ||
                _lyricsBgBrightnessTransition.IsTransitioning ||
                _albumArtBgTransition.IsTransitioning)
            {
                if (_isDeviceChanged || _albumArtChanged)
                {
                    // 必须先在此处重置动画
                    _albumArtBgTransition.Reset(0f);
                    _albumArtBgTransition.StartTransition(1f);

                    // 更新 last 和 current
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

            _isLyricsLayoutOrientationChanged = false;
            _isAlbumArtSizeChanged = false;

            if (_isDeviceChanged || _isAlbumArtBgOpacityChanged || _isAlbumArtBgBlurAmountChanged || _isCoverAcrylicEffectAmountChanged)
            {
                if (_isDeviceChanged || _isCoverAcrylicEffectAmountChanged)
                {
                    UpdateCoverAcrylicOverlay(control);
                }

                DisposeAlbumArtBgRenderTarget();
                UpdateAlbumArtBgEffect(control);
                _isAlbumArtBgEffectChanged = true;

                _isAlbumArtBgOpacityChanged = false;
                _isAlbumArtBgBlurAmountChanged = false;
                _isCoverAcrylicEffectAmountChanged = false;
            }

            _albumArtChanged = false;

            if (_isDeviceChanged || (!_isAlbumArtEffectChanged && _albumArtEffect != null))
            {
                UpdateAlbumArtRenderTarget(control);
                DisposeAlbumArtEffect();
            }

            if (_isDeviceChanged || (!_isAlbumArtBgEffectChanged && _albumArtBgEffect != null))
            {
                UpdateAlbumArtBgRenderTarget(control);
                DisposeAlbumArtBgEffect();
            }

            if (_isDeviceChanged || _isCanvasHeightChanged || _isCanvasWidthChanged || _isLyricsXChanged)
            {
                _maxLyricsWidth = Math.Max(_canvasWidth - _lyricsX - _rightMargin, 0);
                _isLayoutChanged = true;
                _isLyrics3DMatrixChanged = true;
                _isLyricsXChanged = false;
            }

            if (_isLyrics3DMatrixChanged)
            {
                UpdateLyrics3DMatrix();
                _isLyrics3DMatrixChanged = false;
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

            _lyricsYTransition.Update(_elapsedTime);

            _albumArtXTransition.Update(_elapsedTime);
            _albumArtYTransition.Update(_elapsedTime);

            _lyricsOpacityTransition.Update(_elapsedTime);
            _albumArtOpacityTransition.Update(_elapsedTime);

            _immersiveBgOpacityTransition.Update(_elapsedTime);
            _immersiveBgColorTransition.Update(_elapsedTime);

            _albumArtAccentColor1Transition.Update(_elapsedTime);
            _albumArtAccentColor2Transition.Update(_elapsedTime);
            _albumArtAccentColor3Transition.Update(_elapsedTime);
            _albumArtAccentColor4Transition.Update(_elapsedTime);

            _albumArtBgTransition.Update(_elapsedTime);
            _lyricsBgBrightnessTransition.Update(_elapsedTime);
            _songInfoOpacityTransition.Update(_elapsedTime);
            _canvasYScrollTransition.Update(_elapsedTime);

            _isDeviceChanged = false;
        }

        private string AutoSelectFontFamily(string text)
        {
            return LanguageHelper.IsCJK(text)
                    ? _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsCJKFontFamily
                    : _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsWesternFontFamily;
        }

        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.IsDynamicLyricsFontSize)
            {
                _originalLyricsFontSize = (int)Math.Clamp(Math.Min(_canvasHeight, _canvasWidth) / 15, 18, 96);
                _translatedLyricsFontSize = _phoneticLyricsFontSize = (int)(_originalLyricsFontSize * 2.0 / 3.0);
            }
            else
            {
                _phoneticLyricsFontSize = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.PhoneticLyricsFontSize;
                _originalLyricsFontSize = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.OriginalLyricsFontSize;
                _translatedLyricsFontSize = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.TranslatedLyricsFontSize;
            }

            _originalLyricsFontWeight = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontWeight;
            _titleTextFormat.FontWeight = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontWeight.ToFontWeight();
            _artistTextFormat.FontWeight = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontWeight.ToFontWeight();
            _albumTextFormat.FontWeight = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontWeight.ToFontWeight();

            if (SongInfo != null)
            {
                _titleTextFormat.FontFamily = AutoSelectFontFamily(SongInfo.Title);
                _artistTextFormat.FontFamily = AutoSelectFontFamily(SongInfo.Artist);
                _albumTextFormat.FontFamily = AutoSelectFontFamily(SongInfo.Album);
            }

            _canvasYScrollTransition.SetDuration(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollDuration / 1000.0);
            _canvasYScrollTransition.SetEasingType(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollEasingType);

            double y = 0;

            // Init Positions
            for (int i = 0; i < _currentLyricsData?.LyricsLines.Count; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);

                if (line == null)
                {
                    continue;
                }

                line.RecreateTextLayout(control,
                    _settingsService.AppSettings.TranslationSettings.IsChineseRomanizationEnabled || _settingsService.AppSettings.TranslationSettings.IsJapaneseRomanizationEnabled,
                    _settingsService.AppSettings.TranslationSettings.IsTranslationEnabled,
                    _phoneticLyricsFontSize, _originalLyricsFontSize, _translatedLyricsFontSize,
                    _originalLyricsFontWeight,
                    _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsCJKFontFamily, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsWesternFontFamily,
                    _maxLyricsWidth, _canvasHeight, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsAlignmentType);
                line.RecreateTextGeometry();

                // 设定注音文本布局坐标
                line.PhoneticPosition = new Vector2(0, (float)y);

                // Y += 注音文本布局高度
                if (line.PhoneticCanvasTextLayout != null)
                {
                    y += line.PhoneticCanvasTextLayout.LayoutBounds.Height;
                }

                // Y += 自定义 倍注音文本行高
                if (line.PhoneticCanvasTextLayout != null)
                {
                    y +=
                        (double)line.PhoneticCanvasTextLayout.LayoutBounds.Height
                        / line.PhoneticCanvasTextLayout.LineCount
                        * 0.1;
                }

                // 设定原文文本布局坐标
                line.OriginalPosition = new Vector2(0, (float)y);

                // Y += 原文文本布局高度
                if (line.OriginalCanvasTextLayout != null)
                {
                    y += (double)line.OriginalCanvasTextLayout.LayoutBounds.Height;
                }

                if (line.TranslatedCanvasTextLayout != null)
                {
                    // Y += 自定义 倍翻译文本行高
                    y +=
                        (double)line.TranslatedCanvasTextLayout.LayoutBounds.Height
                        / line.TranslatedCanvasTextLayout.LineCount
                        * 0.1;
                }

                // 设定翻译文本布局坐标
                line.TranslatedPosition = new Vector2(0, (float)y);

                // Y += 翻译文本布局高度
                if (line.TranslatedCanvasTextLayout != null)
                {
                    y += line.TranslatedCanvasTextLayout.LayoutBounds.Height;
                }

                // Y += 用户自定义倍数原文文本布局高度
                if (line.OriginalCanvasTextLayout != null)
                {
                    y += (double)line.OriginalCanvasTextLayout.LayoutBounds.Height
                        / line.OriginalCanvasTextLayout.LineCount
                        * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsLineSpacingFactor;
                }

                line.UpdateCenterPosition(_maxLyricsWidth, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsAlignmentType);

            }
        }

        private void UpdateCanvasTargetYScrollOffset()
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0) return;

            // Set _scrollOffsetY

            LyricsLine? currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null) return;

            var playingTextLayout = currentPlayingLine?.OriginalCanvasTextLayout;

            if (playingTextLayout == null) return;

            //double? targetYScrollOffset = -currentPlayingLine!.OriginalPosition.Y + _currentLyricsData?.LyricsLines[0].OriginalPosition.Y - playingTextLayout.LayoutBounds.Height / 2.0;
            double? targetYScrollOffset =
                -currentPlayingLine!.OriginalPosition.Y
                + _currentLyricsData?.LyricsLines[0].OriginalPosition.Y
                - (currentPlayingLine.TranslatedPosition.Y + (currentPlayingLine.TranslatedCanvasTextLayout?.LayoutBounds.Height ?? 0) - currentPlayingLine.PhoneticPosition.Y) / 2.0;

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
                var layout = line.OriginalCanvasTextLayout;
                if (layout == null) break;
                double value = offset + line.OriginalPosition.Y + (double)layout.LayoutBounds.Height;
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
                var layout = line.OriginalCanvasTextLayout;
                if (layout == null) break;
                double value = offset + line.OriginalPosition.Y + (double)layout.LayoutBounds.Height;
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
            if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
            {
                ThemeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(_environmentalColor);
            }
            else
            {
                ThemeTypeSent = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.LyricsBackgroundTheme;
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
                _albumArtAccentColor1Transition.StartTransition(_albumArtLightAccentColors.ElementAtOrDefault(0));
                _albumArtAccentColor2Transition.StartTransition(_albumArtLightAccentColors.ElementAtOrDefault(1));
                _albumArtAccentColor3Transition.StartTransition(_albumArtLightAccentColors.ElementAtOrDefault(2));
                _albumArtAccentColor4Transition.StartTransition(_albumArtLightAccentColors.ElementAtOrDefault(3));
            }
            else
            {
                _adaptiveGrayedFontColor = _lightColor;
                brightness = 0.3f;
                _grayedEnvironmentalColor = _darkColor;
                _albumArtAccentColor1Transition.StartTransition(_albumArtDarkAccentColors.ElementAtOrDefault(0));
                _albumArtAccentColor2Transition.StartTransition(_albumArtDarkAccentColors.ElementAtOrDefault(1));
                _albumArtAccentColor3Transition.StartTransition(_albumArtDarkAccentColors.ElementAtOrDefault(2));
                _albumArtAccentColor4Transition.StartTransition(_albumArtDarkAccentColors.ElementAtOrDefault(3));
            }

            _lyricsBgBrightnessTransition.StartTransition(brightness);

            if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
            {
                _adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(_environmentalColor);
            }
            else
            {
                if (isLight)
                {
                    _adaptiveColoredFontColor = _albumArtDarkAccentColors.ElementAtOrDefault(0);
                }
                else
                {
                    _adaptiveColoredFontColor = _albumArtLightAccentColors.ElementAtOrDefault(0);
                }
            }

            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _bgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _bgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _bgFontColor = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    break;
            }

            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _fgFontColor = _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _fgFontColor = _adaptiveColoredFontColor ?? _adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    _fgFontColor = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    break;
            }

            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    _strokeFontColor = _grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    _strokeFontColor = _environmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    _strokeFontColor = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsCustomStrokeFontColor;
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
                    double distanceFromPlayingLine = Math.Abs(line.OriginalPosition.Y - currentPlayingLine.OriginalPosition.Y);
                    double distanceFactor = Math.Clamp(distanceFromPlayingLine / (_canvasHeight / 2), 0, 1);

                    line.AngleTransition.StartTransition(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsFanLyricsEnabled
                            ? Math.PI
                                * (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.FanLyricsAngle / 180.0)
                                * distanceFactor
                                * (i > _playingLineIndex ? 1 : -1)
                            : 0
                    );

                    line.BlurAmountTransition.StartTransition(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsBlurAmount * distanceFactor);
                    line.ScaleTransition.StartTransition(_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale));
                    line.OpacityTransition.StartTransition(
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontOpacity / 100.0 -
                        distanceFactor * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontOpacity / 100.0 *
                        (1 - _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsVerticalEdgeOpacity / 100.0));
                    line.HighlightOpacityTransition.StartTransition(i == _playingLineIndex ? 1f : 0f);

                    double yScrollDuration;
                    double yScrollDelay;

                    if (lineCountDelta < 0)
                    {
                        yScrollDuration =
                            _canvasYScrollTransition.DurationSeconds +
                            distanceFactor * (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollTopDuration / 1000.0 - _canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollTopDelay / 1000.0;
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
                            distanceFactor * (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollBottomDuration / 1000.0 - _canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsScrollBottomDelay / 1000.0;
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

        private void UpdateCoverAcrylicOverlay(ICanvasAnimatedControl control)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverAcrylicEffectAmount > 0)
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

        private void UpdateSongInfoFontSize()
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.IsAutoSongInfoFontSize)
            {
                _titleTextFormat.FontSize = (int)Math.Clamp(Math.Min(_canvasHeight, _canvasWidth) / 20, 8, 72);
            }
            else
            {
                _titleTextFormat.FontSize = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.SongInfoFontSize;
            }

            _artistTextFormat.FontSize = (int)(_titleTextFormat.FontSize * 0.8);
            _albumTextFormat.FontSize = (int)(_titleTextFormat.FontSize * 0.8);

            _isSongInfoFontSizeChanged = true;
        }

        private void UpdateLyrics3DMatrix()
        {
            if (!_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled) return;

            Vector3 center = new(
                (float)(_lyricsX + _maxLyricsWidth / 2),
                (float)(_lyricsYTransition.Value + _canvasHeight / 2),
                0);

            float rotationX = (float)(Math.PI * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Lyrics3DXAngle / 180.0);
            float rotationY = (float)(Math.PI * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Lyrics3DYAngle / 180.0);
            float rotationZ = (float)(Math.PI * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Lyrics3DZAngle / 180.0);

            Matrix4x4 rotation =
                Matrix4x4.CreateRotationX(rotationX) *
                Matrix4x4.CreateRotationY(rotationY) *
                Matrix4x4.CreateRotationZ(rotationZ);
            Matrix4x4 perspective = Matrix4x4.Identity;
            perspective.M34 = 1.0f / _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Lyrics3DDepth;

            // 组合变换：
            // 1. 将中心移到原点
            // 2. 旋转
            // 3. 应用透视
            // 4. 将中心移回原位
            _lyrics3DMatrix =
                Matrix4x4.CreateTranslation(-center) *
                rotation *
                perspective *
                Matrix4x4.CreateTranslation(center);
        }

        private void UpdateMaxSongInfoWidth()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    _maxSongInfoWidth = _albumArtSize;
                    break;
                case LyricsLayoutOrientation.Vertical:
                    _maxSongInfoWidth = _canvasWidth - _leftMargin - _albumArtSize - _rightMargin;
                    break;
            }
        }

        private void UpdateSongTitle(ICanvasAnimatedControl control)
        {
            _lastTitleTextLayout?.Dispose();
            _lastTitleTextLayout = null;
            _lastTitleTextLayout = new(
                control, _lastSongTitle ?? string.Empty,
                _titleTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _titleTextLayout?.Dispose();
            _titleTextLayout = null;
            _titleTextLayout = new(
                control, _songTitle ?? string.Empty,
                _titleTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _isSongTitleChanged = false;
        }

        private void UpdateSongArtist(ICanvasAnimatedControl control)
        {
            _lastArtistTextLayout?.Dispose();
            _lastArtistTextLayout = null;
            _lastArtistTextLayout = new(
                control, _lastSongArtist ?? string.Empty,
                _artistTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _artistTextLayout?.Dispose();
            _artistTextLayout = null;
            _artistTextLayout = new(
                control, _songArtist ?? string.Empty,
                _artistTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _isSongArtistChanged = false;
        }

        private void UpdateSongAlbum(ICanvasAnimatedControl control)
        {
            _lastAlbumTextLayout?.Dispose();
            _lastAlbumTextLayout = null;
            _lastAlbumTextLayout = new(
                control, _lastSongAlbum ?? string.Empty,
                _albumTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _albumTextLayout?.Dispose();
            _albumTextLayout = null;
            _albumTextLayout = new(
                control, _songAlbum ?? string.Empty,
                _albumTextFormat, (float)_maxSongInfoWidth, (float)_canvasHeight
            );

            _isSongAlbumChanged = false;
        }

        private void UpdateSongInfoHeight()
        {
            _songInfoHeight = 0;
            if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowTitle)
            {
                _songInfoHeight += (int)(_titleTextLayout?.LayoutBounds.Height ?? 0);
                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowArtists)
                {
                    _songInfoHeight += (int)(_artistTextLayout?.LayoutBounds.Height ?? 0);
                }
                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowAlbum)
                {
                    _songInfoHeight += (int)(_albumTextLayout?.LayoutBounds.Height ?? 0);
                }
            }
        }

        private void UpdateAlbumArtSize()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.AutoAlbumArtSize)
                    {
                        _albumArtSize = Math.Min((_canvasHeight - _topMargin - _bottomMargin) * 8.5 / 16.0,
                            (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0);
                        _albumArtSize = Math.Max(0, _albumArtSize);
                    }
                    else
                    {
                        _albumArtSize = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.AlbumArtSize;
                    }
                    break;
                case LyricsLayoutOrientation.Vertical:
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.AutoAlbumArtSize)
                    {
                        _albumArtSize = Math.Min((_canvasHeight - _topMargin - _bottomMargin) * 3.0 / 16.0,
                            (_canvasWidth - _leftMargin - _middleMargin - _rightMargin) * 4.0 / 16.0);
                        _albumArtSize = Math.Max(0, _albumArtSize);
                    }
                    else
                    {
                        _albumArtSize = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.AlbumArtSize;
                    }
                    break;
            }
        }
    }
}
