using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Renderer
{
    public class PlayingLineRenderer
    {
        public static void Draw(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            int strokeWidth,
            ICanvasImage cachedStroke,
            ICanvasImage cachedFill,
            ICanvasImage unplayedComp,
            RenderLyricsLine line,
            double currentProgressMs,
            LyricsEffectSettings settings)
        {
            if (cachedStroke == null) return;

            DrawTertiaryText(ds, unplayedComp, strokeWidth, line);
            DrawPrimaryText(resourceCreator, ds, strokeWidth, cachedStroke, cachedFill, line, currentProgressMs, settings);
            DrawSecondaryText(ds, unplayedComp, strokeWidth, line);
        }

        private static void DrawTertiaryText(CanvasDrawingSession ds, ICanvasImage source, int strokeWidth, RenderLyricsLine line)
        {
            if (line.TertiaryTextLayout == null) return;

            var opacity = line.PhoneticOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.TertiaryTextLayout.LayoutBounds.Extend(strokeWidth / 2f);

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.TertiaryPosition.X,
                bounds.Y + line.TertiaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using var cropEffect = new CropEffect { Source = source, BorderMode = EffectBorderMode.Hard, SourceRectangle = destRect };
            using var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
            using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity };
            ds.DrawImage(opacityEffect);
        }

        private static void DrawSecondaryText(CanvasDrawingSession ds, ICanvasImage source, int strokeWidth, RenderLyricsLine line)
        {
            if (line.SecondaryTextLayout == null) return;

            var opacity = line.TranslatedOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.SecondaryTextLayout.LayoutBounds.Extend(strokeWidth / 2f);

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.SecondaryPosition.X,
                bounds.Y + line.SecondaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using var cropEffect = new CropEffect { Source = source, BorderMode = EffectBorderMode.Hard, SourceRectangle = destRect };
            using var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
            using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity };
            ds.DrawImage(opacityEffect);

            //ds.FillRectangle(destRect, Microsoft.UI.Colors.Red.WithAlpha(128));
        }

        private static void DrawPrimaryText(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            int strokeWidth,
            ICanvasImage cachedStroke,
            ICanvasImage cachedFill,
            RenderLyricsLine line,
            double currentProgressMs,
            LyricsEffectSettings settings)
        {
            if (line.PrimaryTextLayout == null || line.PrimaryTextRegions == null) return;

            for (int i = 0; i < line.PrimaryTextRegions.Length; i++)
            {
                DrawSubLineRegion(resourceCreator, ds, strokeWidth, cachedStroke, cachedFill, line, line.PrimaryTextRegions[i], i, currentProgressMs, settings);
            }
        }

        private static void DrawSubLineRegion(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            int strokeWidth,
            ICanvasImage cachedStroke,
            ICanvasImage cachedFill,
            RenderLyricsLine line,
            CanvasTextLayoutRegion subLineRegion,
            int regionIndex,
            double currentProgressMs,
            LyricsEffectSettings settings)
        {
            if (line.RenderLyricsRegions == null || regionIndex >= line.RenderLyricsRegions.Length) return;

            var playedOpacity = line.PlayedPrimaryOpacityTransition.Value;
            var unplayedOpacity = line.UnplayedPrimaryOpacityTransition.Value;

            var playedFillColor = line.PlayedFillColorTransition.Value;
            var unplayedFillColor = line.UnplayedFillColorTransition.Value;
            var playedStrokeColor = line.PlayedStrokeColorTransition.Value;
            var unplayedStrokeColor = line.UnplayedStrokeColorTransition.Value;

            var subLineLayoutBounds = subLineRegion.LayoutBounds.Extend(strokeWidth, strokeWidth / 2f);
            Rect subLineRect = new(
                subLineLayoutBounds.X + line.PrimaryPosition.X,
                subLineLayoutBounds.Y + line.PrimaryPosition.Y,
                subLineLayoutBounds.Width,
                subLineLayoutBounds.Height
            );

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
                        playedWidth += ch.LayoutRect.Width;
                    else
                        break;
                }
            }

            float progressInRegion = Math.Clamp((float)(playedWidth / subLineRegion.LayoutBounds.Width), 0f, 1f);
            float fadeProgressInRegion = 1f / subLineRegion.CharacterCount * 0.5f;

            if (subLineRegion.CharacterIndex >= line.PrimaryRenderChars.Count) return;
            float firstCharProgressInRegion = Math.Clamp((float)line.PrimaryRenderChars[subLineRegion.CharacterIndex].GetPlayProgress(currentProgressMs), 0f, 1f);

            // RenderLyricsRegion 缓存
            var region = line.RenderLyricsRegions[regionIndex];

            var fillStops = region.FillStops;
            fillStops[0].Position = 0; fillStops[0].Color = playedFillColor.WithAlpha((byte)(255 * playedOpacity));
            fillStops[1].Position = progressInRegion; fillStops[1].Color = playedFillColor.WithAlpha((byte)(255 * playedOpacity));
            fillStops[2].Position = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion; fillStops[2].Color = unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity));
            fillStops[3].Position = 1 + fadeProgressInRegion; fillStops[3].Color = unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity));

            var strokeStops = region.StrokeStops;
            strokeStops[0].Position = 0; strokeStops[0].Color = playedStrokeColor.WithAlpha((byte)(255 * playedOpacity));
            strokeStops[1].Position = progressInRegion; strokeStops[1].Color = playedStrokeColor.WithAlpha((byte)(255 * playedOpacity));
            strokeStops[2].Position = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion; strokeStops[2].Color = unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity));
            strokeStops[3].Position = 1 + fadeProgressInRegion; strokeStops[3].Color = unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity));

            using var fillGradientBrush = new CanvasLinearGradientBrush(resourceCreator, fillStops)
            {
                StartPoint = new Vector2((float)subLineRect.X, (float)subLineRect.Y),
                EndPoint = new Vector2((float)(subLineRect.X + subLineRect.Width), (float)subLineRect.Y)
            };
            using var fillGradientLayer = new CanvasCommandList(resourceCreator);
            using (var gds = fillGradientLayer.CreateDrawingSession())
            {
                gds.FillRectangle(subLineRect, fillGradientBrush);
            }

            region.FinalFillEffect.Source = fillGradientLayer;
            ICanvasImage finalOutputImage = region.FinalFillEffect;

            bool hasStroke = cachedStroke != null && region.FinalStrokeEffect != null && region.CombinedEffect != null;

            using var strokeGradientBrush = hasStroke ? new CanvasLinearGradientBrush(resourceCreator, strokeStops)
            {
                StartPoint = new Vector2((float)subLineRect.X, (float)subLineRect.Y),
                EndPoint = new Vector2((float)(subLineRect.X + subLineRect.Width), (float)subLineRect.Y)
            } : null;

            using var strokeGradientLayer = hasStroke ? new CanvasCommandList(resourceCreator) : null;

            if (hasStroke)
            {
                using (var gds = strokeGradientLayer!.CreateDrawingSession())
                {
                    gds.FillRectangle(subLineRect, strokeGradientBrush);
                }

                region.FinalStrokeEffect!.Source = strokeGradientLayer;
                finalOutputImage = region.CombinedEffect!;
            }

            if (!settings.IsLyricsFloatAnimationEnabled && !settings.IsLyricsGlowEffectEnabled && !settings.IsLyricsScaleEffectEnabled)
            {
                ds.DrawImage(finalOutputImage);
            }
            else
            {
                int endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                for (int i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                {
                    DrawSingleCharacter(ds, line, i, finalOutputImage);
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

                renderChar.Crop.Source = source;
                renderChar.Crop.SourceRectangle = sourcePlayedCharRect;
                renderChar.Glow.BlurAmount = (float)glow;

                ds.DrawImage(renderChar.Glow, destCharRect.Extend(destCharRect.Height), sourceCharRect.Extend(sourceCharRect.Height));
            }

            // Draw the top layer
            ds.DrawImage(source, destCharRect, sourceCharRect);
        }
    }

}
