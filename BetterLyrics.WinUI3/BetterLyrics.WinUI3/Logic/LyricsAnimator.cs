using ATL;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using DevWinUI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsAnimator
    {
        private readonly double _defaultScale = 0.75f;
        private readonly double _highlightedScale = 1.0f;

        public void UpdateLines(
            IList<RenderLyricsLine>? lines,
            int startIndex,
            int endIndex,
            int primaryPlayingLineIndex,
            double canvasHeight,
            double targetYScrollOffset,
            double playingLineTopOffsetFactor,
            LyricsStyleSettings lyricsStyle,
            LyricsEffectSettings lyricsEffect,
            ValueTransition<double> canvasYScrollTransition,
            Color bgColor,
            Color fgColor,
            TimeSpan elapsedTime,
            bool isMouseScrolling,
            bool isLayoutChanged,
            bool isPrimaryPlayingLineChanged,
            bool isMouseScrollingChanged,
            double currentPositionMs
        )
        {
            if (lines == null) return;

            var currentPlayingLine = lines.ElementAtOrDefault(primaryPlayingLineIndex);
            if (currentPlayingLine == null) return;

            var phoneticOpacity = lyricsStyle.PhoneticLyricsOpacity / 100.0;
            var originalOpacity = lyricsStyle.OriginalLyricsOpacity / 100.0;
            var translatedOpacity = lyricsStyle.TranslatedLyricsOpacity / 100.0;

            for (int i = startIndex; i <= endIndex + 1; i++)
            {
                var line = lines.ElementAtOrDefault(i);
                if (line == null) continue;

                bool isSecondaryLinePlaying = line.StartMs <= currentPositionMs && currentPositionMs <= line.EndMs;
                if (i == primaryPlayingLineIndex) isSecondaryLinePlaying = true;
                bool isSecondaryLinePlayingChanged = line.IsPlayingLastFrame != isSecondaryLinePlaying;
                line.IsPlayingLastFrame = isSecondaryLinePlaying;

                if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged || isSecondaryLinePlayingChanged)
                {
                    int lineCountDelta = i - primaryPlayingLineIndex;
                    double distanceFromPlayingLine = Math.Abs(line.OriginalPosition.Y - currentPlayingLine.OriginalPosition.Y);

                    double distanceFactor;
                    if (lineCountDelta < 0)
                    {
                        distanceFactor = Math.Clamp(distanceFromPlayingLine / (canvasHeight * playingLineTopOffsetFactor), 0, 1);
                    }
                    else
                    {
                        distanceFactor = Math.Clamp(distanceFromPlayingLine / (canvasHeight * (1 - playingLineTopOffsetFactor)), 0, 1);
                    }

                    double yScrollDuration;
                    double yScrollDelay;

                    if (lineCountDelta < 0)
                    {
                        yScrollDuration =
                            canvasYScrollTransition.DurationSeconds +
                            distanceFactor * (lyricsEffect.LyricsScrollTopDuration / 1000.0 - canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * lyricsEffect.LyricsScrollTopDelay / 1000.0;
                    }
                    else if (lineCountDelta == 0)
                    {
                        yScrollDuration = canvasYScrollTransition.DurationSeconds;
                        yScrollDelay = 0;
                    }
                    else
                    {
                        yScrollDuration =
                            canvasYScrollTransition.DurationSeconds +
                            distanceFactor * (lyricsEffect.LyricsScrollBottomDuration / 1000.0 - canvasYScrollTransition.DurationSeconds);
                        yScrollDelay = distanceFactor * lyricsEffect.LyricsScrollBottomDelay / 1000.0;
                    }

                    line.BlurAmountTransition.SetDuration(yScrollDuration);
                    line.BlurAmountTransition.SetDelay(yScrollDelay);
                    line.BlurAmountTransition.StartTransition(
                        (isMouseScrolling || isSecondaryLinePlaying) ? 0 :
                        (lyricsEffect.IsLyricsBlurEffectEnabled ? (5 * distanceFactor) : 0));

                    line.ScaleTransition.SetDuration(yScrollDuration);
                    line.ScaleTransition.SetDelay(yScrollDelay);
                    line.ScaleTransition.StartTransition(
                        isSecondaryLinePlaying ? _highlightedScale :
                        (lyricsEffect.IsLyricsOutOfSightEffectEnabled ?
                        (_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)) :
                        _highlightedScale));

                    line.PhoneticOpacityTransition.SetDuration(yScrollDuration);
                    line.PhoneticOpacityTransition.SetDelay(yScrollDelay);
                    line.PhoneticOpacityTransition.StartTransition(
                        isSecondaryLinePlaying ? phoneticOpacity :
                        CalculateTargetOpacity(phoneticOpacity, phoneticOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    // 原文不透明度（已播放）
                    line.PlayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.PlayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.PlayedOriginalOpacityTransition.StartTransition(
                        isSecondaryLinePlaying ? 1.0 :
                        CalculateTargetOpacity(originalOpacity, 1.0, distanceFactor, isMouseScrolling, lyricsEffect));
                    // 原文不透明度（未播放）
                    line.UnplayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.UnplayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.UnplayedOriginalOpacityTransition.StartTransition(
                        isSecondaryLinePlaying ? originalOpacity :
                        CalculateTargetOpacity(originalOpacity, originalOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.TranslatedOpacityTransition.SetDuration(yScrollDuration);
                    line.TranslatedOpacityTransition.SetDelay(yScrollDelay);
                    line.TranslatedOpacityTransition.StartTransition(
                        isSecondaryLinePlaying ? translatedOpacity :
                        CalculateTargetOpacity(translatedOpacity, translatedOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.ColorTransition.SetDuration(yScrollDuration);
                    line.ColorTransition.SetDelay(yScrollDelay);
                    line.ColorTransition.StartTransition(isSecondaryLinePlaying ? fgColor : bgColor);

                    line.AngleTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.AngleTransition.SetDuration(yScrollDuration);
                    line.AngleTransition.SetDelay(yScrollDelay);
                    line.AngleTransition.StartTransition(
                        (lyricsEffect.IsFanLyricsEnabled && !isMouseScrolling) ?
                        Math.PI * (lyricsEffect.FanLyricsAngle / 180.0) * distanceFactor * (i > primaryPlayingLineIndex ? 1 : -1) :
                        0);

                    line.YOffsetTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.YOffsetTransition.SetDuration(yScrollDuration);
                    line.YOffsetTransition.SetDelay(yScrollDelay);
                    // 设计之初是当 isLayoutChanged 为真时 jumpTo
                    // 但考虑到动画视觉，强制使用动画
                    line.YOffsetTransition.StartTransition(targetYScrollOffset);
                }

                if (line.RenderLyricsOriginalChars != null)
                {
                    foreach (var renderChar in line.RenderLyricsOriginalChars)
                    {
                        var syllable = line.LyricsSyllables.FirstOrDefault(x => x.StartIndex <= renderChar.Index && renderChar.Index <= x.EndIndex);
                        if (syllable == null) continue;

                        var avgCharDuration = syllable.DurationMs / syllable.Length;
                        if (avgCharDuration == null || avgCharDuration == 0) continue;

                        var charStartMs = syllable.StartMs + (renderChar.Index - syllable.StartIndex) * avgCharDuration.Value;
                        var charEndMs = charStartMs + avgCharDuration;
                        var progressPlayed = (currentPositionMs - charStartMs) / avgCharDuration.Value;
                        progressPlayed = Math.Clamp(progressPlayed, 0, 1);
                        renderChar.ProgressPlayed = progressPlayed;

                        bool isCharPlaying = charStartMs <= currentPositionMs && currentPositionMs <= charEndMs;
                        bool isCharPlayingChanged = renderChar.IsPlayingLastFrame != isCharPlaying;

                        if (isSecondaryLinePlayingChanged || isCharPlayingChanged)
                        {
                            if (lyricsEffect.IsLyricsScaleEffectEnabled)
                            {
                                double targetScale =
                                    lyricsEffect.IsLyricsScaleEffectAmountAutoAdjust ? 1.15 : lyricsEffect.LyricsScaleEffectAmount / 100.0;

                                if (isCharPlayingChanged)
                                {
                                    if (syllable.DurationMs >= lyricsEffect.LyricsScaleEffectLongSyllableDuration)
                                    {
                                        renderChar.ScaleTransition.SetDuration((syllable.DurationMs ?? 0) / 1000.0 / 2);
                                        renderChar.ScaleTransition.StartTransition(isCharPlaying ? targetScale : 1);
                                    }
                                }
                            }

                            if (lyricsEffect.IsLyricsGlowEffectEnabled)
                            {
                                double targetGlow = lyricsEffect.IsLyricsGlowEffectAmountAutoAdjust ? renderChar.LayoutRect.Height * 0.2 : lyricsEffect.LyricsGlowEffectAmount;
                                switch (lyricsEffect.LyricsGlowEffectScope)
                                {
                                    case Enums.LyricsEffectScope.LongDurationSyllable:
                                        if (isCharPlayingChanged)
                                        {
                                            if (syllable.DurationMs >= lyricsEffect.LyricsGlowEffectLongSyllableDuration)
                                            {
                                                renderChar.GlowTransition.SetDuration((syllable.DurationMs ?? 0) / 1000.0 / 2);
                                                renderChar.GlowTransition.StartTransition(isCharPlaying ? targetGlow : 0);
                                            }
                                        }
                                        break;
                                    case Enums.LyricsEffectScope.LineStartToCurrentChar:
                                        if (isSecondaryLinePlayingChanged)
                                        {
                                            renderChar.GlowTransition.SetDuration(renderChar.AnimationDuration);
                                            renderChar.GlowTransition.StartTransition(isSecondaryLinePlaying ? targetGlow : 0);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }

                            if (lyricsEffect.IsLyricsFloatAnimationEnabled)
                            {
                                double targetFloat =
                                    lyricsEffect.IsLyricsFloatAnimationAmountAutoAdjust ? renderChar.LayoutRect.Height * 0.1 : lyricsEffect.LyricsFloatAnimationAmount;

                                if (isSecondaryLinePlayingChanged)
                                {
                                    renderChar.FloatTransition.StartTransition(isSecondaryLinePlaying ? targetFloat : 0);
                                }
                                if (isCharPlayingChanged)
                                {
                                    renderChar.FloatTransition.StartTransition(0);
                                }
                            }

                            renderChar.IsPlayingLastFrame = isCharPlaying;
                        }

                        renderChar.ScaleTransition.Update(elapsedTime);
                        renderChar.GlowTransition.Update(elapsedTime);
                        renderChar.FloatTransition.Update(elapsedTime);
                    }
                }

                line.AngleTransition.Update(elapsedTime);
                line.ScaleTransition.Update(elapsedTime);
                line.BlurAmountTransition.Update(elapsedTime);
                line.PhoneticOpacityTransition.Update(elapsedTime);
                line.PlayedOriginalOpacityTransition.Update(elapsedTime);
                line.UnplayedOriginalOpacityTransition.Update(elapsedTime);
                line.TranslatedOpacityTransition.Update(elapsedTime);
                line.YOffsetTransition.Update(elapsedTime);
                line.ColorTransition.Update(elapsedTime);
            }
        }

        private static double CalculateTargetOpacity(double baseOpacity, double baseOpacityWhenZeroDistanceFactor, double distanceFactor, bool isMouseScrolling, LyricsEffectSettings lyricsEffect)
        {
            double targetOpacity;
            if (distanceFactor == 0)
            {
                targetOpacity = baseOpacityWhenZeroDistanceFactor;
            }
            else
            {
                if (isMouseScrolling)
                {
                    targetOpacity = baseOpacity;
                }
                else
                {
                    if (lyricsEffect.IsLyricsFadeOutEffectEnabled)
                    {
                        targetOpacity = (1 - distanceFactor) * baseOpacity;
                    }
                    else
                    {
                        targetOpacity = baseOpacity;
                    }
                }
            }
            return targetOpacity;
        }
    }
}
