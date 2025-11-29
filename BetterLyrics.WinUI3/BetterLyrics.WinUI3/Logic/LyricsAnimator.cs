using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsAnimator
    {
        private readonly double _defaultScale = 0.75f;
        private readonly double _highlightedScale = 1.0f;

        public void UpdateVisibleLines(
            LyricsData? lyricsData,
            int startVisibleIndex,
            int endVisibleIndex,
            int playingLineIndex,
            double canvasHeight,
            double targetYScrollOffset,
            LyricsEffectSettings lyricsEffect,
            ValueTransition<double> canvasYScrollTransition,
            Color bgColor,
            Color fgColor,
            TimeSpan elapsedTime,
            bool isForceUpdate) // 对应 _isLayoutChanged || _isPlayingLineChanged
        {
            if (lyricsData == null) return;

            var currentPlayingLine = lyricsData.LyricsLines.ElementAtOrDefault(playingLineIndex);
            if (currentPlayingLine == null) return;

            // 循环更新可见行
            for (int i = startVisibleIndex; i <= endVisibleIndex + 1; i++)
            {
                var line = lyricsData.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;

                if (isForceUpdate)
                {
                    int lineCountDelta = i - playingLineIndex;
                    int absLineCountDelta = Math.Abs(lineCountDelta);
                    double distanceFromPlayingLine = Math.Abs(line.OriginalPosition.Y - currentPlayingLine.OriginalPosition.Y);
                    double distanceFactor = Math.Clamp(distanceFromPlayingLine / (canvasHeight / 2), 0, 1);

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
                    line.BlurAmountTransition.StartTransition(5 * distanceFactor);

                    line.ScaleTransition.SetDuration(yScrollDuration);
                    line.ScaleTransition.SetDelay(yScrollDelay);
                    line.ScaleTransition.StartTransition(_highlightedScale - distanceFactor * (_highlightedScale - _defaultScale));

                    line.OpacityTransition.SetDuration(yScrollDuration);
                    line.OpacityTransition.SetDelay(yScrollDelay);
                    line.OpacityTransition.StartTransition(absLineCountDelta == 0 ? 1 : (1 - distanceFactor) * 0.3);

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
                    line.YOffsetTransition.StartTransition(targetYScrollOffset);
                }

                line.AngleTransition.Update(elapsedTime);
                line.ScaleTransition.Update(elapsedTime);
                line.BlurAmountTransition.Update(elapsedTime);
                line.OpacityTransition.Update(elapsedTime);
                line.YOffsetTransition.Update(elapsedTime);
                line.ColorTransition.Update(elapsedTime);
            }
        }
    }
}
