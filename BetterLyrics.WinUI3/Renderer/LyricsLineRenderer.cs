using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using System;
using System.Numerics;
using Windows.Foundation;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.Renderer
{
    public class LyricsLineRenderer
    {
        public bool IsPlaying { get; set; }

        public int StrokeWidth { get; set; }
        public double CurrentProgressMs { get; set; }

        public double LyricsWidth { get; set; }
        public double LyricsHeight { get; set; }

        public RenderLyricsLine? Line { get; set; }
        public LyricsWindowStatus? LyricsWindowStatus { get; set; }

        public void Draw(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds)
        {
            DrawTertiaryText(ds);
            DrawPrimaryText(resourceCreator, ds);
            DrawSecondaryText(ds);
        }

        private void DrawTertiaryText(CanvasDrawingSession ds)
        {
            if (LyricsWindowStatus == null) return;
            if (Line == null) return;
            if (Line.TertiaryTextLayout == null) return;

            var opacity = Line.TertiaryOpacityTransition.Value;
            var blur = Line.BlurAmountTransition.Value;
            var bounds = Line.TertiaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);

            if (double.IsNaN(opacity) || opacity <= 0) return;

            var srcRect = new Rect(
                bounds.X + Line.TertiaryPosition.X,
                bounds.Y + Line.TertiaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            var destRect = srcRect;

            if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            {
                destRect = destRect.AddX(Line.TertiaryXOffsetTransition.Value);
            }

            using var cropEffect = new CropEffect { Source = Line.UnplayedComposite, BorderMode = EffectBorderMode.Hard, SourceRectangle = srcRect };
            using var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
            using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity };
            ds.DrawImage(opacityEffect, destRect, srcRect);
        }

        private void DrawSecondaryText(CanvasDrawingSession ds)
        {
            if (LyricsWindowStatus == null) return;
            if (Line == null) return;
            if (Line.SecondaryTextLayout == null) return;

            var opacity = Line.SecondaryOpacityTransition.Value;
            var blur = Line.BlurAmountTransition.Value;
            var bounds = Line.SecondaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);

            if (double.IsNaN(opacity)) return;

            var srcRect = new Rect(
                bounds.X + Line.SecondaryPosition.X,
                bounds.Y + Line.SecondaryPosition.Y,
                bounds.Width,
                bounds.Height
            );

            var destRect = srcRect;

            if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            {
                destRect = destRect.AddX(Line.SecondaryXOffsetTransition.Value);
            }

            using var cropEffect = new CropEffect { Source = Line.UnplayedComposite, BorderMode = EffectBorderMode.Hard, SourceRectangle = srcRect };
            using var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
            using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity };
            ds.DrawImage(opacityEffect, destRect, srcRect);
        }

        private void DrawPrimaryText(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds)
        {
            if (LyricsWindowStatus == null) return;
            if (Line == null) return;

            if (Line.PrimaryTextLayout == null || Line.PrimaryTextRegions == null) return;

            var bounds = Line.PrimaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);
            var srcRect = new Rect(bounds.X + Line.PrimaryPosition.X, bounds.Y + Line.PrimaryPosition.Y, bounds.Width, bounds.Height);
            var destRect = srcRect;

            if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            {
                destRect = destRect.AddX(Line.PrimaryXOffsetTransition.Value);
            }

            if (IsPlaying)
            {
                for (int i = 0; i < Line.PrimaryTextRegions.Length; i++)
                {
                    DrawSubLineRegion(resourceCreator, ds, i);
                }
            }
            else
            {
                var opacity = MathF.Max((float)Line.PlayedPrimaryOpacityTransition.Value, (float)Line.UnplayedPrimaryOpacityTransition.Value);
                var blur = Line.BlurAmountTransition.Value;

                if (double.IsNaN(opacity)) return;

                using var cropEffect = new CropEffect { Source = Line.UnplayedComposite, BorderMode = EffectBorderMode.Hard, SourceRectangle = srcRect };
                using var blurEffect = new GaussianBlurEffect { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
                using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = opacity };
                ds.DrawImage(opacityEffect, destRect, srcRect);
            }
        }

        private void DrawSubLineRegion(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds, int regionIndex)
        {
            if (LyricsWindowStatus == null) return;
            if (Line == null) return;
            if (Line.PrimaryTextRegions == null) return;

            if (Line.RenderLyricsRegions == null || regionIndex >= Line.RenderLyricsRegions.Length) return;

            var subLineRegion = Line.PrimaryTextRegions[regionIndex];

            var playedOpacity = Line.PlayedPrimaryOpacityTransition.Value;
            var unplayedOpacity = Line.UnplayedPrimaryOpacityTransition.Value;

            var playedFillColor = Line.PlayedFillColorTransition.Value;
            var unplayedFillColor = Line.UnplayedFillColorTransition.Value;
            var playedStrokeColor = Line.PlayedStrokeColorTransition.Value;
            var unplayedStrokeColor = Line.UnplayedStrokeColorTransition.Value;

            var subLineLayoutBounds = subLineRegion.LayoutBounds.Extend(StrokeWidth, StrokeWidth / 2f);
            Rect subLineRect = new(
                subLineLayoutBounds.X + Line.PrimaryPosition.X,
                subLineLayoutBounds.Y + Line.PrimaryPosition.Y,
                subLineLayoutBounds.Width,
                subLineLayoutBounds.Height
            );

            double playedWidth = 0;
            if (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == Enums.WordByWordEffectMode.Never ||
                (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == Enums.WordByWordEffectMode.Auto && !Line.IsPrimaryHasRealSyllableInfo))
            {
                playedWidth = subLineRegion.LayoutBounds.Width;
            }
            else
            {
                for (int i = subLineRegion.CharacterIndex; i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount; i++)
                {
                    if (i >= Line.PrimaryRenderChars.Count) return;
                    var ch = Line.PrimaryRenderChars[i];
                    if (ch.IsPlayingLastFrame)
                    {
                        playedWidth += ch.LayoutRect.Width * ch.GetPlayProgress(CurrentProgressMs);
                        break;
                    }

                    if (ch.GetPlayProgress(CurrentProgressMs) >= 1)
                        playedWidth += ch.LayoutRect.Width;
                    else
                        break;
                }
            }

            float progressInRegion = Math.Clamp((float)(playedWidth / subLineRegion.LayoutBounds.Width), 0f, 1f);
            float fadeProgressInRegion = 1f / subLineRegion.CharacterCount * 0.5f;

            if (subLineRegion.CharacterIndex >= Line.PrimaryRenderChars.Count) return;
            float firstCharProgressInRegion = Math.Clamp((float)Line.PrimaryRenderChars[subLineRegion.CharacterIndex].GetPlayProgress(CurrentProgressMs), 0f, 1f);

            // RenderLyricsRegion 缓存
            var region = Line.RenderLyricsRegions[regionIndex];

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

            bool hasStroke = Line.CachedStroke != null && region.FinalStrokeEffect != null && region.CombinedEffect != null;

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

            if (!LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled && !LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled && !LyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled)
            {
                ds.DrawImage(finalOutputImage);
            }
            else
            {
                int endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                for (int i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                {
                    DrawSingleCharacter(ds, i, finalOutputImage);
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
        private void DrawSingleCharacter(CanvasDrawingSession ds, int charIndex, ICanvasImage source)
        {
            if (LyricsWindowStatus == null) return;
            if (Line == null) return;
            if (Line.PrimaryTextLayout == null) return;
            if (charIndex >= Line.PrimaryRenderChars.Count) return;

            RenderLyricsChar renderChar = Line.PrimaryRenderChars[charIndex];

            var rect = renderChar.LayoutRect;
            var sourceCharRect = new Rect(
                rect.X + Line.PrimaryPosition.X,
                rect.Y + Line.PrimaryPosition.Y,
                rect.Width,
                rect.Height
            );

            double scale = renderChar.ScaleTransition.Value;
            double glow = renderChar.GlowTransition.Value;
            double floatOffset = renderChar.FloatTransition.Value;

            var destCharRect = sourceCharRect.Scale(scale).AddY(floatOffset);

            if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            {
                destCharRect = destCharRect.AddX(Line.PrimaryXOffsetTransition.Value);
            }

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
