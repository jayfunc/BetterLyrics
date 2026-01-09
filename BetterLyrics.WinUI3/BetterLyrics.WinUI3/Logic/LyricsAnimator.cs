using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using DevWinUI;
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

                bool isSecondaryLinePlaying = line.GetIsPlaying(currentPositionMs);
                bool isSecondaryLinePlayingChanged = line.IsPlayingLastFrame != isSecondaryLinePlaying;
                line.IsPlayingLastFrame = isSecondaryLinePlaying;

                // 行动画
                if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged || isSecondaryLinePlayingChanged)
                {
                    int lineCountDelta = i - primaryPlayingLineIndex;
                    double distanceFromPlayingLine = Math.Abs(line.PrimaryPosition.Y - currentPlayingLine.PrimaryPosition.Y);

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
                    line.BlurAmountTransition.Start(
                        (isMouseScrolling || isSecondaryLinePlaying) ? 0 :
                        (lyricsEffect.IsLyricsBlurEffectEnabled ? (5 * distanceFactor) : 0));

                    line.ScaleTransition.SetDuration(yScrollDuration);
                    line.ScaleTransition.SetDelay(yScrollDelay);
                    line.ScaleTransition.Start(
                        isSecondaryLinePlaying ? _highlightedScale :
                        (lyricsEffect.IsLyricsOutOfSightEffectEnabled ?
                        (_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)) :
                        _highlightedScale));

                    line.PhoneticOpacityTransition.SetDuration(yScrollDuration);
                    line.PhoneticOpacityTransition.SetDelay(yScrollDelay);
                    line.PhoneticOpacityTransition.Start(
                        isSecondaryLinePlaying ? phoneticOpacity :
                        CalculateTargetOpacity(phoneticOpacity, phoneticOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    // 原文不透明度（已播放）
                    line.PlayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.PlayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.PlayedOriginalOpacityTransition.Start(
                        isSecondaryLinePlaying ? 1.0 :
                        CalculateTargetOpacity(originalOpacity, 1.0, distanceFactor, isMouseScrolling, lyricsEffect));
                    // 原文不透明度（未播放）
                    line.UnplayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.UnplayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.UnplayedOriginalOpacityTransition.Start(
                        isSecondaryLinePlaying ? originalOpacity :
                        CalculateTargetOpacity(originalOpacity, originalOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.TranslatedOpacityTransition.SetDuration(yScrollDuration);
                    line.TranslatedOpacityTransition.SetDelay(yScrollDelay);
                    line.TranslatedOpacityTransition.Start(
                        isSecondaryLinePlaying ? translatedOpacity :
                        CalculateTargetOpacity(translatedOpacity, translatedOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.ColorTransition.SetDuration(yScrollDuration);
                    line.ColorTransition.SetDelay(yScrollDelay);
                    line.ColorTransition.Start(isSecondaryLinePlaying ? fgColor : bgColor);

                    line.AngleTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.AngleTransition.SetDuration(yScrollDuration);
                    line.AngleTransition.SetDelay(yScrollDelay);
                    line.AngleTransition.Start(
                        (lyricsEffect.IsFanLyricsEnabled && !isMouseScrolling) ?
                        Math.PI * (lyricsEffect.FanLyricsAngle / 180.0) * distanceFactor * (i > primaryPlayingLineIndex ? 1 : -1) :
                        0);

                    line.YOffsetTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.YOffsetTransition.SetDuration(yScrollDuration);
                    line.YOffsetTransition.SetDelay(yScrollDelay);
                    // 设计之初是当 isLayoutChanged 为真时 jumpTo
                    // 但考虑到动画视觉，强制使用动画
                    line.YOffsetTransition.Start(targetYScrollOffset);
                }

                var maxAnimationDurationMs = Math.Max(line.EndMs - currentPositionMs, 0);

                // 字符动画
                foreach (var renderChar in line.PrimaryRenderChars)
                {
                    renderChar.ProgressPlayed = renderChar.GetPlayProgress(currentPositionMs);

                    bool isCharPlaying = renderChar.GetIsPlaying(currentPositionMs);
                    bool isCharPlayingChanged = renderChar.IsPlayingLastFrame != isCharPlaying;

                    if (isSecondaryLinePlayingChanged || isCharPlayingChanged)
                    {
                        if (lyricsEffect.IsLyricsGlowEffectEnabled)
                        {
                            double targetGlow = lyricsEffect.IsLyricsGlowEffectAmountAutoAdjust ? renderChar.LayoutRect.Height * 0.2 : lyricsEffect.LyricsGlowEffectAmount;
                            switch (lyricsEffect.LyricsGlowEffectScope)
                            {
                                case Enums.LyricsEffectScope.LineStartToCurrentChar:
                                    if (isSecondaryLinePlayingChanged && isSecondaryLinePlaying)
                                    {
                                        var stepInOutDuration = Math.Min(Time.AnimationDuration.TotalMilliseconds, maxAnimationDurationMs) / 2.0 / 1000.0;
                                        var stepLastingDuration = Math.Max(maxAnimationDurationMs / 1000.0 - stepInOutDuration * 2, 0);
                                        renderChar.GlowTransition.Start(
                                            new Models.Keyframe<double>(targetGlow, stepInOutDuration),
                                            new Models.Keyframe<double>(targetGlow, stepLastingDuration),
                                            new Models.Keyframe<double>(0, stepInOutDuration)
                                        );
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
                                renderChar.FloatTransition.Start(isSecondaryLinePlaying ? targetFloat : 0);
                            }
                            if (isCharPlayingChanged)
                            {
                                renderChar.FloatTransition.SetDurationMs(Math.Min(lyricsEffect.LyricsFloatAnimationDuration, maxAnimationDurationMs));
                                renderChar.FloatTransition.Start(0);
                            }
                        }

                        if (isCharPlayingChanged)
                        {
                            renderChar.IsPlayingLastFrame = isCharPlaying;
                        }
                    }
                }

                // 音节动画
                foreach (var syllable in line.PrimaryRenderSyllables)
                {
                    bool isSyllablePlaying = syllable.GetIsPlaying(currentPositionMs);
                    bool isSyllablePlayingChanged = syllable.IsPlayingLastFrame != isSyllablePlaying;

                    if (isSyllablePlayingChanged)
                    {
                        var syllableHeight = syllable.ChildrenRenderLyricsChars.FirstOrDefault()?.LayoutRect.Height ?? 0;

                        if (lyricsEffect.IsLyricsScaleEffectEnabled)
                        {
                            double targetScale =
                                lyricsEffect.IsLyricsScaleEffectAmountAutoAdjust ? 1.15 : lyricsEffect.LyricsScaleEffectAmount / 100.0;

                            foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                            {
                                if (syllable.DurationMs >= lyricsEffect.LyricsScaleEffectLongSyllableDuration)
                                {
                                    if (isSyllablePlaying)
                                    {
                                        var stepDuration = Math.Min(syllable.DurationMs, maxAnimationDurationMs) / 2.0 / 1000.0;
                                        renderChar.ScaleTransition.Start(
                                            new Models.Keyframe<double>(targetScale, stepDuration),
                                            new Models.Keyframe<double>(1.0, stepDuration)
                                        );
                                    }
                                }
                            }
                        }

                        if (lyricsEffect.IsLyricsGlowEffectEnabled)
                        {
                            double targetGlow = lyricsEffect.IsLyricsGlowEffectAmountAutoAdjust ? syllableHeight * 0.2 : lyricsEffect.LyricsGlowEffectAmount;
                            switch (lyricsEffect.LyricsGlowEffectScope)
                            {
                                case Enums.LyricsEffectScope.LongDurationSyllable:
                                    if (syllable.DurationMs >= lyricsEffect.LyricsGlowEffectLongSyllableDuration)
                                    {
                                        foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                                        {
                                            if (isSyllablePlaying)
                                            {
                                                var stepDuration = Math.Min(syllable.DurationMs, maxAnimationDurationMs) / 2.0 / 1000.0;
                                                renderChar.GlowTransition.Start(
                                                    new Models.Keyframe<double>(targetGlow, stepDuration),
                                                    new Models.Keyframe<double>(0, stepDuration)
                                                );
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }

                        syllable.IsPlayingLastFrame = isSyllablePlaying;
                    }
                }

                // 更新动画
                foreach (var renderChar in line.PrimaryRenderChars)
                {
                    renderChar.Update(elapsedTime);
                }

                line.Update(elapsedTime);
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
