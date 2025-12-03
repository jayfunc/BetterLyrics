using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using System;
using System.Linq;
using Windows.UI;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsAnimator
    {
        private readonly double _defaultScale = 0.75f;
        private readonly double _highlightedScale = 1.0f;

        public void UpdateLines(
            LyricsData? lyricsData,
            int startIndex,
            int endIndex,
            int playingLineIndex,
            double canvasHeight,
            double targetYScrollOffset,
            double playingLineTopOffsetFactor,
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
            if (lyricsData == null) return;

            var currentPlayingLine = lyricsData.LyricsLines.ElementAtOrDefault(playingLineIndex);
            if (currentPlayingLine == null) return;

            for (int i = startIndex; i <= endIndex + 1; i++)
            {
                var line = lyricsData.LyricsLines.ElementAtOrDefault(i);
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
                    line.ScaleTransition.StartTransition(_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale));

                    line.PhoneticOpacityTransition.SetDuration(yScrollDuration);
                    line.PhoneticOpacityTransition.SetDelay(yScrollDelay);
                    line.PhoneticOpacityTransition.StartTransition(absLineCountDelta == 0 ? 0.6 : (isMouseScrolling ? 0.3 : (1 - distanceFactor) * 0.3));

                    line.PlayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.PlayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.PlayedOriginalOpacityTransition.StartTransition(absLineCountDelta == 0 ? 1 : (isMouseScrolling ? 0.3 : (1 - distanceFactor) * 0.3));

                    line.UnplayedOriginalOpacityTransition.SetDuration(yScrollDuration);
                    line.UnplayedOriginalOpacityTransition.SetDelay(yScrollDelay);
                    line.UnplayedOriginalOpacityTransition.StartTransition(absLineCountDelta == 0 ? 0.3 : (isMouseScrolling ? 0.3 : (1 - distanceFactor) * 0.3));

                    line.TranslatedOpacityTransition.SetDuration(yScrollDuration);
                    line.TranslatedOpacityTransition.SetDelay(yScrollDelay);
                    line.TranslatedOpacityTransition.StartTransition(absLineCountDelta == 0 ? 0.6 : (isMouseScrolling ? 0.3 : (1 - distanceFactor) * 0.3));

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
    }
}
