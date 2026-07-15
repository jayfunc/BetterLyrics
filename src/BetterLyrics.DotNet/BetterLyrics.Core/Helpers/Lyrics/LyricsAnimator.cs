using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers.Lyrics;

public class LyricsAnimator
{
    private readonly double _defaultScale = 0.75f;
    private readonly double _highlightedScale = 1.0f;

    public void UpdateLines(
        IList<BaseRenderLyricsLine>? lines,
        int startIndex,
        int endIndex,
        int primaryPlayingLineIndex,
        double lyricsWidth,
        double lyricsHeight,
        double targetScrollOffset,
        double playingLineTopOffsetFactor,
        LyricsStyleSettings lyricsStyle,
        LyricsEffectSettings lyricsEffect,
        ValueTransition<double> canvasScrollTransition,
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
        var isVertical = lyricsStyle.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        var phoneticOpacity = lyricsStyle.PhoneticLyricsOpacity / 100.0;
        var originalOpacity = lyricsStyle.UnplayedOriginalLyricsOpacity / 100.0;
        var translatedOpacity = lyricsStyle.TranslatedLyricsOpacity / 100.0;

        // 动态视口缓冲区域
        var spaceBefore = isVertical
            ? lyricsWidth * playingLineTopOffsetFactor
            : lyricsHeight * playingLineTopOffsetFactor;

        var spaceAfter = isVertical
            ? lyricsWidth * (1 - playingLineTopOffsetFactor)
            : lyricsHeight * (1 - playingLineTopOffsetFactor);

        var scrollTopDurationSec = lyricsEffect.LyricsScrollTopDuration / 1000.0;
        var scrollTopDelaySec = lyricsEffect.LyricsScrollTopDelay / 1000.0;
        var scrollBottomDurationSec = lyricsEffect.LyricsScrollBottomDuration / 1000.0;
        var scrollBottomDelaySec = lyricsEffect.LyricsScrollBottomDelay / 1000.0;
        var canvasTransDuration = canvasScrollTransition.DurationSeconds;

        var isBlurEnabled = lyricsEffect.IsLyricsBlurEffectEnabled;
        var isOutOfSightEnabled = lyricsEffect.IsLyricsOutOfSightEffectEnabled;
        var isFanEnabled = lyricsEffect.IsFanLyricsEnabled;
        var fanAngleRad = Math.PI * (lyricsEffect.FanLyricsAngle / 180.0);
        var isGlowEnabled = lyricsEffect.IsLyricsGlowEffectEnabled;
        var isFloatEnabled = lyricsEffect.IsLyricsFloatAnimationEnabled;
        var isScaleEnabled = lyricsEffect.IsLyricsScaleEffectEnabled;

        var safeStart = Math.Max(0, startIndex);
        var safeEnd = Math.Min(lines.Count - 1, endIndex + 1);

        for (var i = safeStart; i <= safeEnd; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var lineHeight = line.PrimaryLineHeight;
            if (lineHeight == null || lineHeight <= 0) continue;

            var isWordAnimationEnabled = lyricsEffect.WordByWordEffectMode switch
            {
                WordByWordEffectMode.Auto => line.IsPrimaryHasRealSyllableInfo,
                WordByWordEffectMode.Always => true,
                WordByWordEffectMode.Never => false,
                _ => line.IsPrimaryHasRealSyllableInfo
            };

            var targetCharFloat = lyricsEffect.IsLyricsFloatAnimationAmountAutoAdjust
                ? lineHeight.Value * 0.1
                : lyricsEffect.LyricsFloatAnimationAmount;
            var targetCharGlow = lyricsEffect.IsLyricsGlowEffectAmountAutoAdjust
                ? lineHeight.Value * 0.2
                : lyricsEffect.LyricsGlowEffectAmount;
            var targetCharScale = lyricsEffect.IsLyricsScaleEffectAmountAutoAdjust
                ? 1.15
                : lyricsEffect.LyricsScaleEffectAmount / 100.0;

            var maxAnimationDurationMs = Math.Max((line.EndMs ?? 0) - currentPositionMs, 0);

            var isSecondaryLinePlaying = line.GetIsPlaying(currentPositionMs);
            var isSecondaryLinePlayingChanged = line.IsPlayingLastFrame != isSecondaryLinePlaying;
            line.IsPlayingLastFrame = isSecondaryLinePlaying;

            var playProgress = line.GetPlayProgress(currentPositionMs);

            // 行动画
            if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged ||
                isSecondaryLinePlayingChanged || isArtThemeColorsChanged)
            {
                var lineCountDelta = i - primaryPlayingLineIndex;

                // 动态距离计算
                double distanceFromPlayingLine = isVertical
                    ? Math.Abs(line.TopLeftPosition.X - primaryPlayingLine.TopLeftPosition.X)
                    : Math.Abs(line.TopLeftPosition.Y - primaryPlayingLine.TopLeftPosition.Y);

                double distanceFactor;
                if (lineCountDelta < 0)
                    distanceFactor = Math.Clamp(distanceFromPlayingLine / spaceBefore, 0, 1);
                else
                    distanceFactor = Math.Clamp(distanceFromPlayingLine / spaceAfter, 0, 1);

                double scrollDuration;
                double scrollDelay;

                if (lineCountDelta < 0)
                {
                    scrollDuration =
                        canvasTransDuration +
                        distanceFactor * (scrollTopDurationSec - canvasTransDuration);
                    scrollDelay = distanceFactor * scrollTopDelaySec;
                }
                else if (lineCountDelta == 0)
                {
                    scrollDuration = canvasTransDuration;
                    scrollDelay = 0;
                }
                else
                {
                    scrollDuration =
                        canvasTransDuration +
                        distanceFactor * (scrollBottomDurationSec - canvasTransDuration);
                    scrollDelay = distanceFactor * scrollBottomDelaySec;
                }

                line.BlurAmountTransition.SetDuration(scrollDuration);
                line.BlurAmountTransition.SetDelay(scrollDelay);
                line.BlurAmountTransition.Start(
                    isMouseScrolling || isSecondaryLinePlaying ? 0 : isBlurEnabled ? 5 * distanceFactor : 0);

                line.ScaleTransition.SetDuration(scrollDuration);
                line.ScaleTransition.SetDelay(scrollDelay);
                line.ScaleTransition.Start(
                    isSecondaryLinePlaying
                        ? _highlightedScale
                        : isOutOfSightEnabled
                            ? _highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)
                            : _highlightedScale);

                line.TertiaryOpacityTransition.SetDuration(scrollDuration);
                line.TertiaryOpacityTransition.SetDelay(scrollDelay);
                line.TertiaryOpacityTransition.Start(
                    isSecondaryLinePlaying
                        ? phoneticOpacity
                        : CalculateTargetOpacity(phoneticOpacity, phoneticOpacity, distanceFactor, isMouseScrolling,
                            lyricsEffect));

                // 原文不透明度（已播放）
                line.PlayedPrimaryOpacityTransition.SetDuration(scrollDuration);
                line.PlayedPrimaryOpacityTransition.SetDelay(scrollDelay);
                line.PlayedPrimaryOpacityTransition.Start(
                    isSecondaryLinePlaying
                        ? 1.0
                        : CalculateTargetOpacity(originalOpacity, 1.0, distanceFactor, isMouseScrolling,
                            lyricsEffect));
                // 原文不透明度（未播放）
                line.UnplayedPrimaryOpacityTransition.SetDuration(scrollDuration);
                line.UnplayedPrimaryOpacityTransition.SetDelay(scrollDelay);
                line.UnplayedPrimaryOpacityTransition.Start(
                    isSecondaryLinePlaying
                        ? originalOpacity
                        : CalculateTargetOpacity(originalOpacity, originalOpacity, distanceFactor, isMouseScrolling,
                            lyricsEffect));

                line.SecondaryOpacityTransition.SetDuration(scrollDuration);
                line.SecondaryOpacityTransition.SetDelay(scrollDelay);
                line.SecondaryOpacityTransition.Start(
                    isSecondaryLinePlaying
                        ? translatedOpacity
                        : CalculateTargetOpacity(translatedOpacity, translatedOpacity, distanceFactor,
                            isMouseScrolling, lyricsEffect));

                line.PlayedFillColorTransition.SetDuration(scrollDuration);
                line.PlayedFillColorTransition.SetDelay(scrollDelay);
                line.PlayedFillColorTransition.Start(isSecondaryLinePlaying
                    ? albumArtThemeColors.PlayedCurrentLineFillColor
                    : albumArtThemeColors.NonCurrentLineFillColor);

                line.UnplayedFillColorTransition.SetDuration(scrollDuration);
                line.UnplayedFillColorTransition.SetDelay(scrollDelay);
                line.UnplayedFillColorTransition.Start(isSecondaryLinePlaying
                    ? albumArtThemeColors.UnplayedCurrentLineFillColor
                    : albumArtThemeColors.NonCurrentLineFillColor);

                line.PlayedStrokeColorTransition.SetDuration(scrollDuration);
                line.PlayedStrokeColorTransition.SetDelay(scrollDelay);
                line.PlayedStrokeColorTransition.Start(isSecondaryLinePlaying
                    ? albumArtThemeColors.PlayedTextStrokeColor
                    : albumArtThemeColors.UnplayedTextStrokeColor);

                line.UnplayedStrokeColorTransition.SetDuration(scrollDuration);
                line.UnplayedStrokeColorTransition.SetDelay(scrollDelay);
                line.UnplayedStrokeColorTransition.Start(isSecondaryLinePlaying
                    ? albumArtThemeColors.UnplayedTextStrokeColor
                    : albumArtThemeColors.UnplayedTextStrokeColor);

                line.AngleTransition.SetInterpolator(canvasScrollTransition.Interpolator);
                line.AngleTransition.SetDuration(scrollDuration);
                line.AngleTransition.SetDelay(scrollDelay);
                line.AngleTransition.Start(
                    isFanEnabled && !isMouseScrolling
                        ? fanAngleRad * distanceFactor * (i > primaryPlayingLineIndex ? 1 : -1)
                        : 0);

                if (isLayoutChanged || isPrimaryPlayingLineChanged || isMouseScrollingChanged)
                {
                    line.OffsetTransition.SetInterpolator(canvasScrollTransition.Interpolator);
                    line.OffsetTransition.SetDuration(scrollDuration);
                    line.OffsetTransition.SetDelay(scrollDelay);
                    if (isLayoutChanged)
                        line.OffsetTransition.JumpTo(targetScrollOffset);
                    else
                        line.OffsetTransition.Start(targetScrollOffset);
                }
            }

            if (isWordAnimationEnabled)
            {
                if (isSecondaryLinePlayingChanged)
                {
                    // 辉光动画（从行首开始到当前）
                    if (isGlowEnabled && lyricsEffect.LyricsGlowEffectScope ==
                                      LyricsEffectScope.LineStartToCurrentChar
                                      && isSecondaryLinePlaying)
                        foreach (var renderChar in line.PrimaryRenderChars)
                        {
                            var stepInOutDuration =
                                Math.Min(Time.AnimationDuration.TotalMilliseconds, maxAnimationDurationMs) / 2.0 /
                                1000.0;
                            var stepLastingDuration =
                                Math.Max(maxAnimationDurationMs / 1000.0 - stepInOutDuration * 2, 0);
                            renderChar.GlowTransition.Start(
                                new Keyframe<double>(targetCharGlow, stepInOutDuration),
                                new Keyframe<double>(targetCharGlow, stepLastingDuration),
                                new Keyframe<double>(0, stepInOutDuration)
                            );
                        }

                    // 浮动动画（控制整体）
                    if (isFloatEnabled)
                        foreach (var renderChar in line.PrimaryRenderChars)
                            if (isSecondaryLinePlaying)
                            {
                                if (renderChar.EndMs < currentPositionMs)
                                    // 确保已播放的部分恢复原位
                                    renderChar.FloatTransition.JumpTo(0);
                                else
                                    // 下沉（以便后续上浮）
                                    renderChar.FloatTransition.Start(targetCharFloat);
                            }
                            else
                            {
                                // 恢复初始状态（相当于上浮）
                                renderChar.FloatTransition.Start(0);
                            }
                }

                // 浮动动画（控制单个）
                foreach (var renderChar in line.PrimaryRenderChars)
                {
                    renderChar.ProgressPlayed = renderChar.GetPlayProgress(currentPositionMs);

                    var isCharPlaying = renderChar.GetIsPlaying(currentPositionMs);
                    var isCharPlayingChanged = renderChar.IsPlayingLastFrame != isCharPlaying;

                    if (isCharPlayingChanged)
                    {
                        if (isFloatEnabled)
                        {
                            renderChar.FloatTransition.SetDurationMs(
                                Math.Min(lyricsEffect.LyricsFloatAnimationDuration, maxAnimationDurationMs));
                            renderChar.FloatTransition.Start(0);
                        }

                        renderChar.IsPlayingLastFrame = isCharPlaying;
                    }
                    else
                    {
                        if (!isCharPlaying && currentPositionMs > renderChar.EndMs &&
                            renderChar.FloatTransition.Value != 0)
                        {
                            renderChar.FloatTransition.SetDurationMs(
                                Math.Min(lyricsEffect.LyricsFloatAnimationDuration, maxAnimationDurationMs));
                            renderChar.FloatTransition.Start(0);
                        }
                    }
                }

                foreach (var syllable in line.PrimaryRenderSyllables)
                {
                    var isSyllablePlaying = syllable.GetIsPlaying(currentPositionMs);
                    var isSyllablePlayingChanged = syllable.IsPlayingLastFrame != isSyllablePlaying;

                    var desiredAnimationDurationMs = Math.Max((syllable.EndMs ?? 0) - currentPositionMs, 0);

                    if (isSyllablePlayingChanged)
                    {
                        // 缩放
                        if (isScaleEnabled && isSyllablePlaying)
                            foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                                if (syllable.DurationMs >= lyricsEffect.LyricsScaleEffectLongSyllableDuration)
                                {
                                    var (inDuration, outDuration) =
                                        CalculateSegmentDuration(desiredAnimationDurationMs / 1000.0,
                                            maxAnimationDurationMs / 1000.0);
                                    renderChar.ScaleTransition.Start(
                                        new Keyframe<double>(targetCharScale, inDuration),
                                        new Keyframe<double>(1.0, outDuration)
                                    );
                                }

                        // 辉光（长音节）
                        if (isGlowEnabled && isSyllablePlaying && lyricsEffect.LyricsGlowEffectScope ==
                            LyricsEffectScope.LongDurationSyllable
                            && syllable.DurationMs >= lyricsEffect.LyricsGlowEffectLongSyllableDuration)
                            foreach (var renderChar in syllable.ChildrenRenderLyricsChars)
                            {
                                var (inDuration, outDuration) =
                                    CalculateSegmentDuration(desiredAnimationDurationMs / 1000.0,
                                        maxAnimationDurationMs / 1000.0);
                                renderChar.GlowTransition.Start(
                                    new Keyframe<double>(targetCharGlow, inDuration),
                                    new Keyframe<double>(0, outDuration)
                                );
                            }

                        syllable.IsPlayingLastFrame = isSyllablePlaying;
                    }
                }

                foreach (var renderChar in line.PrimaryRenderChars) renderChar.Update(elapsedTime);
            }

            if (!autoWrap)
            {
                // 长文本跑马灯滚动计算，竖排用高度做极限比较，横排用宽度
                var layoutLimit = isVertical ? lyricsHeight : lyricsWidth;

                var pSize = line.PrimaryTextLayoutBounds != null ? (isVertical ? line.PrimaryTextLayoutBounds.Height : line.PrimaryTextLayoutBounds.Width) : 0;
                var sSize = line.SecondaryTextLayoutBounds != null ? (isVertical ? line.SecondaryTextLayoutBounds.Height : line.SecondaryTextLayoutBounds.Width) : 0;
                var tSize = line.TertiaryTextLayoutBounds != null ? (isVertical ? line.TertiaryTextLayoutBounds.Height : line.TertiaryTextLayoutBounds.Width) : 0;

                var alignmentForNonWrap = lyricsStyle.UseInternalLyricsAlignment
                    ? line.HorizontalAlignmentType ?? lyricsStyle.LyricsAlignmentType
                    : lyricsStyle.LyricsAlignmentType;
                if (alignmentForNonWrap == TextAlignmentType.LeftRight)
                {
                    alignmentForNonWrap = i % 2 == 0 ? TextAlignmentType.Left : TextAlignmentType.Right;
                }

                if (isSecondaryLinePlaying)
                {
                    if (pSize > 0)
                        line.PrimaryXOffsetTransition.JumpTo(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, pSize, layoutLimit,
                                playProgress));
                    if (sSize > 0)
                        line.SecondaryXOffsetTransition.JumpTo(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, sSize, layoutLimit,
                                playProgress));
                    if (tSize > 0)
                        line.TertiaryXOffsetTransition.JumpTo(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, tSize, layoutLimit,
                                playProgress));
                }

                if (isSecondaryLinePlayingChanged && !isSecondaryLinePlaying)
                {
                    if (pSize > 0)
                        line.PrimaryXOffsetTransition.Start(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, pSize, layoutLimit, 0));
                    if (sSize > 0)
                        line.SecondaryXOffsetTransition.Start(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, sSize, layoutLimit, 0));
                    if (tSize > 0)
                        line.TertiaryXOffsetTransition.Start(
                            CalculateTargetNonWrapOffset(alignmentForNonWrap, tSize, layoutLimit, 0));
                }
            }

            line.Update(elapsedTime);
        }
    }

    private static double CalculateTargetOpacity(double baseOpacity, double baseOpacityWhenZeroDistanceFactor,
        double distanceFactor, bool isMouseScrolling, LyricsEffectSettings lyricsEffect)
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
                    targetOpacity = (1 - distanceFactor) * baseOpacity;
                else
                    targetOpacity = baseOpacity;
            }
        }

        return targetOpacity;
    }

    private static double CalculateTargetNonWrapOffset(TextAlignmentType textAlignmentType, double actualSize,
        double limitSize, double progress)
    {
        var offset = textAlignmentType switch
        {
            TextAlignmentType.Center => (limitSize - actualSize) / 2,
            TextAlignmentType.Right => limitSize - actualSize,
            _ => 0
        };
        offset = -Math.Min(0, offset);
        var progressStartToScroll = limitSize * 0.5 / actualSize;
        var progressEndToScroll = 1 - progressStartToScroll;
        return -Math.Max(Math.Min(progress, progressEndToScroll) - progressStartToScroll, 0) * actualSize +
               offset;
    }

    private static (double InDuration, double OutDuration) CalculateSegmentDuration(double desiredDuration,
        double maxDuration)
    {
        var inDuration = Math.Min(desiredDuration, maxDuration);
        var outDuration = Math.Min(maxDuration - inDuration, Time.AnimationDuration.TotalSeconds);
        if (outDuration == 0)
        {
            outDuration = inDuration / 3 * 1;
            inDuration = outDuration * 2;
        }

        return (inDuration, outDuration);
    }
}