using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.UI;
using static Vanara.PInvoke.Shell32;

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
                combinedDs.DrawImage(new Transform3DEffect
                {
                    Source = blurredLyrics,
                    TransformMatrix = _lyrics3DMatrix
                });
            }
            else
            {
                combinedDs.DrawImage(blurredLyrics);
            }

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawSongInfo(control, ds);

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
            if (_spectrumGeometry != null)
            {
                var gradientStops = new CanvasGradientStop[]
                 {
                    new() { Position = 0.0f, Color = Colors.Transparent },
                    new() { Position = 0.7f, Color = Colors.Transparent },
                    new() { Position = 1.0f, Color = _adaptiveColoredFontColor ?? _albumArtAccentColor1Transition.Value }
                 };

                using var gradientBrush = new CanvasLinearGradientBrush(ds, gradientStops);

                switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.SpectrumPlacement)
                {
                    case SpectrumPlacement.Top:
                        gradientBrush.StartPoint = new Vector2(0, (float)_canvasHeight);
                        gradientBrush.EndPoint = new Vector2(0, 0);
                        break;
                    case SpectrumPlacement.Bottom:
                        gradientBrush.StartPoint = new Vector2(0, 0);
                        gradientBrush.EndPoint = new Vector2(0, (float)_canvasHeight);
                        break;
                    default:
                        break;
                }


                // 使用渐变画刷填充
                ds.FillGeometry(_spectrumGeometry, gradientBrush);

                // 纯色
                //ds.FillGeometry(geometry, _adaptiveColoredFontColor ?? _albumArtAccentColor1Transition.Value);

                // 绘制轮廓线
                //var lineColor = Colors.SkyBlue;
                //float strokeWidth = 2f;
                //ds.DrawGeometry(geometry, _albumArtAccentColor4Transition.Value, strokeWidth);
            }
        }

        private void DrawFluidBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_fluidEffect != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsFluidOverlayEnabled)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _fluidEffect,
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

        private void DrawSongInfo(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_maxSongInfoWidth <= 0)
            {
                return;
            }

            DrawSingleSongInfo(control, ds, _lastTitleTextLayout, _lastArtistTextLayout, _lastAlbumTextLayout, 1 - _songInfoOpacityTransition.Value);
            DrawSingleSongInfo(control, ds, _titleTextLayout, _artistTextLayout, _albumTextLayout, _songInfoOpacityTransition.Value);
        }

        private void DrawSingleSongInfo(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasTextLayout? titleLayout, CanvasTextLayout? artistLayout, CanvasTextLayout? albumLayout, double opacity)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowTitle && titleLayout != null)
            {
                double y = _titleYTransition.Value;

                ds.DrawTextLayout(
                    titleLayout,
                    new Vector2((float)_titleXTransition.Value, (float)y),
                    _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));

                y += titleLayout.LayoutBounds.Height;

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowArtists && artistLayout != null)
                {
                    ds.DrawTextLayout(
                        artistLayout,
                        new Vector2((float)_titleXTransition.Value, (float)y),
                        _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));

                    y += artistLayout.LayoutBounds.Height;
                }

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowAlbum && albumLayout != null)
                {
                    ds.DrawTextLayout(
                        albumLayout,
                        new Vector2((float)_titleXTransition.Value, (float)y),
                        _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 64 * opacity)));
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

                var textLayout = line.OriginalCanvasTextLayout;
                if (textLayout == null) continue;

                double layoutWidth = (double)textLayout.LayoutBounds.Width;
                double layoutHeight = (double)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0) continue;

                double yOffset = line.YOffsetTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;

                //// 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition)
                    * Matrix3x2.CreateRotation((float)line.AngleTransition.Value,
                    currentPlayingLine.OriginalPosition.WithX(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.FanLyricsAngle < 0 ? (float)_maxLyricsWidth : 0))
                    * Matrix3x2.CreateTranslation((float)_lyricsX, (float)yOffset);

                using var combined = new CanvasCommandList(control);
                using var combinedDs = combined.CreateDrawingSession();

                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                using var backgroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor,
                    _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth, _bgFontColor);

                using var backgroundEffect = CanvasHelper.CreateBackgroundEffect(line, backgroundFontEffect, _lyricsOpacityTransition.Value);
                combinedDs.DrawImage(backgroundEffect);

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
                    if (line.PhoneticText != "" && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.PhoneticLyricsHighlightAmount != 0)
                    {
                        using var phoneticHighlightMask = CanvasHelper.CreatePhoneticHighlightMask(control, line);
                        using var foregroundPhoneticHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, phoneticHighlightMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.PhoneticLyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundPhoneticHighlightEffect);
                    }
                    if (line.TranslatedText != "" && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.TranslatedLyricsHighlightAmount != 0)
                    {
                        using var translatedHighlightMask = CanvasHelper.CreateTranslatedHighlightMask(control, line);
                        using var foregroundTranslatedHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, translatedHighlightMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.TranslatedLyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundTranslatedHighlightEffect);
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
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightAmount != 0)
                    {
                        var highlightEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightScope);
                        using var foregroundHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, highlightEffectMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightAmount / 100.0);
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
