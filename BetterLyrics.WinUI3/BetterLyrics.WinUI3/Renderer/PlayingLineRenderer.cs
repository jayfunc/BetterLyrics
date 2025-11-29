using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
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
using Windows.Foundation;
using Windows.UI;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.Renderer
{
    public class PlayingLineRenderer
    {
        public void Draw(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            ICanvasImage textOnlyLayer,
            LyricsLine line,
            LinePlaybackState playbackState,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            DrawPhonetic(ds, textOnlyLayer, line);
            DrawOriginalText(control, ds, textOnlyLayer, line, playbackState, bgColor, fgColor, settings);
            DrawTranslated(ds, textOnlyLayer, line);
        }

        private void DrawPhonetic(CanvasDrawingSession ds, ICanvasImage source, LyricsLine line)
        {
            if (line.PhoneticCanvasTextLayout == null) return;

            var opacity = line.OpacityTransition.Value * 0.3;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.PhoneticCanvasTextLayout.LayoutBounds;

            var destRect = new Rect(
                bounds.X + line.PhoneticPosition.X,
                bounds.Y + line.PhoneticPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using (var blurEffect = new GaussianBlurEffect
            {
                BlurAmount = (float)blur,
                Source = source,
                BorderMode = EffectBorderMode.Soft
            })
            {
                ds.DrawImage(blurEffect, destRect, destRect, (float)opacity);
            }
        }

        private void DrawTranslated(CanvasDrawingSession ds, ICanvasImage source, LyricsLine line)
        {
            if (line.TranslatedCanvasTextLayout == null) return;

            var opacity = line.OpacityTransition.Value * 0.3;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.TranslatedCanvasTextLayout.LayoutBounds;

            var destRect = new Rect(
                bounds.X + line.TranslatedPosition.X,
                bounds.Y + line.TranslatedPosition.Y,
                bounds.Width,
                bounds.Height
            );

            using (var blurEffect = new GaussianBlurEffect
            {
                BlurAmount = (float)blur,
                Source = source,
                BorderMode = EffectBorderMode.Soft
            })
            {
                ds.DrawImage(blurEffect, destRect, destRect, (float)opacity);
            }
        }

        private void DrawOriginalText(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            LyricsLine line,
            LinePlaybackState state,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            if (line.OriginalCanvasTextLayout == null) return;

            var opacity = line.OpacityTransition.Value;

            var curCharIndex = state.SyllableStartIndex + state.SyllableLength * state.SyllableProgress;
            float fadeWidth = (1f / Math.Max(1, line.OriginalText.Length)) * 0.5f;

            var lineRegions = line.OriginalCanvasTextLayout.GetCharacterRegions(0, line.OriginalText.Length);

            foreach (var subLineRegion in lineRegions)
            {
                DrawSubLineRegion(resourceCreator, ds, source, line, subLineRegion, curCharIndex, fadeWidth, opacity, bgColor, fgColor, state, settings);
            }
        }

        private void DrawSubLineRegion(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            LyricsLine line,
            CanvasTextLayoutRegion subLineRegion,
            double curCharIndex,
            float fadeWidth,
            double opacity,
            Color bgColor,
            Color fgColor,
            LinePlaybackState state,
            LyricsEffectSettings settings)
        {
            var blur = line.BlurAmountTransition.Value;

            var subLineLayoutBounds = subLineRegion.LayoutBounds;
            Rect subLineRect = new(
                subLineLayoutBounds.X + line.OriginalPosition.X,
                subLineLayoutBounds.Y + line.OriginalPosition.Y,
                subLineLayoutBounds.Width,
                subLineLayoutBounds.Height
            );

            using (var maskLayer = new CanvasCommandList(resourceCreator))
            {
                using (var maskLayerDs = maskLayer.CreateDrawingSession())
                {
                    float progressInRegion = (float)((curCharIndex - subLineRegion.CharacterIndex) / subLineRegion.CharacterCount);
                    progressInRegion = Math.Clamp(progressInRegion, 0, 1 + fadeWidth);

                    var stop1 = fgColor.WithAlpha((byte)(255 * opacity));
                    var stop2 = bgColor.WithAlpha((byte)(255 * Math.Min(0.3, opacity)));

                    using (var maskBrush = new CanvasLinearGradientBrush(resourceCreator,
                        [
                            new CanvasGradientStop { Position = 0, Color = stop1 },
                            new CanvasGradientStop { Position = progressInRegion, Color = stop1 },
                            new CanvasGradientStop { Position = progressInRegion + fadeWidth, Color = stop2 },
                            new CanvasGradientStop { Position = 1 + fadeWidth, Color = stop2 }
                        ]))
                    {
                        maskBrush.StartPoint = new Vector2((float)subLineRect.X, (float)subLineRect.Y);
                        maskBrush.EndPoint = new Vector2((float)(subLineRect.X + subLineRect.Width), (float)subLineRect.Y);
                        maskLayerDs.FillRectangle(subLineRect, maskBrush);
                    }
                }

                using var cropEffect = new CropEffect
                {
                    Source = source,
                    SourceRectangle = subLineRect,
                    BorderMode = EffectBorderMode.Soft
                };
                using (var textWithColorLayer = new CompositeEffect
                {
                    Mode = CanvasComposite.DestinationIn,
                    Sources = { maskLayer, cropEffect }
                })
                using (var textWithBlurLayer = new GaussianBlurEffect
                {
                    Source = textWithColorLayer,
                    BorderMode = EffectBorderMode.Soft,
                    BlurAmount = (float)blur,
                })
                {
                    int endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                    for (int i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                    {
                        DrawSingleCharacter(ds, line, i, curCharIndex, textWithBlurLayer, state, settings);
                    }
                }
            }
        }

        private void DrawSingleCharacter(
            CanvasDrawingSession ds,
            LyricsLine line,
            int charIndex,
            double exactProgressIndex,
            ICanvasImage maskedSource,
            LinePlaybackState state,
            LyricsEffectSettings settings)
        {
            var curCharIndexInt = (int)Math.Floor(exactProgressIndex);
            if (line.OriginalCanvasTextLayout == null) return;

            var charRegions = line.OriginalCanvasTextLayout.GetCharacterRegions(charIndex, 1);
            if (charRegions.Length == 0) return;
            var charRegion = charRegions[0];
            var charLayoutBounds = charRegion.LayoutBounds;

            var sourceCharRect = new Rect(
                charLayoutBounds.X + line.OriginalPosition.X,
                charLayoutBounds.Y + line.OriginalPosition.Y,
                charLayoutBounds.Width,
                charLayoutBounds.Height
            );

            double floatOffset = 0;
            double scale = 1;
            double glow = 0;

            if (settings.IsLyricsFloatAnimationEnabled)
            {
                double targetFloatOffset = sourceCharRect.Height * 0.05;
                if (charIndex < curCharIndexInt) floatOffset = 0;
                else if (charIndex == curCharIndexInt)
                {
                    var p = exactProgressIndex - curCharIndexInt;
                    floatOffset = -targetFloatOffset + p * targetFloatOffset;
                }
                else floatOffset = -targetFloatOffset;
            }

            var parentSyllable = line.LyricsSyllables.FirstOrDefault(x => x.StartIndex <= charIndex && charIndex < x.StartIndex + x.Text.Length);

            if (parentSyllable != null && parentSyllable.IsLongDuration && parentSyllable.StartIndex == state.SyllableStartIndex)
            {
                if (settings.IsLyricsScaleEffectEnabled)
                {
                    scale += Math.Sin(state.SyllableProgress * Math.PI) * 0.15;
                }
                if (settings.IsLyricsGlowEffectEnabled)
                {
                    glow = Math.Sin(state.SyllableProgress * Math.PI) * sourceCharRect.Height * 0.2;
                }
            }

            var destCharRect = sourceCharRect.Scale(scale).AddY(-floatOffset);

            using (var singleCharCrop = new CropEffect
            {
                Source = maskedSource,
                SourceRectangle = sourceCharRect,
                BorderMode = EffectBorderMode.Soft
            })
            {
                // 这里不做 glow > 0 的判断而总是加入辉光效果是为了平滑不断层
                using (var glowEffect = new GaussianBlurEffect
                {
                    Source = singleCharCrop,
                    BlurAmount = (float)glow,
                    BorderMode = EffectBorderMode.Soft
                })
                {
                    ds.DrawImage(glowEffect, destCharRect.Extend(sourceCharRect.Height), sourceCharRect.Extend(sourceCharRect.Height));
                }

                ds.DrawImage(singleCharCrop, destCharRect, sourceCharRect);
            }
        }
    }

}
