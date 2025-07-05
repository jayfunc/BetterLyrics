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

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            DrawAlbumArtBackground(control, combinedDs);

            if (_isDockMode)
            {
                DrawImmersiveBackground(control, combinedDs);
            }

            combinedDs.DrawImage(blurredLyrics);

            if (_isDesktopMode)
            {
                ds.DrawImage(blurredLyrics);
            }
            else
            {
                ds.DrawImage(combined);
            }

            DrawAlbumArt(control, ds);

            DrawTitleAndArtist(control, ds);

            if (_isDebugOverlayEnabled)
            {
                var currentPlayingLineIndex = GetCurrentPlayingLineIndex();
                var currentPlayingLine = _multiLangLyrics
                    .SafeGet(_langIndex)
                    ?.SafeGet(currentPlayingLineIndex);

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
                            $"Cur playing {currentPlayingLineIndex}, char start idx {charStartIndex}, length {charLength}, prog {charProgress}\n" +
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
                    //            $"[{i}] {line.Text} {line.ScaleTransition.Value}",
                    //            new Vector2(10, 30 + (i - _startVisibleLineIndex) * 20),
                    //            ThemeTypeSent == Microsoft.UI.Xaml.ElementTheme.Light ? Colors.Black : Colors.White
                    //        );
                    //    }
                    //}
                }
            }
        }

        private void DrawBackgroundImgae(ICanvasAnimatedControl control, CanvasDrawingSession ds, SoftwareBitmap swBitmap, float opacity)
        {
            using var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, swBitmap);
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float scaleFactor = MathF.Sqrt(MathF.Pow(_canvasWidth, 2) + MathF.Pow(_canvasHeight, 2)) / MathF.Min(imageWidth, imageHeight);

            float x = _canvasWidth / 2 - imageWidth * scaleFactor / 2;
            float y = _canvasHeight / 2 - imageHeight * scaleFactor / 2;

            ds.DrawImage(new OpacityEffect
            {
                Source = new ScaleEffect
                {
                    Scale = new Vector2(scaleFactor),
                    Source = canvasBitmap,
                },
                Opacity = opacity,
            }, new Vector2(x, y)
            );
        }

        private void DrawForegroundImgae(ICanvasAnimatedControl control, CanvasDrawingSession ds, SoftwareBitmap swBitmap, float opacity)
        {
            using var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, swBitmap);
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float scaleFactor = _albumArtSize / Math.Min(imageWidth, imageHeight);
            if (scaleFactor < 0.1f) return;

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

            var overlappedCovers = new CanvasCommandList(control.Device);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_lastAlbumArtSwBitmap != null)
            {
                DrawBackgroundImgae(control, overlappedCoversDs, _lastAlbumArtSwBitmap, 1 - _albumArtBgTransition.Value);
            }
            if (_albumArtSwBitmap != null)
            {
                DrawBackgroundImgae(control, overlappedCoversDs, _albumArtSwBitmap, _albumArtBgTransition.Value);
            }

            using var coverOverlayEffect = new OpacityEffect
            {
                Opacity = CoverOverlayOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = CoverOverlayBlurAmount,
                    Source = overlappedCovers,
                },
            };
            ds.DrawImage(coverOverlayEffect);

            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            using var albumArt = new CanvasCommandList(control.Device);
            using var albumArtDs = albumArt.CreateDrawingSession();
            if (_albumArtSwBitmap != null)
            {
                DrawForegroundImgae(control, albumArtDs, _albumArtSwBitmap, _albumArtBgTransition.Value);
            }
            if (_lastAlbumArtSwBitmap != null)
            {
                DrawForegroundImgae(control, albumArtDs, _lastAlbumArtSwBitmap, 1 - _albumArtBgTransition.Value);
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
                _fontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));
            ds.DrawTextLayout(
                artistLayout,
                new Vector2(_albumArtXTransition.Value, _titleY + (float)titleLayout.LayoutBounds.Height),
                _fontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
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

                switch (LyricsAlignmentType)
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

                // Create the original lyrics line
                using var lyrics = new CanvasCommandList(control.Device);
                using var lyricsDs = lyrics.CreateDrawingSession();
                lyricsDs.DrawTextLayout(textLayout, position, _fontColor);

                // Mock gradient blurred lyrics layer
                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层
                // Current line will not be blurred
                ds.DrawImage(
                    new GaussianBlurEffect
                    {
                        Source = new OpacityEffect { Source = lyrics, Opacity = line.OpacityTransition.Value * _lyricsOpacityTransition.Value },
                        BlurAmount = line.BlurAmountTransition.Value,
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    }
                );

                // 再叠加当前行歌词层
                using var mask = new CanvasCommandList(control.Device);
                using var maskDs = mask.CreateDrawingSession();

                using var highlightMask = new CanvasCommandList(control.Device);
                using var highlightMaskDs = highlightMask.CreateDrawingSession();

                if (i == currentPlayingLineIndex)
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
                            Background = IsLyricsGlowEffectEnabled
                                ? new GaussianBlurEffect
                                {
                                    Source = new AlphaMaskEffect
                                    {
                                        Source = lyrics,
                                        AlphaMask = LyricsGlowEffectScope switch
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
                                Source = lyrics,
                                AlphaMask = mask,
                            },
                        },
                        Opacity = line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value,
                    }
                );

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawImmersiveBackground(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            bool withGradient = true
        )
        {
            ds.FillRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                new CanvasLinearGradientBrush(
                    control,
                    [
                        new CanvasGradientStop
                        {
                            Position = 0f,
                            Color = withGradient
                                ? Color.FromArgb(
                                    211,
                                    _immersiveBgTransition.Value.R,
                                    _immersiveBgTransition.Value.G,
                                    _immersiveBgTransition.Value.B
                                )
                                : _immersiveBgTransition.Value,
                        },
                        new CanvasGradientStop
                        {
                            Position = 1,
                            Color = _immersiveBgTransition.Value,
                        },
                    ]
                )
                {
                    StartPoint = new Vector2(0, 0),
                    EndPoint = new Vector2(0, _canvasHeight),
                }
            );
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
