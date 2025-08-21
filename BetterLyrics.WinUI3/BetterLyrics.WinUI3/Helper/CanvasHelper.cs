using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
                    Opacity = (float)(lyricsLine.OpacityTransition.Value * lyricsLayerOpacity),
                };
            }
            else
            {
                return new OpacityEffect
                {
                    Source = new GaussianBlurEffect
                    {
                        Source = backgroundFontEffect,
                        BlurAmount = (float)lyricsLine.BlurAmountTransition.Value,
                        BorderMode = EffectBorderMode.Soft,
                        Optimization = EffectOptimization.Speed,
                    },
                    Opacity = (float)(lyricsLine.OpacityTransition.Value * lyricsLayerOpacity),
                };
            }
        }

        public static CanvasCommandList CreateFontEffect(LyricsLine lyricsLine, ICanvasAnimatedControl control, Color strokeColor, int strokeWidth, Color fontColor)
        {
            CanvasCommandList list = new(control);
            using var ds = list.CreateDrawingSession();
            if (strokeWidth > 0)
            {
                if (lyricsLine.TextGeometry == null)
                {
                    return list;
                }
                ds.DrawGeometry(lyricsLine.TextGeometry, lyricsLine.Position, strokeColor, strokeWidth); // 描边
            }
            if (lyricsLine.CanvasTextLayout == null)
            {
                return list;
            }
            ds.DrawTextLayout(lyricsLine.CanvasTextLayout, lyricsLine.Position, fontColor); // 绘制文本（填充）
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
                BlurAmount = (float)glowEffectAmount,
                Optimization = EffectOptimization.Speed,
            };
        }

        public static CanvasCommandList CreateCharMask(ICanvasAnimatedControl control, LyricsLine lyricsLine, int charStartIndex, int charLength, double charProgress)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.CanvasTextLayout == null)
            {
                return mask;
            }

            var highlightRegion = lyricsLine.CanvasTextLayout.GetCharacterRegions(charStartIndex, charLength).FirstOrDefault();

            double highlightTotalWidth = (double)highlightRegion.LayoutBounds.Width;
            // Draw the highlight for the current character
            double highlightWidth = highlightTotalWidth * charProgress;

            double fadingWidth = (double)highlightRegion.LayoutBounds.Height / 2;

            // Rects
            var highlightRect = new Rect(
                highlightRegion.LayoutBounds.X,
                highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );
            var fadeOutRect = new Rect(
                highlightRect.Right,
                highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
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

            if (lyricsLine.CanvasTextLayout == null)
            {
                return mask;
            }

            using var ds = mask.CreateDrawingSession();

            var regions = lyricsLine.CanvasTextLayout.GetCharacterRegions(0, charStartIndex);
            var highlightRegion = lyricsLine.CanvasTextLayout
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
                        region.LayoutBounds.Y + lyricsLine.Position.Y,
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
                highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );

            ds.FillRectangle(highlightRect, Color.FromArgb(255, 128, 128, 128));

            if (fade)
            {
                var fadeOutRect = new Rect(
                    highlightRect.Right,
                    highlightRegion.LayoutBounds.Y + lyricsLine.Position.Y,
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

            if (lyricsLine.CanvasTextLayout == null)
            {
                return mask;
            }

            var regions = lyricsLine.CanvasTextLayout.GetCharacterRegions(0, lyricsLine.OriginalText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + lyricsLine.Position.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Colors.White);
                }
            }

            return mask;
        }

        public static CanvasCommandList CreateTranslationHighlightMask(ICanvasAnimatedControl control, LyricsLine lyricsLine)
        {
            var mask = new CanvasCommandList(control);
            using var ds = mask.CreateDrawingSession();

            if (lyricsLine.CanvasTextLayout == null)
            {
                return mask;
            }

            var regions = lyricsLine.CanvasTextLayout.GetCharacterRegions(lyricsLine.OriginalText.Length, lyricsLine.DisplayedText.Length - lyricsLine.OriginalText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + lyricsLine.Position.Y,
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
                Opacity = (float)opacity,
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
                BlurAmount = (float)shadowAmount,
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
                Opacity = (float)opacity,
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
