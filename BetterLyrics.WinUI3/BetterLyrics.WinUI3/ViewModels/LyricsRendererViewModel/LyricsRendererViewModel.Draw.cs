using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI;
using Hqub.Lastfm;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            // Blurred lyrics layer
            using var blurredLyrics = new CanvasCommandList(control);
            using (var blurredLyricsDs = blurredLyrics.CreateDrawingSession())
            {
                DrawBlurredLyrics(control, blurredLyricsDs);
            }

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsPureColorOverlayEnabled)
            {
                if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
                {
                    FillBackground(control, combinedDs, _immersiveBgColorTransition.Value, 0f,
                        _immersiveBgOpacityTransition.Value * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100f);
                }
                else
                {
                    FillBackground(control, combinedDs, _albumArtAccentColor1Transition.Value, 0f,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100.0);
                }
            }
            DrawAlbumArtBackground(control, combinedDs);
            DrawFluidBackground(control, combinedDs);
            DrawSpectrum(control, combinedDs);

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled)
            {
                using var perspectiveEffect = new Transform3DEffect
                {
                    Source = blurredLyrics,
                    TransformMatrix = _lyrics3DMatrix
                };

                combinedDs.DrawImage(perspectiveEffect);
            }
            else
            {
                combinedDs.DrawImage(blurredLyrics);
            }

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawTitleAndArtist(control, ds);

            if (_isDebugOverlayEnabled)
            {
                _drawFrameCount++;

                var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

                if (currentPlayingLine != null)
                {
                    GetLinePlayingProgress(
                        _playingLineIndex,
                        out int charStartIndex,
                        out int charLength,
                        out double charProgress
                    );

                    ds.DrawText(
                        $"[DEBUG]\n" +
                            $"Canvas size: {_canvasWidth}x{_canvasHeight}\n" +
                            $"FPS (Draw): {_displayedDrawFrameCount}\n" +
                            $"Playing line: {_playingLineIndex}\n" +
                            $"Syllable start idx: {charStartIndex}\n" +
                            $"Syllable len: {charLength}\n" +
                            $"Syllable prog: {charProgress}\n" +
                            $"Visible lines: [{_startVisibleLineIndex}, {_endVisibleLineIndex}]\n" +
                            $"Total line count: {GetMaxLyricsLineIndexBoundaries().Item2 + 1}\n" +
                            $"Cur time: {TotalTime + _positionOffset}\n" +
                            $"Song duration: {TimeSpan.FromMilliseconds(SongInfo?.DurationMs ?? 0)}\n" +
                            $"Y offset: {_canvasYScrollTransition.Value}",
                        new Vector2(10, 40),
                        ThemeTypeSent == Microsoft.UI.Xaml.ElementTheme.Light ? Colors.Black : Colors.White,
                        _debugTextFormat
                    );
                }

                if (_drawFrameStopwatch?.Elapsed.TotalSeconds >= 1.0)
                {
                    _displayedDrawFrameCount = _drawFrameCount;
                    _drawFrameStopwatch?.Restart();
                    _drawFrameCount = 0;
                }
            }
        }

        public void DrawSpectrum(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_spectrumAnalyzer != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsSpectrumOverlayEnabled)
            {
                var points = new Vector2[_spectrumAnalyzer.BarCount];
                float pointSpacing = 0;

                if (_spectrumAnalyzer.BarCount > 1)
                {
                    pointSpacing = (float)_canvasWidth / (_spectrumAnalyzer.BarCount - 1);
                }

                for (int i = 0; i < _spectrumAnalyzer.BarCount; i++)
                {
                    float x = i * pointSpacing;
                    float amplitude = _spectrumAnalyzer.SmoothSpectrum.Average() * 10 + _spectrumAnalyzer.SmoothSpectrum[i] * 0.5f;
                    float y = (float)_canvasHeight - amplitude;
                    points[i] = new Vector2(x, y);
                }

                // 用于填充的闭合路径
                using var pathBuilder = new CanvasPathBuilder(ds);
                pathBuilder.BeginFigure(points[0]);

                if (_spectrumAnalyzer.BarCount > 2)
                {
                    for (int i = 0; i < _spectrumAnalyzer.BarCount - 1; i++)
                    {
                        Vector2 p0 = points[Math.Max(i - 1, 0)];
                        Vector2 p1 = points[i];
                        Vector2 p2 = points[i + 1];
                        Vector2 p3 = points[Math.Min(i + 2, _spectrumAnalyzer.BarCount - 1)];

                        Vector2 cp1 = p1 + (p2 - p0) / 6.0f;
                        Vector2 cp2 = p2 - (p3 - p1) / 6.0f;

                        pathBuilder.AddCubicBezier(cp1, cp2, p2);
                    }
                }
                else
                {
                    pathBuilder.AddLine(points[1]);
                }

                pathBuilder.AddLine(new Vector2(points[_spectrumAnalyzer.BarCount - 1].X, (float)_canvasHeight));
                pathBuilder.AddLine(new Vector2(points[0].X, (float)_canvasHeight));
                pathBuilder.EndFigure(CanvasFigureLoop.Closed);

                using var geometry = CanvasGeometry.CreatePath(pathBuilder);
                var gradientStops = new CanvasGradientStop[]
                {
                    new() { Position = 0.0f, Color = _albumArtAccentColor1Transition.Value },
                    new() { Position = 1.0f, Color = Colors.Transparent }
                };

                using var gradientBrush = new CanvasLinearGradientBrush(ds, gradientStops);
                gradientBrush.StartPoint = new Vector2((float)_canvasWidth / 2, (float)_canvasHeight);
                gradientBrush.EndPoint = new Vector2((float)_canvasWidth / 2, points.Select(p => p.Y).Min());

                // 使用渐变画刷填充
                ds.FillGeometry(geometry, gradientBrush);

                // 绘制轮廓线
                // var lineColor = Colors.SkyBlue;
                // float strokeWidth = 2f;
                // session.DrawGeometry(geometry, lineColor, strokeWidth);

            }
        }

        private void DrawFluidBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_effect != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsFluidOverlayEnabled)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _effect,
                    Opacity = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.FluidOverlayOpacity / 100f
                });
            }
        }

        private void DrawBackgroundImgae(OpacityEffect effect, CanvasDrawingSession ds, CanvasBitmap canvasBitmap)
        {
            double imageWidth = (double)canvasBitmap.Size.Width;
            double imageHeight = (double)canvasBitmap.Size.Height;

            double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
            double scaleFactor = targetSize / Math.Min(imageWidth, imageHeight);

            double x = _canvasWidth / 2 - imageWidth * scaleFactor / 2;
            double y = _canvasHeight / 2 - imageHeight * scaleFactor / 2;

            ds.DrawImage(effect, new Vector2((float)x, (float)y));
        }

        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsCoverOverlayEnabled)
            {
                ds.Transform = Matrix3x2.CreateRotation((float)_rotateAngle, control.Size.ToVector2() * 0.5f);

                if (_albumArtBgEffect != null)
                {
                    ds.DrawImage(_albumArtBgEffect);
                }
                else if (_albumArtBgRenderTarget != null)
                {
                    double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
                    float offsetX = (float)(_canvasWidth - targetSize) / 2;
                    float offsetY = (float)(_canvasHeight - targetSize) / 2;

                    ds.DrawImage(_albumArtBgRenderTarget, new Vector2(offsetX, offsetY));
                }

                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_isAlbumArtEffectChanged && _albumArtEffect != null)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _albumArtEffect,
                    Opacity = (float)_albumArtOpacityTransition.Value
                }, new Vector2((float)_albumArtXTransition.Value, (float)_albumArtYTransition.Value));
            }
            else if (!_isAlbumArtEffectChanged && _albumArtRenderTarget != null)
            {
                // 这里给一个相反的偏移以恢复位置
                ds.DrawImage(new OpacityEffect
                {
                    Source = _albumArtRenderTarget,
                    Opacity = (float)_albumArtOpacityTransition.Value
                }, new Vector2((float)_albumArtXTransition.Value, (float)_albumArtYTransition.Value) -
                    control.Size.ToVector2() / 2 + new Vector2((float)_albumArtSize, (float)_albumArtSize) / 2);
            }
        }

        private void DrawTitleAndArtist(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_lastSongTitle != null || _lastSongArtist != null)
            {
                DrawSingleTitleAndArtist(control, ds, _lastSongTitle, _lastSongArtist, 1 - _songInfoOpacityTransition.Value);
            }
            if (_songTitle != null || _songArtist != null)
            {
                DrawSingleTitleAndArtist(control, ds, _songTitle, _songArtist, _songInfoOpacityTransition.Value);
            }
        }

        private void DrawSingleTitleAndArtist(ICanvasAnimatedControl control, CanvasDrawingSession ds, string? title, string? artist, double opacity)
        {
            var maxWidth = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation switch
            {
                LyricsLayoutOrientation.Horizontal => _albumArtSize,
                LyricsLayoutOrientation.Vertical => _canvasWidth - _leftMargin - _albumArtSize - _rightMargin,
                _ => 0f
            };
            if (maxWidth <= 0)
            {
                return;
            }

            using CanvasTextLayout titleLayout = new(
                control, title ?? string.Empty,
                _titleTextFormat, (float)maxWidth, (float)_canvasHeight
            );
            using CanvasTextLayout artistLayout = new(
                control, artist ?? string.Empty,
                _artistTextFormat, (float)maxWidth, (float)_canvasHeight
            );

            if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowTitle)
            {
                ds.DrawTextLayout(
                    titleLayout,
                    new Vector2((float)_titleXTransition.Value, (float)_titleYTransition.Value),
                    _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowArtists)
                {
                    ds.DrawTextLayout(
                        artistLayout,
                        new Vector2((float)_titleXTransition.Value, (float)(_titleYTransition.Value + titleLayout.LayoutBounds.Height)),
                        _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));
                }
            }
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null)
            {
                return;
            }

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;

                var textLayout = line.CanvasTextLayout;
                if (textLayout == null) continue;

                double layoutWidth = (double)textLayout.LayoutBounds.Width;
                double layoutHeight = (double)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0) continue;

                double yOffset = line.YOffsetTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;

                //// 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition)
                    * Matrix3x2.CreateRotation((float)line.AngleTransition.Value,
                    currentPlayingLine.Position.WithX(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.FanLyricsAngle < 0 ? (float)_maxLyricsWidth : 0))
                    * Matrix3x2.CreateTranslation((float)_lyricsXTransition.Value, (float)yOffset);

                using var combined = new CanvasCommandList(control);
                using var combinedDs = combined.CreateDrawingSession();

                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                using var backgroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor,
                    _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth, _bgFontColor);
                using var backgroundEffect = CanvasHelper.CreateBackgroundEffect(line, backgroundFontEffect, _lyricsOpacityTransition.Value);
                combinedDs.DrawImage(backgroundEffect);

                //if (i == _playingLineIndex)
                if (line.HighlightOpacityTransition.Value != 0)
                {
                    GetLinePlayingProgress(i, out int charStartIndex, out int charLength, out double charProgress);

                    using var charMask = CanvasHelper.CreateCharMask(control, line, charStartIndex, charLength, charProgress);
                    using var lineStartToCharMask = CanvasHelper.CreateLineStartToCharMask(control, line, charStartIndex, charLength, charProgress,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsLineFadeEnabled);
                    using var lineMask = CanvasHelper.CreateLineMask(control, line);

                    using var foregroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth, _fgFontColor);

                    using var effectLayer = new CanvasCommandList(control);
                    using var effectLayerDs = effectLayer.CreateDrawingSession();
                    if (line.OriginalText != line.DisplayedText && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsTranslationHighlightAmount != 0)
                    {
                        using var translationHighlightMask = CanvasHelper.CreateTranslationHighlightMask(control, line);
                        using var foregroundTranslationHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, translationHighlightMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsTranslationHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundTranslationHighlightEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsShadowEnabled)
                    {
                        var shadowEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsShadowScope);
                        using var foregroundShadowEffect = CanvasHelper.CreateForegroundShadowEffect(foregroundFontEffect, shadowEffectMask,
                            _albumArtAccentColor1Transition.Value, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsShadowAmount);
                        effectLayerDs.DrawImage(foregroundShadowEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled)
                    {
                        var blurEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectScope);
                        using var foregroundBlurEffect = CanvasHelper.CreateForegroundBlurEffect(foregroundFontEffect, blurEffectMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectAmount);
                        effectLayerDs.DrawImage(foregroundBlurEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsHighlightAmount != 0)
                    {
                        var highlightEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsHighlightScope);
                        using var foregroundHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, highlightEffectMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundHighlightEffect);
                    }

                    combinedDs.DrawImage(new OpacityEffect
                    {
                        Source = effectLayer,
                        Opacity = (float)(line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value),
                    });

                    if (i == _playingLineIndex)
                    {
                        if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled)
                        {
                            ds.DrawImage(new DisplacementMapEffect
                            {
                                Source = combined,
                                Displacement = lineStartToCharMask,
                                XChannelSelect = EffectChannelSelect.Red,
                                YChannelSelect = EffectChannelSelect.Alpha,
                                Amount = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsFloatAmount,
                            });
                        }
                        else
                        {
                            ds.DrawImage(combined);
                        }
                    }
                    else
                    {
                        ds.DrawImage(combined);
                    }
                }
                else
                {
                    ds.DrawImage(combined);
                }

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, Color color, double radius, double opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                (float)radius,
                (float)radius,
                color.WithAlpha((byte)(opacity * 255))
            );
        }

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasLinearGradientBrush brush, double radius, double opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                (float)radius,
                (float)radius,
                brush
            );
        }

        private CanvasLinearGradientBrush CreateVerticalFillBrush(
            ICanvasAnimatedControl control,
            List<(double position, Color color)> stops,
            double startY,
            double height
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(x => new CanvasGradientStop
            {
                Position = (float)x.position,
                Color = x.color,
            }).ToArray())
            {
                StartPoint = new Vector2(0, (float)startY),
                EndPoint = new Vector2(0, (float)(startY + height)),
            };
        }
    }
}
