using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Linq;
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
                DrawSubLineRegion(resourceCreator, ds, source, line, subLineRegion, curCharIndex, fadeWidth, bgColor, fgColor, state, settings);
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
            LinePlaybackState state,
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
                            DrawSingleCharacter(ds, line, i, curCharIndex, textWithOpacityLayer, state, settings);
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
        /// <param name="charIndex">遍历的字符索引</param>
        /// <param name="exactProgressIndex">当前播放字符相对于整行的索引</param>
        /// <param name="source"></param>
        /// <param name="state"></param>
        /// <param name="settings"></param>
        private void DrawSingleCharacter(
            CanvasDrawingSession ds,
            RenderLyricsLine line,
            int charIndex,
            double exactProgressIndex,
            ICanvasImage source,
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

            bool drawGlow = false;

            if (settings.IsLyricsFloatAnimationEnabled)
            {
                double targetFloatOffset;
                if (settings.IsLyricsFloatAnimationAmountAutoAdjust)
                {
                    targetFloatOffset = sourceCharRect.Height * 0.1;
                }
                else
                {
                    targetFloatOffset = settings.LyricsFloatAnimationAmount;
                }

                // 已经浮完了的
                if (charIndex < curCharIndexInt)
                {
                    floatOffset = 0;
                }
                // 正在浮的
                else if (charIndex == curCharIndexInt)
                {
                    var p = exactProgressIndex - curCharIndexInt;
                    floatOffset = -targetFloatOffset + p * targetFloatOffset;
                }
                // 还没浮的
                else
                {
                    floatOffset = -targetFloatOffset;
                }

                // 制造句间上浮过度动画，这里用任何一个 Transition 都行，主要是获取当前行的进入视野的 Progress
                floatOffset *= line.YOffsetTransition.Progress;
            }

            var parentSyllable = line.LyricsSyllables.FirstOrDefault(x => x.StartIndex <= charIndex && charIndex < x.StartIndex + x.Text.Length);

            if (settings.IsLyricsScaleEffectEnabled)
            {
                if (parentSyllable != null && parentSyllable.StartIndex == state.SyllableStartIndex)
                {
                    if (parentSyllable.DurationMs >= settings.LyricsScaleEffectLongSyllableDuration)
                    {
                        if (settings.IsLyricsScaleEffectAmountAutoAdjust)
                        {
                            scale += Math.Sin(state.SyllableProgress * Math.PI) * 0.15;
                        }
                        else
                        {
                            scale += Math.Sin(state.SyllableProgress * Math.PI) * (settings.LyricsScaleEffectAmount / 100.0 - 1);
                        }
                    }
                }
            }

            if (settings.IsLyricsGlowEffectEnabled)
            {
                double maxGlow;
                if (settings.IsLyricsGlowEffectAmountAutoAdjust)
                {
                    maxGlow = sourceCharRect.Height * 0.2;
                }
                else
                {
                    maxGlow = settings.LyricsGlowEffectAmount;
                }
                switch (settings.LyricsGlowEffectScope)
                {
                    case Enums.LyricsEffectScope.LongDurationSyllable:
                        if (parentSyllable != null && parentSyllable.StartIndex == state.SyllableStartIndex)
                        {
                            if (parentSyllable.DurationMs >= settings.LyricsGlowEffectLongSyllableDuration)
                            {
                                glow = maxGlow * Math.Sin(state.SyllableProgress * Math.PI);
                                drawGlow = true;
                            }
                        }
                        break;
                    case Enums.LyricsEffectScope.LineStartToCurrentChar:
                        // 已经唱了的
                        if (charIndex < curCharIndexInt)
                        {
                            glow = maxGlow;
                            drawGlow = true;
                        }
                        // 正在唱的
                        else if (charIndex == curCharIndexInt)
                        {
                            var p = exactProgressIndex - curCharIndexInt;
                            glow = p * maxGlow;
                            drawGlow = true;
                        }
                        // 还没唱的
                        else { }
                        glow *= Math.Clamp(line.OriginalText.Length - exactProgressIndex, 0, 1);
                        break;
                    default:
                        break;
                }
            }

            var destCharRect = sourceCharRect.Scale(scale).AddY(-floatOffset);

            if (drawGlow)
            {
                var sourcePlayedCharRect = new Rect(
                    sourceCharRect.X,
                    sourceCharRect.Y,
                    sourceCharRect.Width,
                    sourceCharRect.Height
                );

                if (charIndex == curCharIndexInt)
                {
                    var p = exactProgressIndex - curCharIndexInt;
                    sourcePlayedCharRect.Width *= p;
                }
                else if (charIndex > curCharIndexInt)
                {
                    sourcePlayedCharRect.Width = 0;
                }

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
                    ds.DrawImage(glowEffect, destCharRect.Extend(sourceCharRect.Height), sourceCharRect.Extend(sourceCharRect.Height));
                }
            }

            ds.DrawImage(source, destCharRect, sourceCharRect);
        }
    }

}
