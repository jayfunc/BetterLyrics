using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsLayoutManager
    {
        /// <summary>
        /// 重排歌词，Y 轴从 0 刻度开始算
        /// </summary>
        /// <param name="resourceCreator"></param>
        /// <param name="lyricsData"></param>
        /// <param name="status"></param>
        /// <param name="appSettings"></param>
        /// <param name="canvasWidth"></param>
        /// <param name="canvasHeight"></param>
        /// <param name="lyricsWidth"></param>
        /// <param name="lyricsHeight"></param>
        public void MeasureAndArrange(
            ICanvasAnimatedControl resourceCreator,
            LyricsData? lyricsData,
            LyricsWindowStatus status,
            AppSettings appSettings,
            double canvasWidth,
            double canvasHeight,
            double lyricsWidth,
            double lyricsHeight)
        {
            if (lyricsData == null || resourceCreator == null) return;

            // 计算字体大小
            int originalFontSize, phoneticFontSize, translatedFontSize;
            var style = status.LyricsStyleSettings;

            if (style.IsDynamicLyricsFontSize)
            {
                var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(canvasWidth, canvasHeight);

                phoneticFontSize = (int)lyricsLayoutMetrics.TransliterationSize;
                originalFontSize = (int)lyricsLayoutMetrics.MainLyricsSize;
                translatedFontSize = (int)lyricsLayoutMetrics.TranslationSize;
            }
            else
            {
                phoneticFontSize = style.PhoneticLyricsFontSize;
                originalFontSize = style.OriginalLyricsFontSize;
                translatedFontSize = style.TranslatedLyricsFontSize;
            }

            var fontWeight = style.LyricsFontWeight;

            // 排版
            double currentY = 0;

            foreach (var line in lyricsData.LyricsLines)
            {
                if (line == null) continue;

                line.RecreateTextLayout(
                    resourceCreator,
                    appSettings.TranslationSettings.IsChineseRomanizationEnabled || appSettings.TranslationSettings.IsJapaneseRomanizationEnabled,
                    appSettings.TranslationSettings.IsTranslationEnabled,
                    phoneticFontSize, originalFontSize, translatedFontSize,
                    fontWeight,
                    style.LyricsCJKFontFamily, style.LyricsWesternFontFamily,
                    lyricsWidth, lyricsHeight, style.LyricsAlignmentType
                );

                line.RecreateTextGeometry();

                // 顶部坐标
                line.TopPosition = new Vector2(0, (float)currentY);

                // 注音层
                line.PhoneticPosition = line.TopPosition;
                if (line.PhoneticCanvasTextLayout != null)
                {
                    currentY += line.PhoneticCanvasTextLayout.LayoutBounds.Height;
                    // 间距
                    currentY += (line.PhoneticCanvasTextLayout.LayoutBounds.Height / line.PhoneticCanvasTextLayout.LineCount) * 0.1;
                }

                // 原文层
                line.OriginalPosition = new Vector2(0, (float)currentY);
                if (line.OriginalCanvasTextLayout != null)
                {
                    currentY += line.OriginalCanvasTextLayout.LayoutBounds.Height;
                }

                // 翻译层
                if (line.TranslatedCanvasTextLayout != null)
                {
                    // 间距
                    currentY += (line.TranslatedCanvasTextLayout.LayoutBounds.Height / line.TranslatedCanvasTextLayout.LineCount) * 0.1;
                }
                line.TranslatedPosition = new Vector2(0, (float)currentY);
                if (line.TranslatedCanvasTextLayout != null)
                {
                    currentY += line.TranslatedCanvasTextLayout.LayoutBounds.Height;
                }

                // 底部坐标
                line.BottomPosition = new Vector2(0, (float)currentY);

                // 行间距
                if (line.OriginalCanvasTextLayout != null)
                {
                    currentY += (line.OriginalCanvasTextLayout.LayoutBounds.Height / line.OriginalCanvasTextLayout.LineCount) * style.LyricsLineSpacingFactor;
                }

                // 更新中心点
                line.UpdateCenterPosition(lyricsWidth, style.LyricsAlignmentType);
            }
        }

        /// <summary>
        /// 计算为了让当前歌词行的竖直几何中心点对齐到 0（原点），画布应该移动的距离（从画布最初始状态计算的值）
        /// </summary>
        public double? CalculateTargetScrollOffset(
            LyricsData? lyricsData,
            int playingLineIndex)
        {
            var lines = lyricsData?.LyricsLines;
            if (lines == null || lines.Count == 0) return null;

            var currentLine = lines.ElementAtOrDefault(playingLineIndex);
            var firstLine = lines.FirstOrDefault();

            if (currentLine?.OriginalCanvasTextLayout == null || firstLine == null) return null;

            return -currentLine.OriginalPosition.Y
                + firstLine.OriginalPosition.Y
                - (currentLine.TranslatedPosition.Y
                    + (currentLine.TranslatedCanvasTextLayout?.LayoutBounds.Height ?? 0)
                    - currentLine.PhoneticPosition.Y) / 2.0;
        }

        /// <summary>
        /// 计算当前屏幕可见的行范围
        /// 返回值: (StartVisibleIndex, EndVisibleIndex)
        /// </summary>
        public (int Start, int End) CalculateVisibleRange(
            IList<LyricsLine>? lines,
            double currentScrollOffset,
            double lyricsY,
            double lyricsHeight,
            double canvasHeight)
        {
            if (lines == null || lines.Count == 0) return (-1, -1);

            double offset = currentScrollOffset + lyricsY + lyricsHeight / 2;

            int start = FindFirstVisibleLine(lines, offset, lyricsY);
            int end = FindLastVisibleLine(lines, offset, lyricsY, lyricsHeight, canvasHeight);

            // 修正边界情况
            if (start != -1 && end == -1)
            {
                end = lines.Count - 1;
            }

            return (start, end);
        }

        private int FindFirstVisibleLine(IList<LyricsLine> lines, double offset, double lyricsY)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                if (line.OriginalCanvasTextLayout == null) break;
                double value = offset + line.BottomPosition.Y;
                // 理论上说应该使用下面这一行来精确计算视野内的首个可见行，但是考虑到动画视觉效果，还是注释掉了
                //if (value >= lyricsY) { result = mid; right = mid - 1; }
                if (value >= 0) { result = mid; right = mid - 1; }
                else { left = mid + 1; }
            }
            return result;
        }

        private int FindLastVisibleLine(IList<LyricsLine> lines, double offset, double lyricsY, double lyricsHeight, double canvasHeight)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                if (line.OriginalCanvasTextLayout == null) break;
                double value = offset + line.BottomPosition.Y;
                // 同理
                //if (value >= lyricsY + lyricsHeight) { result = mid; right = mid - 1; }
                if (value >= canvasHeight) { result = mid; right = mid - 1; }
                else { left = mid + 1; }
            }
            return result;
        }
    }
}
