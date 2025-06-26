using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        /// <summary>
        /// The Draw
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (IsCoverOverlayEnabled)
            {
                DrawAlbumArtBackground(control, ds);
            }

            if (IsDockMode)
            {
                DrawImmersiveBackground(control, ds, IsCoverOverlayEnabled);
            }

            // Blurred lyrics layer
            using var blurredLyrics = new CanvasCommandList(control);
            using (var blurredLyricsDs = blurredLyrics.CreateDrawingSession())
            {
                switch (DisplayType)
                {
                    case LyricsDisplayType.AlbumArtOnly:
                    case LyricsDisplayType.PlaceholderOnly:
                        break;
                    case LyricsDisplayType.LyricsOnly:
                    case LyricsDisplayType.SplitView:
                        DrawBlurredLyrics(control, blurredLyricsDs);
                        break;
                    default:
                        break;
                }
            }

            // Masked mock gradient blurred lyrics layer
            using var maskedBlurredLyrics = new CanvasCommandList(control);
            using (var maskedBlurredLyricsDs = maskedBlurredLyrics.CreateDrawingSession())
            {
                if (LyricsVerticalEdgeOpacity == 100)
                {
                    maskedBlurredLyricsDs.DrawImage(blurredLyrics);
                }
                else
                {
                    using var mask = new CanvasCommandList(control);
                    using (var maskDs = mask.CreateDrawingSession())
                    {
                        DrawGradientOpacityMask(control, maskDs);
                    }
                    maskedBlurredLyricsDs.DrawImage(
                        new AlphaMaskEffect { Source = blurredLyrics, AlphaMask = mask }
                    );
                }
            }

            // For desktop mode
            //ds.DrawImage(
            //    new ShadowEffect
            //    {
            //        Source = maskedBlurredLyrics,
            //        ShadowColor = Colors.Black,
            //        BlurAmount = 8f,
            //        Optimization = EffectOptimization.Quality,
            //    }
            //);

            ds.DrawImage(maskedBlurredLyrics);

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

                if (_isDebugOverlayEnabled)
                {
                    ds.DrawText(
                        $"DEBUG: "
                            + $"播放行 {currentPlayingLineIndex}, 字符 {charStartIndex}, 长度 {charLength}, 进度 {charProgress}\n"
                            + $"可见行 [{_startVisibleLineIndex}, {_endVisibleLineIndex}]\n"
                            + $"当前时刻 {TotalTime}",
                        new Vector2(10, 10),
                        Colors.Red
                    );
                }
            }
        }

        /// <summary>
        /// The DrawImgae
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="softwareBitmap">The softwareBitmap<see cref="SoftwareBitmap"/></param>
        /// <param name="opacity">The opacity<see cref="float"/></param>
        private static void DrawImgae(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            SoftwareBitmap softwareBitmap,
            float opacity
        )
        {
            using var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, softwareBitmap);
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            var scaleFactor =
                (float)Math.Sqrt(Math.Pow(control.Size.Width, 2) + Math.Pow(control.Size.Height, 2))
                / Math.Min(imageWidth, imageHeight);

            ds.DrawImage(
                new OpacityEffect
                {
                    Source = new ScaleEffect
                    {
                        InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
                        BorderMode = EffectBorderMode.Hard,
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    Opacity = opacity,
                },
                (float)control.Size.Width / 2 - imageWidth * scaleFactor / 2,
                (float)control.Size.Height / 2 - imageHeight * scaleFactor / 2
            );
        }

        /// <summary>
        /// The DrawAlbumArtBackground
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            ds.Transform = Matrix3x2.CreateRotation(_rotateAngle, control.Size.ToVector2() * 0.5f);

            var overlappedCovers = new CanvasCommandList(control.Device);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_albumArtBgTransition.IsTransitioning)
            {
                if (_lastAlbumArtBitmap != null)
                {
                    DrawImgae(
                        control,
                        overlappedCoversDs,
                        _lastAlbumArtBitmap,
                        1 - _albumArtBgTransition.Value
                    );
                }
                if (_albumArtBitmap != null)
                {
                    DrawImgae(
                        control,
                        overlappedCoversDs,
                        _albumArtBitmap,
                        _albumArtBgTransition.Value
                    );
                }
            }
            else if (_albumArtBitmap != null)
            {
                DrawImgae(control, overlappedCoversDs, _albumArtBitmap, 1f);
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

        /// <summary>
        /// The DrawGradientOpacityMask
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        private void DrawGradientOpacityMask(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds
        )
        {
            byte verticalEdgeAlpha = (byte)(255 * LyricsVerticalEdgeOpacity / 100f);
            using var maskBrush = new CanvasLinearGradientBrush(
                control,
                [
                    new() { Position = 0, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                    new() { Position = 0.5f, Color = Color.FromArgb(255, 0, 0, 0) },
                    new() { Position = 1, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                ]
            )
            {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height),
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        /// <summary>
        /// The DrawImmersiveBackground
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="withGradient">The withGradient<see cref="bool"/></param>
        private void DrawImmersiveBackground(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            bool withGradient
        )
        {
            ds.FillRectangle(
                new Rect(0, 0, control.Size.Width, control.Size.Height),
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
                    EndPoint = new Vector2(0, (float)control.Size.Height),
                }
            );
        }

        /// <summary>
        /// The DrawLyrics
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="currentLineHighlightType">The currentLineHighlightType<see cref="LyricsHighlightType"/></param>
        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

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
                    case LyricsAlignmentType.Left:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += (float)_maxLyricsWidthTransition.Value / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)_maxLyricsWidthTransition.Value;
                        break;
                    default:
                        break;
                }

                float offsetToLeft =
                    (float)control.Size.Width - _rightMargin - _maxLyricsWidthTransition.Value;

                // Scale
                ds.Transform =
                    Matrix3x2.CreateScale(line.ScaleTransition.Value, new Vector2(centerX, centerY))
                    * Matrix3x2.CreateTranslation(
                        offsetToLeft,
                        _canvasYScrollTransition.Value + (float)(control.Size.Height / 2)
                    );

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
                        Source = new OpacityEffect { Source = lyrics, Opacity = _defaultOpacity },
                        BlurAmount = line.BlurAmountTransition.Value,
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    }
                );

                // 再叠加当前行歌词层
                // Only draw the current line and the two lines around it
                // This layer is to highlight the current line
                // and for fade-in and fade-out effects, two lines around it is also drawn
                if (Math.Abs(i - currentPlayingLineIndex) <= 1)
                {
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
                        maskDs.FillRectangle(
                            new Rect(
                                textLayout.LayoutBounds.X,
                                position.Y,
                                textLayout.LayoutBounds.Width,
                                textLayout.LayoutBounds.Height
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
                                        BorderMode = EffectBorderMode.Soft,
                                    }
                                    : new CanvasCommandList(control.Device),
                                Foreground = new AlphaMaskEffect
                                {
                                    Source = lyrics,
                                    AlphaMask = mask,
                                },
                            },
                            Opacity = line.HighlightOpacityTransition.Value,
                        }
                    );
                }

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        /// <summary>
        /// The GetHorizontalFillBrush
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="stopPosition">The stopPosition<see cref="float[]"/></param>
        /// <param name="stopOpacity">The stopOpacity<see cref="float[]"/></param>
        /// <param name="startX">The startX<see cref="float"/></param>
        /// <param name="endX">The endX<see cref="float"/></param>
        /// <returns>The <see cref="CanvasLinearGradientBrush"/></returns>
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
