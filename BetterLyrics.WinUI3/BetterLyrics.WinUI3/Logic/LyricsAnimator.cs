using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
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
            int playingLineIndex,
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
            bool isPlayingLineChanged,
            bool isMouseScrollingChanged
        )
        {
            if (lines == null) return;

            var currentPlayingLine = lines.ElementAtOrDefault(playingLineIndex);
            if (currentPlayingLine == null) return;

            var phoneticOpacity = lyricsStyle.PhoneticLyricsOpacity / 100.0;
            var originalOpacity = lyricsStyle.OriginalLyricsOpacity / 100.0;
            var translatedOpacity = lyricsStyle.TranslatedLyricsOpacity / 100.0;

            for (int i = startIndex; i <= endIndex + 1; i++)
            {
                var line = lines.ElementAtOrDefault(i);
                if (line == null) continue;

                if (isLayoutChanged || isPlayingLineChanged || isMouseScrollingChanged)
                {
                    int lineCountDelta = i - playingLineIndex;
                    int absLineCountDelta = Math.Abs(lineCountDelta);
                    double distanceFromPlayingLine = Math.Abs(line.OriginalPosition.Y - currentPlayingLine.OriginalPosition.Y);

                    double distanceFactor = 0;
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
                    line.BlurAmountTransition.StartTransition(isMouseScrolling ? 0 : (lyricsEffect.IsLyricsBlurEffectEnabled ? (5 * distanceFactor) : 0));

                    line.ScaleTransition.SetDuration(yScrollDuration);
                    line.ScaleTransition.SetDelay(yScrollDelay);
                    line.ScaleTransition.StartTransition(
                        lyricsEffect.IsLyricsOutOfSightEffectEnabled ?
                        (_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale)) :
                        _highlightedScale);

                    line.PhoneticOpacityTransition.SetDuration(yScrollDuration);
                    line.PhoneticOpacityTransition.SetDelay(yScrollDelay);
                    line.PhoneticOpacityTransition.StartTransition(
                        CalculateTargetOpacity(phoneticOpacity, phoneticOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.PlayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.PlayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.PlayedOriginalOpacityTransition.StartTransition(
                        CalculateTargetOpacity(originalOpacity, 1.0, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.UnplayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.UnplayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.UnplayedOriginalOpacityTransition.StartTransition(
                        CalculateTargetOpacity(originalOpacity, originalOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.TranslatedOpacityTransition.SetDuration(yScrollDuration);
                    line.TranslatedOpacityTransition.SetDelay(yScrollDelay);
                    line.TranslatedOpacityTransition.StartTransition(
                        CalculateTargetOpacity(translatedOpacity, translatedOpacity, distanceFactor, isMouseScrolling, lyricsEffect));

                    line.ColorTransition.SetDuration(yScrollDuration);
                    line.ColorTransition.SetDelay(yScrollDelay);
                    line.ColorTransition.StartTransition(absLineCountDelta == 0 ? fgColor : bgColor);

                    line.AngleTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.AngleTransition.SetDuration(yScrollDuration);
                    line.AngleTransition.SetDelay(yScrollDelay);
                    line.AngleTransition.StartTransition(lyricsEffect.IsFanLyricsEnabled ?
                        Math.PI * (lyricsEffect.FanLyricsAngle / 180.0) * distanceFactor * (i > playingLineIndex ? 1 : -1) : 0);

                    line.YOffsetTransition.SetEasingType(canvasYScrollTransition.EasingType);
                    line.YOffsetTransition.SetDuration(yScrollDuration);
                    line.YOffsetTransition.SetDelay(yScrollDelay);
                    // 设计之初是当 isLayoutChanged 为真时 jumpTo
                    // 但考虑到动画视觉，强制使用动画
                    line.YOffsetTransition.StartTransition(targetYScrollOffset);
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
