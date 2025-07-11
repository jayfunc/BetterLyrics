using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.ViewModels
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

            if (_lastAlbumArtSwBitmap != null && _lastAlbumArtCanvasBitmap == null)
            {
                _lastAlbumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _lastAlbumArtSwBitmap);
            }
            if (_albumArtSwBitmap != null && _albumArtCanvasBitmap == null)
            {
                _albumArtCanvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, _albumArtSwBitmap);
            }

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            if (_isDockMode)
            {
                DrawImmersiveBackground(control, combinedDs, 0f);
            }
            else if (_isDesktopMode)
            {
                DrawImmersiveBackground(control, combinedDs, 12f);
            }
            else
            {
                DrawAlbumArtBackground(control, combinedDs);
            }

            combinedDs.DrawImage(blurredLyrics);

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawTitleAndArtist(control, ds);

            if (_isDebugOverlayEnabled)
            {
                var currentPlayingLine = _multiLangLyrics
                    .SafeGet(_langIndex)
                    ?.SafeGet(_playingLineIndex);

                if (currentPlayingLine != null)
                {
                    GetLinePlayingProgress(
                        currentPlayingLine,
                        out int charStartIndex,
                        out int charLength,
                        out float charProgress
                    );

                    ds.DrawText(
                        $"[DEBUG]\n" +
                            $"Cur playing {_playingLineIndex}, char start idx {charStartIndex}, length {charLength}, prog {charProgress}\n" +
                            $"Visible lines [{_startVisibleLineIndex}, {_endVisibleLineIndex}]\n" +
                            $"Cur time {_totalTime + _positionOffset}\n" +
                            $"Lang size {_multiLangLyrics.Count}\n" +
                            $"Song duration {TimeSpan.FromMilliseconds(SongInfo?.DurationMs ?? 0)}",
                        new Vector2(10, 10),
                        ThemeTypeSent == Microsoft.UI.Xaml.ElementTheme.Light ? Colors.Black : Colors.White
                    );

                    //for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
                    //{
                    //    LyricsLine? line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);
                    //    if (line != null)
                    //    {
                    //        ds.DrawText(
                    //            $"[{i}] {line.OriginalText} {line.HighlightOpacityTransition.Value}",
                    //            new Vector2(10, 30 + (i - _startVisibleLineIndex) * 20),
                    //            ThemeTypeSent == ElementTheme.Light ? Colors.Black : Colors.White
                    //        );
                    //    }
                    //}
                }
            }
        }

        private void DrawBackgroundImgae(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasBitmap canvasBitmap, float opacity)
        {
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float targetSize = MathF.Sqrt(MathF.Pow(_canvasWidth, 2) + MathF.Pow(_canvasHeight, 2)) * 1.4f;
            float scaleFactor = targetSize / MathF.Min(imageWidth, imageHeight);

            float x = _canvasWidth / 2 - imageWidth * scaleFactor / 2;
            float y = _canvasHeight / 2 - imageHeight * scaleFactor / 2;

            // Source: https://zhuanlan.zhihu.com/p/37178216
            float bright = _lyricsBgBrightnessTransition.Value / 1f * 2f; // 明度参数，范围在0.0f到2.0f之间

            float whiteX = Math.Min(2 - bright, 1);
            float whiteY = 1f;
            float blackX = Math.Max(1 - bright, 0);
            float blackY = 0f;

            ds.DrawImage(new OpacityEffect
            {
                Source = new BrightnessEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    WhitePoint = new Vector2(whiteX, whiteY),
                    BlackPoint = new Vector2(blackX, blackY),
                },
                Opacity = opacity,
            }, new Vector2(x, y)
            );
        }

        private void DrawForegroundImgae(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasBitmap canvasBitmap, float opacity)
        {
            if (opacity == 0) return;

            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float scaleFactor = _albumArtSize / Math.Min(imageWidth, imageHeight);
            if (scaleFactor < 0.01f) return;

            float cornerRadius = _albumArtCornerRadius / 100f * _albumArtSize / 2;

            using var cornerRadiusMask = new CanvasCommandList(control.Device);
            using var cornerRadiusMaskDs = cornerRadiusMask.CreateDrawingSession();
            cornerRadiusMaskDs.FillRoundedRectangle(
                new Rect(0, 0, imageWidth * scaleFactor, imageHeight * scaleFactor),
                cornerRadius, cornerRadius, Colors.White
            );

            ds.DrawImage(new OpacityEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    AlphaMask = cornerRadiusMask,
                },
                Opacity = opacity,
            }, new Vector2(_albumArtXTransition.Value, _albumArtY)
            );
        }

        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            ds.Transform = Matrix3x2.CreateRotation(_rotateAngle, control.Size.ToVector2() * 0.5f);

            using var overlappedCovers = new CanvasCommandList(control.Device);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_lastAlbumArtCanvasBitmap != null)
            {
                DrawBackgroundImgae(control, overlappedCoversDs, _lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
            }
            if (_albumArtCanvasBitmap != null)
            {
                DrawBackgroundImgae(control, overlappedCoversDs, _albumArtCanvasBitmap, _albumArtBgTransition.Value);
            }

            using var coverOverlayEffect = new OpacityEffect
            {
                Opacity = _albumArtBgOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = _albumArtBgBlurAmount,
                    Source = overlappedCovers,
                    BorderMode = EffectBorderMode.Soft,
                    Optimization = EffectOptimization.Quality,
                },
            };
            ds.DrawImage(coverOverlayEffect);

            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            using var albumArt = new CanvasCommandList(control.Device);
            using var albumArtDs = albumArt.CreateDrawingSession();
            if (_albumArtCanvasBitmap != null)
            {
                DrawForegroundImgae(control, albumArtDs, _albumArtCanvasBitmap, _albumArtBgTransition.Value);
            }
            if (_lastAlbumArtCanvasBitmap != null)
            {
                DrawForegroundImgae(control, albumArtDs, _lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
            }

            using var opacity = new CanvasCommandList(control.Device);
            using var opacityDs = opacity.CreateDrawingSession();
            opacityDs.DrawImage(new GaussianBlurEffect
            {
                Source = albumArt,
                BlurAmount = 12f,
                Optimization = EffectOptimization.Quality,
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
            CanvasTextLayout titleLayout = new(
                control, title ?? string.Empty,
                _titleTextFormat, _albumArtSize, _canvasHeight
            );
            CanvasTextLayout artistLayout = new(
                control, artist ?? string.Empty,
                _artistTextFormat, _albumArtSize, _canvasHeight
            );
            ds.DrawTextLayout(
                titleLayout,
                new Vector2(_albumArtXTransition.Value, _titleY),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));
            ds.DrawTextLayout(
                artistLayout,
                new Vector2(_albumArtXTransition.Value, _titleY + (float)titleLayout.LayoutBounds.Height),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _multiLangLyrics
                .SafeGet(_langIndex)
                ?.SafeGet(_playingLineIndex);

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

                var textLayout = line.CanvasTextLayout;

                if (textLayout == null)
                {
                    continue;
                }

                var position = new Vector2(line.Position.X, line.Position.Y);

                float layoutWidth = (float)textLayout.LayoutBounds.Width;
                float layoutHeight = (float)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0)
                {
                    continue;
                }

                float centerX = position.X;
                float centerY = position.Y + layoutHeight / 2;

                switch (_lyricsAlignmentType)
                {
                    case TextAlignmentType.Left:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case TextAlignmentType.Center:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += _maxLyricsWidth / 2;
                        break;
                    case TextAlignmentType.Right:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += _maxLyricsWidth;
                        break;
                    default:
                        break;
                }

                // 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale(line.ScaleTransition.Value, new Vector2(centerX, centerY))
                    * Matrix3x2.CreateRotation(line.AngleTransition.Value, currentPlayingLine.Position)
                    * Matrix3x2.CreateTranslation(_lyricsXTransition.Value, _canvasYScrollTransition.Value + _canvasHeight / 2);

                // Create the background lyrics line with stroke and fill
                using var bgLyrics = new CanvasCommandList(control.Device);
                using var bgLyricsDs = bgLyrics.CreateDrawingSession();

                // Create the foreground lyrics line with stroke and fill
                using var fgLyrics = new CanvasCommandList(control.Device);
                using var fgLyricsDs = fgLyrics.CreateDrawingSession();

                // 创建文字几何体
                using (var textGeometry = CanvasGeometry.CreateText(textLayout))
                {
                    if (_isDesktopMode)
                    {
                        bgLyricsDs.DrawGeometry(textGeometry, position, _strokeFontColor, _lyricsFontStrokeWidth); // 背景描边
                        fgLyricsDs.DrawGeometry(textGeometry, position, _strokeFontColor, _lyricsFontStrokeWidth); // 前景描边
                    }

                    bgLyricsDs.FillGeometry(textGeometry, position, _bgFontColor); // 背景填充
                    fgLyricsDs.FillGeometry(textGeometry, position, _fgFontColor); // 前景填充
                }

                // Mock gradient blurred lyrics layer
                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                // Current line will not be blurred
                ds.DrawImage(
                    new GaussianBlurEffect
                    {
                        Source = new OpacityEffect { Source = bgLyrics, Opacity = line.OpacityTransition.Value * _lyricsOpacityTransition.Value },
                        BlurAmount = line.BlurAmountTransition.Value,
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
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
                            line,
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
                                    region.LayoutBounds.Y + position.Y,
                                    region.LayoutBounds.Width,
                                    region.LayoutBounds.Height
                                );
                                maskDs.FillRectangle(rect, Colors.Black);
                            }
                        }

                        float highlightTotalWidth = (float)highlightRegion.LayoutBounds.Width;
                        // Draw the highlight for the current character
                        float highlightWidth = highlightTotalWidth * charProgress;

                        float fadingWidth = (float)highlightRegion.LayoutBounds.Height / 2;

                        // Rects
                        var highlightRect = new Rect(
                            highlightRegion.LayoutBounds.X,
                            highlightRegion.LayoutBounds.Y + position.Y,
                            highlightWidth,
                            highlightRegion.LayoutBounds.Height
                        );

                        var fadeInRect = new Rect(
                            highlightRect.Right - fadingWidth,
                            highlightRegion.LayoutBounds.Y + position.Y,
                            fadingWidth,
                            highlightRegion.LayoutBounds.Height
                        );
                        var fadeOutRect = new Rect(
                            highlightRect.Right,
                            highlightRegion.LayoutBounds.Y + position.Y,
                            fadingWidth,
                            highlightRegion.LayoutBounds.Height
                        );

                        // Brushes
                        using var fadeInBrush = GetHorizontalFillBrush(
                            control,
                            [(0f, 0f), (1f, 1f)],
                            (float)highlightRect.Right - fadingWidth,
                            fadingWidth
                        );
                        using var fadeOutBrush = GetHorizontalFillBrush(
                            control,
                            [(0f, 1f), (1f, 0f)],
                            (float)highlightRect.Right,
                            fadingWidth
                        );

                        maskDs.FillRectangle(highlightRect, Colors.White);
                        maskDs.FillRectangle(fadeOutRect, fadeOutBrush);

                        highlightMaskDs.FillRectangle(fadeInRect, fadeInBrush);
                        highlightMaskDs.FillRectangle(fadeOutRect, fadeOutBrush);
                    }
                    else
                    {
                        float height = 0f;
                        var regions = textLayout.GetCharacterRegions(0, string.Join("", line.CharTimings.Select(x => x.Text)).Length);
                        if (regions.Length > 0)
                        {
                            height = (float)regions[^1].LayoutBounds.Bottom - (float)regions[0].LayoutBounds.Top;
                        }

                        maskDs.FillRectangle(
                            new Rect(
                                textLayout.LayoutBounds.X,
                                position.Y,
                                textLayout.LayoutBounds.Width,
                                height
                            ),
                            Colors.White
                        );
                    }

                    ds.DrawImage(
                        new OpacityEffect
                        {
                            Source = new BlendEffect
                            {
                                Background = _isLyricsGlowEffectEnabled
                                    ? new GaussianBlurEffect
                                    {
                                        Source = new AlphaMaskEffect
                                        {
                                            Source = fgLyrics,
                                            AlphaMask = _lyricsGlowEffectScope switch
                                            {
                                                LineRenderingType.UntilCurrentChar => mask,
                                                LineRenderingType.CurrentCharOnly => highlightMask,
                                                _ => mask,
                                            },
                                        },
                                        BlurAmount = _lyricsGlowEffectAmount,
                                        Optimization = EffectOptimization.Quality,
                                    }
                                    : new CanvasCommandList(control.Device),
                                Foreground = new AlphaMaskEffect
                                {
                                    Source = fgLyrics,
                                    AlphaMask = mask,
                                },
                            },
                            Opacity = line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value,
                        }
                    );
                }

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawImmersiveBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, float radius)
        {
            CanvasCommandList list = new(control.Device);
            using var listDs = list.CreateDrawingSession();
            listDs.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                radius,
                radius,
                _immersiveBgTransition.Value
            );
            ds.DrawImage(new OpacityEffect
            {
                Source = list,
                Opacity = _immersiveBgOpacityTransition.Value
            });
        }

        private CanvasLinearGradientBrush GetHorizontalFillBrush(
            ICanvasAnimatedControl control,
            List<(float position, float opacity)> stops,
            float startX,
            float width
        )
        {
            return new CanvasLinearGradientBrush(
                control,
                stops
                    .Select(stops => new CanvasGradientStop
                    {
                        Position = stops.position,
                        Color = Color.FromArgb((byte)(stops.opacity * 255), 0, 0, 0),
                    })
                    .ToArray()
            )
            {
                StartPoint = new Vector2(startX, 0),
                EndPoint = new Vector2(startX + width, 0),
            };
        }
    }
}
