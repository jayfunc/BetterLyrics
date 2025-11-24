using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class CanvasHelper
    {
        public static CanvasLinearGradientBrush CreateHorizontalFillBrush(
            ICanvasAnimatedControl control,
            List<(double position, double opacity)> stops,
            double startX,
            double width
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(stops => new CanvasGradientStop
            {
                Position = (float)stops.position,
                Color = Color.FromArgb((byte)(stops.opacity * 255), 128, 128, 128),
            }).ToArray())
            {
                StartPoint = new Vector2((float)startX, 0),
                EndPoint = new Vector2((float)(startX + width), 0),
            };
        }

        /// <summary>
        /// 背景层
        /// </summary>
        /// <param name="lyricsLayerOpacity">_lyricsOpacityTransition.Value</param>
        public static OpacityEffect CreateBackgroundEffect(LyricsLine lyricsLine, CanvasCommandList backgroundFontEffect, double lyricsLayerOpacity)
        {
            if (lyricsLine.BlurAmountTransition.Value == 0)
            {
                return new OpacityEffect
                {
                    Source = backgroundFontEffect,
                    Opacity = (float)Math.Clamp(lyricsLine.OpacityTransition.Value * lyricsLayerOpacity, 0, 1),
                };
            }
            else
            {
                return new OpacityEffect
                {
                    Source = new GaussianBlurEffect
                    {
                        Source = backgroundFontEffect,
                        BlurAmount = (float)Math.Max(lyricsLine.BlurAmountTransition.Value, 0),
                        BorderMode = EffectBorderMode.Soft,
                        Optimization = EffectOptimization.Speed,
                    },
                    Opacity = (float)Math.Clamp(lyricsLine.OpacityTransition.Value * lyricsLayerOpacity, 0, 1),
                };
            }
        }

        public static CanvasCommandList CreateFontEffect(LyricsLine lyricsLine, ICanvasAnimatedControl control, Color strokeColor, int strokeWidth, Color fontColor)
        {
            CanvasCommandList list = new(control);
            using var ds = list.CreateDrawingSession();

            // 描边
            if (strokeWidth > 0)
            {
                if (lyricsLine.PhoneticCanvasGeometry != null)
                {
                    ds.DrawGeometry(lyricsLine.PhoneticCanvasGeometry, lyricsLine.PhoneticPosition, strokeColor, strokeWidth);
                }
                if (lyricsLine.OriginalCanvasGeometry != null)
                {
                    ds.DrawGeometry(lyricsLine.OriginalCanvasGeometry, lyricsLine.OriginalPosition, strokeColor, strokeWidth);
                }
                if (lyricsLine.TranslatedCanvasGeometry != null)
                {
                    ds.DrawGeometry(lyricsLine.TranslatedCanvasGeometry, lyricsLine.TranslatedPosition, strokeColor, strokeWidth);
                }
            }

            // 绘制文本（填充）
            if (lyricsLine.PhoneticCanvasTextLayout != null)
            {
                ds.DrawTextLayout(lyricsLine.PhoneticCanvasTextLayout, lyricsLine.PhoneticPosition, fontColor);
            }
            if (lyricsLine.OriginalCanvasTextLayout != null)
            {
                ds.DrawTextLayout(lyricsLine.OriginalCanvasTextLayout, lyricsLine.OriginalPosition, fontColor);
            }
            if (lyricsLine.TranslatedCanvasTextLayout != null)
            {
                ds.DrawTextLayout(lyricsLine.TranslatedCanvasTextLayout, lyricsLine.TranslatedPosition, fontColor);
            }

            return list;
        }

        /// <summary>
        /// 创建辉光效果层
        /// 仅需在布局重构 (Relayout) 时调用
        /// </summary>
        /// <param name="lineRenderingType">_lyricsGlowEffectScope</param>
        /// <param name="glowEffectAmount">_lyricsGlowEffectAmount</param>
        public static GaussianBlurEffect CreateForegroundBlurEffect(CanvasCommandList foregroundFontEffect, IGraphicsEffectSource mask, double glowEffectAmount)
        {
            return new GaussianBlurEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = foregroundFontEffect,
                    AlphaMask = mask,
                },
                BlurAmount = (float)Math.Clamp(glowEffectAmount, 0, 100),
                Optimization = EffectOptimization.Speed,
            };
        }

        public static CanvasCommandList CreateCharMask(ICanvasAnimatedControl control, LyricsLine lyricsLine, int charStartIndex, int charLength, double charProgress)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.OriginalCanvasTextLayout == null)
            {
                return mask;
            }

            var highlightRegion = lyricsLine.OriginalCanvasTextLayout.GetCharacterRegions(charStartIndex, charLength).FirstOrDefault();

            double highlightTotalWidth = (double)highlightRegion.LayoutBounds.Width;
            // Draw the highlight for the current character
            double highlightWidth = highlightTotalWidth * charProgress;

            double fadingWidth = (double)highlightRegion.LayoutBounds.Height / 2;

            // Rects
            var highlightRect = new Rect(
                highlightRegion.LayoutBounds.X,
                highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );
            var fadeOutRect = new Rect(
                highlightRect.Right,
                highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );

            // Brushes
            using var fadeInBrush = CanvasHelper.CreateHorizontalFillBrush(
                control,
                [(0f, 0f), (1f, 1f)],
                (double)highlightRect.Right - fadingWidth,
                fadingWidth
            );
            using var fadeOutBrush = CanvasHelper.CreateHorizontalFillBrush(
                control,
                [(0f, 1f), (1f, 0f)],
                (double)highlightRect.Right,
                fadingWidth
            );

            ds.FillRectangle(fadeInRect, fadeInBrush);
            ds.FillRectangle(fadeOutRect, fadeOutBrush);

            return mask;
        }

        public static CanvasCommandList CreateLineStartToCharMask(ICanvasAnimatedControl control, LyricsLine lyricsLine, int charStartIndex, int charLength, double charProgress, bool fade)
        {
            var mask = new CanvasCommandList(control);

            if (lyricsLine.OriginalCanvasTextLayout == null)
            {
                return mask;
            }

            using var ds = mask.CreateDrawingSession();

            var regions = lyricsLine.OriginalCanvasTextLayout.GetCharacterRegions(0, charStartIndex);
            var highlightRegion = lyricsLine.OriginalCanvasTextLayout
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
                        region.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Color.FromArgb(255, 128, 128, 128));
                }
            }

            double highlightTotalWidth = (double)highlightRegion.LayoutBounds.Width;
            // Draw the highlight for the current character
            double highlightWidth = highlightTotalWidth * charProgress;

            double fadingWidth = (double)highlightRegion.LayoutBounds.Height / 2;

            // Rects
            var highlightRect = new Rect(
                highlightRegion.LayoutBounds.X,
                highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );

            ds.FillRectangle(highlightRect, Color.FromArgb(255, 128, 128, 128));

            if (fade)
            {
                var fadeOutRect = new Rect(
                    highlightRect.Right,
                    highlightRegion.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                    fadingWidth,
                    highlightRegion.LayoutBounds.Height
                );
                using var fadeOutBrush = CreateHorizontalFillBrush(
                    control,
                    [(0f, 1f), (1f, 0f)],
                    (double)highlightRect.Right,
                    fadingWidth
                );
                ds.FillRectangle(fadeOutRect, fadeOutBrush);
            }

            return mask;
        }

        public static CanvasCommandList CreateLineMask(ICanvasAnimatedControl control, LyricsLine lyricsLine)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.OriginalCanvasTextLayout == null)
            {
                return mask;
            }

            var regions = lyricsLine.OriginalCanvasTextLayout.GetCharacterRegions(0, lyricsLine.OriginalText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + lyricsLine.OriginalPosition.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Colors.White);
                }
            }

            return mask;
        }

        public static CanvasCommandList CreatePhoneticHighlightMask(ICanvasAnimatedControl control, LyricsLine lyricsLine)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.PhoneticCanvasTextLayout == null)
            {
                return mask;
            }

            var regions = lyricsLine.PhoneticCanvasTextLayout.GetCharacterRegions(0, lyricsLine.PhoneticText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + lyricsLine.PhoneticPosition.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Colors.White);
                }
            }

            return mask;
        }

        public static CanvasCommandList CreateTranslatedHighlightMask(ICanvasAnimatedControl control, LyricsLine lyricsLine)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.TranslatedCanvasTextLayout == null)
            {
                return mask;
            }

            var regions = lyricsLine.TranslatedCanvasTextLayout.GetCharacterRegions(0, lyricsLine.TranslatedText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + lyricsLine.TranslatedPosition.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Colors.White);
                }
            }

            return mask;
        }

        /// <summary>
        /// 创建高亮效果层
        /// </summary>
        /// <param name="control"></param>
        /// <param name="lineRenderingType"></param>
        public static OpacityEffect CreateForegroundHighlightEffect(CanvasCommandList foregroundFontEffect, IGraphicsEffectSource mask, double opacity)
        {
            return new OpacityEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = foregroundFontEffect,
                    AlphaMask = mask,
                },
                Opacity = (float)Math.Clamp(opacity, 0, 1),
            };
        }

        public static ShadowEffect CreateForegroundShadowEffect(CanvasCommandList foregroundFontEffect, IGraphicsEffectSource mask, Color shadowColor, double shadowAmount)
        {
            return new ShadowEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = foregroundFontEffect,
                    AlphaMask = mask,
                },
                ShadowColor = shadowColor,
                BlurAmount = (float)Math.Clamp(shadowAmount, 0, 100),
                Optimization = EffectOptimization.Speed,
            };
        }

        public static OpacityEffect CreateForegroundTranslationEffect(CanvasCommandList foregroundFontEffect, IGraphicsEffectSource mask, double opacity)
        {
            return new OpacityEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = foregroundFontEffect,
                    AlphaMask = mask,
                },
                Opacity = (float)Math.Clamp(opacity, 0, 1),
            };
        }

        public static IGraphicsEffectSource GetAlphaMask(ICanvasAnimatedControl control, IGraphicsEffectSource charMask, IGraphicsEffectSource lineStartToCharMask, IGraphicsEffectSource lineMask, LineRenderingType lineRenderingType)
        {
            var result = lineRenderingType switch
            {
                LineRenderingType.CurrentChar => charMask,
                LineRenderingType.LineStartToCurrentChar => lineStartToCharMask,
                LineRenderingType.CurrentLine => lineMask,
                _ => new CanvasCommandList(control),
            };
            return result;
        }
    }
}
