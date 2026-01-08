using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
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
        public void Draw(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            ICanvasImage textOnlyLayer,
            RenderLyricsLine line,
            LinePlaybackState playbackState,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            DrawPhonetic(ds, textOnlyLayer, line);
            DrawOriginalText(control, ds, textOnlyLayer, line, playbackState, bgColor, fgColor, settings);
            DrawTranslated(ds, textOnlyLayer, line);
        }

        private void DrawPhonetic(CanvasDrawingSession ds, ICanvasImage source, RenderLyricsLine line)
        {
            if (line.PhoneticCanvasTextLayout == null) return;

            var opacity = line.PhoneticOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.PhoneticCanvasTextLayout.LayoutBounds;

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.PhoneticPosition.X,
                bounds.Y + line.PhoneticPosition.Y,
                bounds.Width,
                bounds.Height
            );

            ds.DrawImage(new OpacityEffect
            {
                Source = new GaussianBlurEffect
                {
                    BlurAmount = (float)blur,
                    Source = new CropEffect
                    {
                        Source = source,
                        BorderMode = EffectBorderMode.Hard,
                        SourceRectangle = destRect,
                    },
                    BorderMode = EffectBorderMode.Soft
                },
                Opacity = (float)opacity,
            });
        }

        private void DrawTranslated(CanvasDrawingSession ds, ICanvasImage source, RenderLyricsLine line)
        {
            if (line.TranslatedCanvasTextLayout == null) return;

            var opacity = line.TranslatedOpacityTransition.Value;
            var blur = line.BlurAmountTransition.Value;
            var bounds = line.TranslatedCanvasTextLayout.LayoutBounds;

            if (double.IsNaN(opacity)) return;

            var destRect = new Rect(
                bounds.X + line.TranslatedPosition.X,
                bounds.Y + line.TranslatedPosition.Y,
                bounds.Width,
                bounds.Height
            );

            ds.DrawImage(new OpacityEffect
            {
                Source = new GaussianBlurEffect
                {
                    BlurAmount = (float)blur,
                    Source = new CropEffect
                    {
                        Source = source,
                        BorderMode = EffectBorderMode.Hard,
                        SourceRectangle = destRect,
                    },
                    BorderMode = EffectBorderMode.Soft
                },
                Opacity = (float)opacity,
            });
        }

        private void DrawOriginalText(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            RenderLyricsLine line,
            LinePlaybackState state,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            if (line.OriginalCanvasTextLayout == null) return;

            var curCharIndex = state.SyllableStartIndex + state.SyllableLength * state.SyllableProgress;
            float fadeWidth = (1f / Math.Max(1, line.OriginalText.Length)) * 0.5f;

            var lineRegions = line.OriginalCanvasTextLayout.GetCharacterRegions(0, line.OriginalText.Length);

            foreach (var subLineRegion in lineRegions)
            {
                DrawSubLineRegion(resourceCreator, ds, source, line, subLineRegion, curCharIndex, fadeWidth, bgColor, fgColor, settings);
            }
        }

        private void DrawSubLineRegion(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            ICanvasImage source,
            RenderLyricsLine line,
            CanvasTextLayoutRegion subLineRegion,
            double curCharIndex,
            float fadeWidth,
            Color bgColor,
            Color fgColor,
            LyricsEffectSettings settings)
        {
            var playedOpacity = line.PlayedOriginalOpacityTransition.Value;
            var unplayedOpacity = line.UnplayedOriginalOpacityTransition.Value;

            var subLineLayoutBounds = subLineRegion.LayoutBounds;
            Rect subLineRect = new(
                subLineLayoutBounds.X + line.OriginalPosition.X,
                subLineLayoutBounds.Y + line.OriginalPosition.Y,
                subLineLayoutBounds.Width,
                subLineLayoutBounds.Height
            );

            using (var gradientLayer = new CanvasCommandList(resourceCreator))
            {
                using (var gradientLayerDs = gradientLayer.CreateDrawingSession())
                {
                    float progressInRegion = (float)((curCharIndex - subLineRegion.CharacterIndex) / subLineRegion.CharacterCount);
                    progressInRegion = Math.Clamp(progressInRegion, 0, 1 + fadeWidth);

                    var stop1 = fgColor.WithAlpha((byte)(255 * playedOpacity));
                    var stop2 = bgColor.WithAlpha((byte)(255 * unplayedOpacity));

                    using (var gradientBrush = new CanvasLinearGradientBrush(resourceCreator,
                        [
                            new CanvasGradientStop { Position = 0, Color = stop1 },
                            new CanvasGradientStop { Position = progressInRegion, Color = stop1 },
                            // 这里做判断是防止子行未播放时左侧出现渐变的问题
                            new CanvasGradientStop { Position = progressInRegion == 0 ? 0 : (progressInRegion + fadeWidth), Color = stop2 },
                            new CanvasGradientStop { Position = 1 + fadeWidth, Color = stop2 }
                        ]))
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
        private void DrawSingleCharacter(
            CanvasDrawingSession ds,
            RenderLyricsLine line,
            int charIndex,
            ICanvasImage source)
        {
            if (charIndex >= line.RenderLyricsOriginalChars.Count) return;

            RenderLyricsChar renderChar = line.RenderLyricsOriginalChars[charIndex];

            var rect = renderChar.LayoutRect;
            var sourceCharRect = new Rect(
                rect.X + line.OriginalPosition.X,
                rect.Y + line.OriginalPosition.Y,
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
                using (var glowEffect = new GaussianBlurEffect
                {
                    Source = new CropEffect
                    {
                        Source = source,
                        SourceRectangle = sourcePlayedCharRect,
                        BorderMode = EffectBorderMode.Hard
                    },
                    BlurAmount = (float)glow,
                    BorderMode = EffectBorderMode.Soft
                })
                {
                    ds.DrawImage(glowEffect, destCharRect.Extend(destCharRect.Height), sourceCharRect.Extend(sourceCharRect.Height));
                }
            }

            // Draw the top layer
            ds.DrawImage(source, destCharRect, sourceCharRect);
        }
    }

}
