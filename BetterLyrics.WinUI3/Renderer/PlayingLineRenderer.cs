using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public class PlayingLineRenderer
    {
        public static void Draw(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            ICanvasImage textOnlyLayer,
            RenderLyricsLine line,
            double currentProgressMs,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            DrawTertiaryText(ds, textOnlyLayer, line);
            DrawPrimaryText(control, ds, textOnlyLayer, line, currentProgressMs, bgColor, fgColor, settings);
            DrawSecondaryText(ds, textOnlyLayer, line);
        }

        private static void DrawTertiaryText(CanvasDrawingSession ds, ICanvasImage source, RenderLyricsLine line)
        {
            if (line.TertiaryTextLayout == null) return;

            var opacity = line.PhoneticOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.TertiaryTextLayout.LayoutBounds;

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.TertiaryPosition.X,
                bounds.Y + line.TertiaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using (var cropEffect = new CropEffect { Source = source, BorderMode = EffectBorderMode.Hard, SourceRectangle = destRect })
            {
                using (var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft })
                {
                    using (var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity })
                    {
                        ds.DrawImage(opacityEffect);
                    }
                }
            }
        }

        private static void DrawSecondaryText(CanvasDrawingSession ds, ICanvasImage source, RenderLyricsLine line)
        {
            if (line.SecondaryTextLayout == null) return;

            var opacity = line.TranslatedOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.SecondaryTextLayout.LayoutBounds;

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.SecondaryPosition.X,
                bounds.Y + line.SecondaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using (var cropEffect = new CropEffect { Source = source, BorderMode = EffectBorderMode.Hard, SourceRectangle = destRect })
            {
                using (var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft })
                {
                    using (var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity })
                    {
                        ds.DrawImage(opacityEffect);
                    }
                }
            }
        }

        private static void DrawPrimaryText(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            RenderLyricsLine line,
            double currentProgressMs,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            if (line.PrimaryTextLayout == null) return;
            if (line.PrimaryTextRegions == null) return;

            foreach (var subLineRegion in line.PrimaryTextRegions)
            {
                DrawSubLineRegion(resourceCreator, ds, source, line, subLineRegion, currentProgressMs, bgColor, fgColor, settings);
            }
        }

        private static void DrawSubLineRegion(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            RenderLyricsLine line,
            CanvasTextLayoutRegion subLineRegion,
            double currentProgressMs,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            var playedOpacity = line.PlayedPrimaryOpacityTransition.Value;
            var unplayedOpacity = line.UnplayedPrimaryOpacityTransition.Value;

            var subLineLayoutBounds = subLineRegion.LayoutBounds;
            Rect subLineRect = new(
                subLineLayoutBounds.X + line.PrimaryPosition.X,
                subLineLayoutBounds.Y + line.PrimaryPosition.Y,
                subLineLayoutBounds.Width,
                subLineLayoutBounds.Height
            );

            using (var gradientLayer = new CanvasCommandList(resourceCreator))
            {
                using (var gradientLayerDs = gradientLayer.CreateDrawingSession())
                {
                    double playedWidth = 0;
                    if (settings.WordByWordEffectMode == Enums.WordByWordEffectMode.Never ||
                        (settings.WordByWordEffectMode == Enums.WordByWordEffectMode.Auto && !line.IsPrimaryHasRealSyllableInfo))
                    {
                        playedWidth = subLineRegion.LayoutBounds.Width;
                    }
                    else
                    {
                        for (int i = subLineRegion.CharacterIndex; i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount; i++)
                        {
                            if (i >= line.PrimaryRenderChars.Count) return;
                            var ch = line.PrimaryRenderChars[i];
                            if (ch.IsPlayingLastFrame)
                            {
                                playedWidth += ch.LayoutRect.Width * ch.GetPlayProgress(currentProgressMs);
                                break;
                            }

                            if (ch.GetPlayProgress(currentProgressMs) >= 1)
                            {
                                playedWidth += ch.LayoutRect.Width;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    float progressInRegion = (float)(playedWidth / subLineRegion.LayoutBounds.Width);
                    progressInRegion = Math.Clamp(progressInRegion, 0f, 1f);

                    float fadeProgressInRegion = 1f / subLineRegion.CharacterCount * 0.5f;

                    if (subLineRegion.CharacterIndex >= line.PrimaryRenderChars.Count) return;
                    float firstCharProgressInRegion = (float)line.PrimaryRenderChars[subLineRegion.CharacterIndex].GetPlayProgress(currentProgressMs);
                    firstCharProgressInRegion = Math.Clamp(firstCharProgressInRegion, 0f, 1f);

                    var stop1 = fgColor.WithAlpha((byte)(255 * playedOpacity));
                    var stop2 = bgColor.WithAlpha((byte)(255 * unplayedOpacity));

                    var stops = new CanvasGradientStop[4];
                    stops[0] = new CanvasGradientStop { Position = 0, Color = stop1 };
                    stops[1] = new CanvasGradientStop { Position = progressInRegion, Color = stop1 };
                    stops[2] = new CanvasGradientStop { Position = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion, Color = stop2 };
                    stops[3] = new CanvasGradientStop { Position = 1 + fadeProgressInRegion, Color = stop2 };

                    using (var gradientBrush = new CanvasLinearGradientBrush(resourceCreator, stops))
                    {
                        gradientBrush.StartPoint = new Vector2((float)subLineRect.X, (float)subLineRect.Y);
                        gradientBrush.EndPoint = new Vector2((float)(subLineRect.X + subLineRect.Width), (float)subLineRect.Y);
                        gradientLayerDs.FillRectangle(subLineRect, gradientBrush);
                    }
                }

                // 这里 gradientLayer 上色的时候已经限制了 Rect 区域，不用再套一个 CropEffect
                using (var textWithOpacityLayer = new AlphaMaskEffect
                {
                    Source = source,
                    AlphaMask = gradientLayer
                })
                {
                    if (!settings.IsLyricsFloatAnimationEnabled && !settings.IsLyricsGlowEffectEnabled && !settings.IsLyricsScaleEffectEnabled)
                    {
                        ds.DrawImage(textWithOpacityLayer);
                    }
                    else
                    {
                        int endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                        for (int i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                        {
                            DrawSingleCharacter(ds, line, i, textWithOpacityLayer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="line"></param>
        /// <param name="charIndex">遍历的字符索引（相对于整行）</param>
        /// <param name="exactProgressIndex">当前播放字符的索引（相对于整行）</param>
        /// <param name="source"></param>
        /// <param name="state"></param>
        private static void DrawSingleCharacter(
            CanvasDrawingSession ds,
            RenderLyricsLine line,
            int charIndex,
            ICanvasImage source)
        {
            if (charIndex >= line.PrimaryRenderChars.Count) return;

            RenderLyricsChar renderChar = line.PrimaryRenderChars[charIndex];

            var rect = renderChar.LayoutRect;
            var sourceCharRect = new Rect(
                rect.X + line.PrimaryPosition.X,
                rect.Y + line.PrimaryPosition.Y,
                rect.Width,
                rect.Height
            );

            double scale = renderChar.ScaleTransition.Value;
            double glow = renderChar.GlowTransition.Value;
            double floatOffset = renderChar.FloatTransition.Value;

            var destCharRect = sourceCharRect.Scale(scale).AddY(floatOffset);

            // Draw glow
            if (glow > 0)
            {
                var sourcePlayedCharRect = new Rect(
                    sourceCharRect.X,
                    sourceCharRect.Y,
                    sourceCharRect.Width * renderChar.ProgressPlayed,
                    sourceCharRect.Height
                );
                using (var cropEffect = new CropEffect { Source = source, SourceRectangle = sourcePlayedCharRect, BorderMode = EffectBorderMode.Hard })
                {
                    using (var glowEffect = new GaussianBlurEffect { Source = cropEffect, BlurAmount = (float)glow, BorderMode = EffectBorderMode.Soft })
                    {
                        ds.DrawImage(glowEffect, destCharRect.Extend(destCharRect.Height), sourceCharRect.Extend(sourceCharRect.Height));
                    }
                }
            }

            // Draw the top layer
            ds.DrawImage(source, destCharRect, sourceCharRect);
        }
    }

}
