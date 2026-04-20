using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Helper.Lyrics
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
            double lyricsWidth,
            double lyricsHeight,
            double targetYScrollOffset,
            double playingLineTopOffsetFactor,
            LyricsStyleSettings lyricsStyle,
            LyricsEffectSettings lyricsEffect,
            ValueTransition<double> canvasYScrollTransition,
            NowPlayingPalette albumArtThemeColors,
            TimeSpan elapsedTime,
            bool isMouseScrolling,
            bool isLayoutChanged,
            bool isPrimaryPlayingLineChanged,
            bool isMouseScrollingChanged,
            bool isArtThemeColorsChanged,
            double currentPositionMs
        )
        {
            if (lines == null || lines.Count == 0) return;

            if (primaryPlayingLineIndex < 0 || primaryPlayingLineIndex >= lines.Count) return;
            var primaryPlayingLine = lines[primaryPlayingLineIndex];

            var autoWrap = lyricsStyle.AutoWrap;

            var phoneticOpacity = lyricsStyle.PhoneticLyricsOpacity / 100.0;
            var originalOpacity = lyricsStyle.UnplayedOriginalLyricsOpacity / 100.0;
            var translatedOpacity = lyricsStyle.TranslatedLyricsOpacity / 100.0;

            double topHeightFactor = lyricsHeight * playingLineTopOffsetFactor;
            double bottomHeightFactor = lyricsHeight * (1 - playingLineTopOffsetFactor);

            double scrollTopDurationSec = lyricsEffect.LyricsScrollTopDuration / 1000.0;
            double scrollTopDelaySec = lyricsEffect.LyricsScrollTopDelay / 1000.0;
            double scrollBottomDurationSec = lyricsEffect.LyricsScrollBottomDuration / 1000.0;
            double scrollBottomDelaySec = lyricsEffect.LyricsScrollBottomDelay / 1000.0;
            double canvasTransDuration = canvasYScrollTransition.DurationSeconds;

            bool isBlurEnabled = lyricsEffect.IsLyricsBlurEffectEnabled;
            bool isOutOfSightEnabled = lyricsEffect.IsLyricsOutOfSightEffectEnabled;
            bool isFanEnabled = lyricsEffect.IsFanLyricsEnabled;
            double fanAngleRad = Math.PI * (lyricsEffect.FanLyricsAngle / 180.0);
            bool isGlowEnabled = lyricsEffect.IsLyricsGlowEffectEnabled;
            bool isFloatEnabled = lyricsEffect.IsLyricsFloatAnimationEnabled;
            bool isScaleEnabled = lyricsEffect.IsLyricsScaleEffectEnabled;

            int safeStart = Math.Max(0, startIndex);
            int safeEnd = Math.Min(lines.Count - 1, endIndex + 1);

            for (int i = safeStart; i <= safeEnd; i++)
            {
                var line = lines[i];
                var lineHeight = line.PrimaryLineHeight;
                if (lineHeight == null || lineHeight <= 0) continue;

                bool isWordAnimationEnabled = lyricsEffect.WordByWordEffectMode switch
                {
                    Enums.WordByWordEffectMode.Auto => line.IsPrimaryHasRealSyllableInfo,
                    Enums.WordByWordEffectMode.Always => true,
                    Enums.WordByWordEffectMode.Never => false,
                    _ => line.IsPrimaryHasRealSyllableInfo
                };

                double targetCharFloat = lyricsEffect.IsLyricsFloatAnimationAmountAutoAdjust
                    ? lineHeight.Value * 0.1
                    : lyricsEffect.LyricsFloatAnimationAmount;
                double targetCharGlow = lyricsEffect.IsLyricsGlowEffectAmountAutoAdjust
                    ? lineHeight.Value * 0.2
                    : lyricsEffect.LyricsGlowEffectAmount;
                double targetCharScale = lyricsEffect.IsLyricsScaleEffectAmountAutoAdjust
                    ? 1.15
                    : lyricsEffect.LyricsScaleEffectAmount / 100.0;

                var maxAnimationDurationMs = Math.Max(line.EndMs ?? 0 - currentPositionMs, 0);

                bool isSecondaryLinePlaying = line.GetIsPlaying(currentPositionMs);
                bool isSecondaryLinePlayingChanged = line.IsPlayingLastFrame != isSecondaryLinePlaying;
                line.IsPlayingLastFrame = isSecondaryLinePlaying;

                var playProgress = line.GetPlayProgress(currentPositionMs);

                // 行动画
                if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged || isSecondaryLinePlayingChanged || isArtThemeColorsChanged)
                {
                    int lineCountDelta = i - primaryPlayingLineIndex;
                    double distanceFromPlayingLine = Math.Abs(line.TopLeftPosition.Y - primaryPlayingLine.TopLeftPosition.Y);

                    double distanceFactor;
                    if (lineCountDelta < 0)
                    {
                        distanceFactor = Math.Clamp(distanceFromPlayingLine / topHeightFactor, 0, 1);
                    }
                    else
                    {
                        distanceFactor = Math.Clamp(distanceFromPlayingLine / bottomHeightFactor, 0, 1);
                    }

                    double yScrollDuration;
                    double yScrollDelay;

                    if (lineCountDelta < 0)
                    {
                        yScrollDuration =
                            canvasTransDuration +
                            distanceFactor * (scrollTopDurationSec - canvasTransDuration);
                        yScrollDelay = distanceFactor * scrollTopDelaySec;
                    }
                    else if (lineCountDelta == 0)
                    {
                        yScrollDuration = canvasTransDuration;
                        yScrollDelay = 0;
                    }
                    else
                    {
                        yScrollDuration =
                            canvasTransDuration +
                            distanceFactor * (scrollBottomDurationSec - canvasTransDuration);
                        yScrollDelay = distanceFactor * scrollBottomDelaySec;
                    }

                    line.BlurAmountTransition.SetDuration(yScrollDuration);
                    line.BlurAmountTransition.SetDelay(yScrollDelay);
                    line.BlurAmountTransition.Start(
                        (isMouseScrolling || isSecondaryLinePlaying) ? 0 :
                        (isBlurEnabled ? (5 * distanceFactor) : 0));

                    line.ScaleTransition.SetDuration(yScrollDuration);
                    line.ScaleTransition.SetDelay(yScrollDelay);
                    line.ScaleTransition.Start(
                        isSecondaryLinePlaying ? _highlightedScale :
                        (isOutOfSightEnabled ?
                        (_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)) :
                        _highlightedScale));

                    line.TertiaryOpacityTransition.SetDuration(yScrollDuration);
                    line.TertiaryOpacityTransition.SetDelay(yScrollDelay);
                    line.TertiaryOpacityTransition.Start(
                        isSecondaryLinePlaying ? phoneticOpacity :
                        CalculateTargetOpacity(phoneticOpacity, phoneticOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    // 原文不透明度（已播放）
                    line.PlayedPrimaryOpacityTransition.SetDuration(yScrollDuration);
                    line.PlayedPrimaryOpacityTransition.SetDelay(yScrollDelay);
                    line.PlayedPrimaryOpacityTransition.Start(
                        isSecondaryLinePlaying ? 1.0 :
                        CalculateTargetOpacity(originalOpacity, 1.0, distanceFactor, isMouseScrolling, lyricsEffect));
                    // 原文不透明度（未播放）
                    line.UnplayedPrimaryOpacityTransition.SetDuration(yScrollDuration);
                    line.UnplayedPrimaryOpacityTransition.SetDelay(yScrollDelay);
                    line.UnplayedPrimaryOpacityTransition.Start(
                        isSecondaryLinePlaying ? originalOpacity :
                        CalculateTargetOpacity(originalOpacity, originalOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.SecondaryOpacityTransition.SetDuration(yScrollDuration);
                    line.SecondaryOpacityTransition.SetDelay(yScrollDelay);
                    line.SecondaryOpacityTransition.Start(
                        isSecondaryLinePlaying ? translatedOpacity :
                        CalculateTargetOpacity(translatedOpacity, translatedOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.PlayedFillColorTransition.SetDuration(yScrollDuration);
                    line.PlayedFillColorTransition.SetDelay(yScrollDelay);
                    line.PlayedFillColorTransition.Start(isSecondaryLinePlaying ? albumArtThemeColors.PlayedCurrentLineFillColor : albumArtThemeColors.NonCurrentLineFillColor);

                    line.UnplayedFillColorTransition.SetDuration(yScrollDuration);
                    line.UnplayedFillColorTransition.SetDelay(yScrollDelay);
                    line.UnplayedFillColorTransition.Start(isSecondaryLinePlaying ? albumArtThemeColors.UnplayedCurrentLineFillColor : albumArtThemeColors.NonCurrentLineFillColor);

                    line.PlayedStrokeColorTransition.SetDuration(yScrollDuration);
                    line.PlayedStrokeColorTransition.SetDelay(yScrollDelay);
                    line.PlayedStrokeColorTransition.Start(isSecondaryLinePlaying ? albumArtThemeColors.PlayedTextStrokeColor : albumArtThemeColors.UnplayedTextStrokeColor);

                    line.UnplayedStrokeColorTransition.SetDuration(yScrollDuration);
                    line.UnplayedStrokeColorTransition.SetDelay(yScrollDelay);
                    line.UnplayedStrokeColorTransition.Start(isSecondaryLinePlaying ? albumArtThemeColors.UnplayedTextStrokeColor : albumArtThemeColors.UnplayedTextStrokeColor);

                    line.AngleTransition.SetInterpolator(canvasYScrollTransition.Interpolator);
                    line.AngleTransition.SetDuration(yScrollDuration);
                    line.AngleTransition.SetDelay(yScrollDelay);
                    line.AngleTransition.Start(
                        (isFanEnabled && !isMouseScrolling) ?
                        fanAngleRad * distanceFactor * (i > primaryPlayingLineIndex ? 1 : -1) :
                        0);

                    if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged)
                    {
                        line.YOffsetTransition.SetInterpolator(canvasYScrollTransition.Interpolator);
                        line.YOffsetTransition.SetDuration(yScrollDuration);
                        line.YOffsetTransition.SetDelay(yScrollDelay);
                        if (isLayoutChanged)
                        {
                            line.YOffsetTransition.JumpTo(targetYScrollOffset);
                        }
                        else
                        {
                            line.YOffsetTransition.Start(targetYScrollOffset);
                        }
                    }
                }

                if (isWordAnimationEnabled)
                {
                    if (isSecondaryLinePlayingChanged)
                    {
                        // 辉光动画（从行首开始到当前）
                        if (isGlowEnabled && lyricsEffect.LyricsGlowEffectScope == Enums.LyricsEffectScope.LineStartToCurrentChar
                             && isSecondaryLinePlaying)
                        {
                            foreach (var renderChar in line.PrimaryRenderChars)
                            {
                                var stepInOutDuration = Math.Min(Time.AnimationDuration.TotalMilliseconds, maxAnimationDurationMs) / 2.0 / 1000.0;
                                var stepLastingDuration = Math.Max(maxAnimationDurationMs / 1000.0 - stepInOutDuration * 2, 0);
                                renderChar.GlowTransition.Start(
                                    new Models.Keyframe<double>(targetCharGlow, stepInOutDuration),
                                    new Models.Keyframe<double>(targetCharGlow, stepLastingDuration),
                                    new Models.Keyframe<double>(0, stepInOutDuration)
                                );
                            }
                        }

                        // 浮动动画（控制整体）
                        if (isFloatEnabled)
                        {
                            foreach (var renderChar in line.PrimaryRenderChars)
                            {
                                if (isSecondaryLinePlaying)
                                {
                                    if (renderChar.EndMs < currentPositionMs)
                                    {
                                        // 确保已播放的部分恢复原位
                                        renderChar.FloatTransition.JumpTo(0);
                                    }
                                    else
                                    {
                                        // 下沉（以便后续上浮）
                                        renderChar.FloatTransition.Start(targetCharFloat);
                                    }
                                }
                                else
                                {
                                    // 恢复初始状态（相当于上浮）
                                    renderChar.FloatTransition.Start(0);
                                }
                            }
                        }
                    }

                    // 浮动动画（控制单个）
                    foreach (var renderChar in line.PrimaryRenderChars)
                    {
                        renderChar.ProgressPlayed = renderChar.GetPlayProgress(currentPositionMs);

                        bool isCharPlaying = renderChar.GetIsPlaying(currentPositionMs);
                        bool isCharPlayingChanged = renderChar.IsPlayingLastFrame != isCharPlaying;

                        if (isCharPlayingChanged)
                        {
                            if (isFloatEnabled)
                            {
                                renderChar.FloatTransition.SetDurationMs(Math.Min(lyricsEffect.LyricsFloatAnimationDuration, maxAnimationDurationMs));
                                renderChar.FloatTransition.Start(0);
                            }

                            renderChar.IsPlayingLastFrame = isCharPlaying;
                        }
                        else
                        {
                            if (!isCharPlaying && currentPositionMs > renderChar.EndMs && renderChar.FloatTransition.Value != 0)
                            {
                                renderChar.FloatTransition.SetDurationMs(Math.Min(lyricsEffect.LyricsFloatAnimationDuration, maxAnimationDurationMs));
                                renderChar.FloatTransition.Start(0);
                            }
                        }
                    }

                    foreach (var syllable in line.PrimaryRenderSyllables)
                    {
                        bool isSyllablePlaying = syllable.GetIsPlaying(currentPositionMs);
                        bool isSyllablePlayingChanged = syllable.IsPlayingLastFrame != isSyllablePlaying;

                        if (isSyllablePlayingChanged)
                        {
                            // 缩放
                            if (isScaleEnabled && isSyllablePlaying)
                            {
                                foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                                {
                                    if (syllable.DurationMs >= lyricsEffect.LyricsScaleEffectLongSyllableDuration)
                                    {
                                        var (inDuration, outDuration) = CalculateSegmentDuration(syllable.DurationMs / 1000.0, maxAnimationDurationMs / 1000.0);
                                        renderChar.ScaleTransition.Start(
                                            new Models.Keyframe<double>(targetCharScale, inDuration),
                                            new Models.Keyframe<double>(1.0, outDuration)
                                        );
                                    }
                                }
                            }

                            // 辉光（长音节）
                            if (isGlowEnabled && isSyllablePlaying && lyricsEffect.LyricsGlowEffectScope == Enums.LyricsEffectScope.LongDurationSyllable
                                && syllable.DurationMs >= lyricsEffect.LyricsGlowEffectLongSyllableDuration)
                            {
                                foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                                {
                                    var (inDuration, outDuration) = CalculateSegmentDuration(syllable.DurationMs / 1000.0, maxAnimationDurationMs / 1000.0);
                                    renderChar.GlowTransition.Start(
                                        new Models.Keyframe<double>(targetCharGlow, inDuration),
                                        new Models.Keyframe<double>(0, outDuration)
                                    );
                                }
                            }

                            syllable.IsPlayingLastFrame = isSyllablePlaying;
                        }
                    }

                    foreach (var renderChar in line.PrimaryRenderChars)
                    {
                        renderChar.Update(elapsedTime);
                    }
                }

                if (!autoWrap)
                {
                    if (isSecondaryLinePlaying)
                    {
                        line.PrimaryXOffsetTransition.JumpTo(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.PrimaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, playProgress));
                        line.SecondaryXOffsetTransition.JumpTo(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.SecondaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, playProgress));
                        line.TertiaryXOffsetTransition.JumpTo(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.TertiaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, playProgress));
                    }
                    if (isSecondaryLinePlayingChanged)
                    {
                        if (!isSecondaryLinePlaying)
                        {
                            line.PrimaryXOffsetTransition.Start(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.PrimaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, 0));
                            line.SecondaryXOffsetTransition.Start(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.SecondaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, 0));
                            line.TertiaryXOffsetTransition.Start(CalculateTargetXOffset(lyricsStyle.LyricsAlignmentType, line.TertiaryTextLayout?.LayoutBounds.Width ?? 0, lyricsWidth, 0));
                        }
                    }
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

        private static double CalculateTargetXOffset(TextAlignmentType textAlignmentType, double actualWidth, double lyricsWidth, double progress)
        {
            var offset = textAlignmentType switch
            {
                TextAlignmentType.Center => (lyricsWidth - actualWidth) / 2,
                TextAlignmentType.Right => lyricsWidth - actualWidth,
                _ => 0,
            };
            offset = -Math.Min(0, offset);
            var progressStartToScroll = lyricsWidth * 0.5 / actualWidth;
            var progressEndToScroll = 1 - progressStartToScroll;
            return -Math.Max((Math.Min(progress, progressEndToScroll) - progressStartToScroll), 0) * actualWidth + offset;
        }

        private static (double InDuration, double OutDuration) CalculateSegmentDuration(double desiredDuration, double maxDuration)
        {
            // 缓入动画时长尽量接近 desiredDuration
            var inDuration = Math.Min(desiredDuration, maxDuration);
            // 缓出动画时长保证合法
            var outDuration = Math.Min(maxDuration - inDuration, Time.AnimationDuration.TotalSeconds);
            return (inDuration, outDuration);
        }
    }
}
