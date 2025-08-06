using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
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

            if (_isDockMode)
            {
                FillBackground(control, combinedDs, _immersiveBgColorTransition.Value, 0f, _immersiveBgOpacityTransition.Value * _albumArtBgOpacity / 100f);
            }
            else if (_isDesktopMode)
            {
                FillBackground(control, combinedDs, _immersiveBgColorTransition.Value, 0f, _immersiveBgOpacityTransition.Value * _albumArtBgOpacity / 100f);
            }
            else
            {
                FillBackground(control, combinedDs, _albumArtAccentColorTransition.Value, 0f, _albumArtBgOpacity / 100f);
                DrawAlbumArtBackground(control, combinedDs);
            }

            combinedDs.DrawImage(blurredLyrics);

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawTitleAndArtist(control, ds);

            if (_isDebugOverlayEnabled)
            {
                _drawFrameCount++;

                var currentPlayingLine = _lyricsDataArr
                    .ElementAtOrDefault(_langIndex)
                    ?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

                if (currentPlayingLine != null)
                {
                    GetLinePlayingProgress(
                        _playingLineIndex,
                        out int charStartIndex,
                        out int charLength,
                        out float charProgress
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
                            $"Lang size: {_lyricsDataArr.Count}\n" +
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

        private void DrawBackgroundImgae(OpacityEffect effect, CanvasDrawingSession ds, CanvasBitmap canvasBitmap)
        {
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float targetSize = MathF.Sqrt(MathF.Pow(_canvasWidth, 2) + MathF.Pow(_canvasHeight, 2));
            float scaleFactor = targetSize / MathF.Min(imageWidth, imageHeight);

            float x = _canvasWidth / 2 - imageWidth * scaleFactor / 2;
            float y = _canvasHeight / 2 - imageHeight * scaleFactor / 2;

            ds.DrawImage(effect, new Vector2(x, y));
        }

        private void DrawForegroundImgae(OpacityEffect effect, CanvasDrawingSession ds)
        {
            ds.DrawImage(effect, new Vector2(_albumArtXTransition.Value, _albumArtYTransition.Value));
        }

        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_albumArtBgEffect == null)
            {
                return;
            }

            ds.Transform = Matrix3x2.CreateRotation(_rotateAngle, control.Size.ToVector2() * 0.5f);
            ds.DrawImage(_albumArtBgEffect);
            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            using var albumArt = new CanvasCommandList(control.Device);
            using var albumArtDs = albumArt.CreateDrawingSession();

            if (_lastFgImageEffect != null && !_lastFgImageEffect.IsDisposed() && _lastAlbumArtCanvasBitmap != null)
            {
                DrawForegroundImgae(_lastFgImageEffect, albumArtDs);
            }
            if (_fgImageEffect != null && !_fgImageEffect.IsDisposed() && _albumArtCanvasBitmap != null)
            {
                DrawForegroundImgae(_fgImageEffect, albumArtDs);
            }

            using var opacity = new CanvasCommandList(control.Device);
            using var opacityDs = opacity.CreateDrawingSession();
            opacityDs.DrawImage(new GaussianBlurEffect
            {
                Source = albumArt,
                BlurAmount = 12f,
                Optimization = EffectOptimization.Speed,
            });
            opacityDs.DrawImage(albumArt);

            ds.DrawImage(new OpacityEffect
            {
                Source = opacity,
                Opacity = _albumArtOpacityTransition.Value
            });
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

        private void DrawSingleTitleAndArtist(ICanvasAnimatedControl control, CanvasDrawingSession ds, string? title, string? artist, float opacity)
        {
            var maxWidth = _lyricsLayoutOrientation switch
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
                _titleTextFormat, maxWidth, _canvasHeight
            );
            using CanvasTextLayout artistLayout = new(
                control, artist ?? string.Empty,
                _artistTextFormat, maxWidth, _canvasHeight
            );
            ds.DrawTextLayout(
                titleLayout,
                new Vector2(_titleXTransition.Value, _titleYTransition.Value),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));
            ds.DrawTextLayout(
                artistLayout,
                new Vector2(_titleXTransition.Value, _titleYTransition.Value + (float)titleLayout.LayoutBounds.Height),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _lyricsDataArr
                .ElementAtOrDefault(_langIndex)
                ?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null)
            {
                return;
            }

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;

                var textLayout = line.CanvasTextLayout;
                if (textLayout == null) continue;

                float layoutWidth = (float)textLayout.LayoutBounds.Width;
                float layoutHeight = (float)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0) continue;

                float yOffset = _canvasYScrollTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;

                // 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale(line.ScaleTransition.Value, line.CenterPosition)
                    * Matrix3x2.CreateRotation(line.AngleTransition.Value, currentPlayingLine.Position)
                    * Matrix3x2.CreateTranslation(_lyricsXTransition.Value, yOffset);

                if (line.BackgroundFontEffect == null || line.ForegroundFontEffect == null) continue;

                using var combined = new CanvasCommandList(control.Device);
                using var combinedDs = combined.CreateDrawingSession();

                // Mock gradient blurred lyrics layer
                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                // Current line will not be blurred
                combinedDs.DrawImage(
                    new OpacityEffect
                    {
                        Source = new GaussianBlurEffect
                        {
                            Source = line.BackgroundFontEffect,
                            BlurAmount = line.BlurAmountTransition.Value,
                            BorderMode = EffectBorderMode.Soft,
                            Optimization = EffectOptimization.Speed,
                        },
                        Opacity = line.OpacityTransition.Value * _lyricsOpacityTransition.Value,
                    }
                );

                if (line.HighlightOpacityTransition.Value != 0)
                {
                    // 再叠加高亮行歌词层（前景歌词层）
                    using var mask = new CanvasCommandList(control.Device);
                    using var maskDs = mask.CreateDrawingSession();

                    using var highlightMask = new CanvasCommandList(control.Device);
                    using var highlightMaskDs = highlightMask.CreateDrawingSession();

                    if (i == _playingLineIndex)
                    {
                        GetLinePlayingProgress(
                            i,
                            out int charStartIndex,
                            out int charLength,
                            out float charProgress
                        );
                        var regions = textLayout.GetCharacterRegions(0, charStartIndex);
                        var highlightRegion = textLayout
                            .GetCharacterRegions(charStartIndex, charLength)
                            .FirstOrDefault();
                        if (regions.Length > 0)
                        {
                            // Draw the mask for the current line
                            for (int j = 0; j < regions.Length; j++)
                            {
                                var region = regions[j];
                                var rect = new Rect(
                                    region.LayoutBounds.X,
                                    region.LayoutBounds.Y + line.Position.Y,
                                    region.LayoutBounds.Width,
                                    region.LayoutBounds.Height
                                );
                                maskDs.FillRectangle(rect, Color.FromArgb(255, 128, 128, 128));
                            }
                        }

                        float highlightTotalWidth = (float)highlightRegion.LayoutBounds.Width;
                        // Draw the highlight for the current character
                        float highlightWidth = highlightTotalWidth * charProgress;

                        float fadingWidth = (float)highlightRegion.LayoutBounds.Height / 2;

                        // Rects
                        var highlightRect = new Rect(
                            highlightRegion.LayoutBounds.X,
                            highlightRegion.LayoutBounds.Y + line.Position.Y,
                            highlightWidth,
                            highlightRegion.LayoutBounds.Height
                        );

                        var fadeInRect = new Rect(
                            highlightRect.Right - fadingWidth,
                            highlightRegion.LayoutBounds.Y + line.Position.Y,
                            fadingWidth,
                            highlightRegion.LayoutBounds.Height
                        );
                        var fadeOutRect = new Rect(
                            highlightRect.Right,
                            highlightRegion.LayoutBounds.Y + line.Position.Y,
                            fadingWidth,
                            highlightRegion.LayoutBounds.Height
                        );

                        // Brushes
                        using var fadeInBrush = CreateHorizontalFillBrush(
                            control,
                            [(0f, 0f), (1f, 1f)],
                            (float)highlightRect.Right - fadingWidth,
                            fadingWidth
                        );
                        using var fadeOutBrush = CreateHorizontalFillBrush(
                            control,
                            [(0f, 1f), (1f, 0f)],
                            (float)highlightRect.Right,
                            fadingWidth
                        );

                        maskDs.FillRectangle(highlightRect, Color.FromArgb(255, 128, 128, 128));
                        maskDs.FillRectangle(fadeOutRect, fadeOutBrush);

                        highlightMaskDs.FillRectangle(fadeInRect, fadeInBrush);
                        highlightMaskDs.FillRectangle(fadeOutRect, fadeOutBrush);
                    }
                    else
                    {
                        //float height = 0f;
                        var regions = textLayout.GetCharacterRegions(0, line.OriginalText.Length);
                        if (regions.Length > 0)
                        {
                            //height = (float)regions[^1].LayoutBounds.Bottom - (float)regions[0].LayoutBounds.Top;

                            for (int j = 0; j < regions.Length; j++)
                            {
                                var region = regions[j];
                                var rect = new Rect(
                                    region.LayoutBounds.X,
                                    region.LayoutBounds.Y + line.Position.Y,
                                    region.LayoutBounds.Width,
                                    region.LayoutBounds.Height
                                );
                                maskDs.FillRectangle(rect, Colors.White);
                            }
                        }

                        //maskDs.FillRectangle(
                        //    new Rect(
                        //        textLayout.LayoutBounds.X,
                        //        line.Position.Y,
                        //        textLayout.LayoutBounds.Width,
                        //        height
                        //    ),
                        //    Colors.White
                        //);
                    }

                    using var opacityEffect = new OpacityEffect
                    {
                        Source = new BlendEffect
                        {
                            Background = _isLyricsGlowEffectEnabled
                                    ? new GaussianBlurEffect
                                    {
                                        Source = new AlphaMaskEffect
                                        {
                                            Source = line.ForegroundFontEffect,
                                            AlphaMask = _lyricsGlowEffectScope switch
                                            {
                                                LineRenderingType.CurrentChar => highlightMask,
                                                LineRenderingType.LineStartToCurrentChar => mask,
                                                LineRenderingType.CurrentLine => line.ForegroundFontEffect,
                                                _ => mask,
                                            },
                                        },
                                        BlurAmount = _lyricsGlowEffectAmount,
                                        Optimization = EffectOptimization.Speed,
                                    }
                                    : new CanvasCommandList(control.Device),
                            Foreground = new AlphaMaskEffect
                            {
                                Source = line.ForegroundFontEffect,
                                AlphaMask = _lyricsHighlightScope switch
                                {
                                    LineRenderingType.CurrentChar => highlightMask,
                                    LineRenderingType.LineStartToCurrentChar => mask,
                                    LineRenderingType.CurrentLine => line.ForegroundFontEffect,
                                    _ => mask,
                                },
                            },
                        },
                        Opacity = line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value,
                    };

                    combinedDs.DrawImage(opacityEffect);

                    if (i == _playingLineIndex)
                    {
                        if (_isLyricsFloatAnimationEnabled)
                        {
                            ds.DrawImage(new DisplacementMapEffect
                            {
                                Source = combined,
                                Displacement = mask,
                                XChannelSelect = EffectChannelSelect.Red,
                                YChannelSelect = EffectChannelSelect.Alpha,
                                Amount = 1f,
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

                line.DisposeFontEffects();
                line.DisposeTextGeometry();
            }
        }

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, Color color, float radius, float opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                radius,
                radius,
                color.WithAlpha((byte)(opacity * 255))
            );
        }

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasLinearGradientBrush brush, float radius, float opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                radius,
                radius,
                brush
            );
        }

        private CanvasLinearGradientBrush CreateHorizontalFillBrush(
            ICanvasAnimatedControl control,
            List<(float position, float opacity)> stops,
            float startX,
            float width
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(stops => new CanvasGradientStop
            {
                Position = stops.position,
                Color = Color.FromArgb((byte)(stops.opacity * 255), 128, 128, 128),
            }).ToArray())
            {
                StartPoint = new Vector2(startX, 0),
                EndPoint = new Vector2(startX + width, 0),
            };
        }

        private CanvasLinearGradientBrush CreateVerticalFillBrush(
            ICanvasAnimatedControl control,
            List<(float position, Color color)> stops,
            float startY,
            float height
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(x => new CanvasGradientStop
            {
                Position = x.position,
                Color = x.color,
            }).ToArray())
            {
                StartPoint = new Vector2(0, startY),
                EndPoint = new Vector2(0, startY + height),
            };
        }
    }
}
