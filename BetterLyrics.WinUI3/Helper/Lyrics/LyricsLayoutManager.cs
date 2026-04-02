using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Helper.Lyrics
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
        public static void MeasureAndArrange(
            ICanvasAnimatedControl resourceCreator,
            IList<RenderLyricsLine>? lines,
            LyricsWindowStatus status,
            AppSettings appSettings,
            double canvasWidth,
            double canvasHeight,
            double lyricsWidth,
            double lyricsHeight)
        {
            if (lines == null || resourceCreator == null) return;

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

            foreach (var line in lines)
            {
                if (line == null) continue;

                double actualWidth = 0;

                line.RecreateTextLayout(
                    resourceCreator,
                    appSettings.TranslationSettings.IsChineseRomanizationEnabled || appSettings.TranslationSettings.IsJapaneseRomanizationEnabled,
                    appSettings.TranslationSettings.IsTranslationEnabled,
                    phoneticFontSize, originalFontSize, translatedFontSize,
                    fontWeight,
                    style.LyricsCJKFontFamily, style.LyricsWesternFontFamily,
                    lyricsWidth, lyricsHeight,
                    style.LyricsAlignmentType, style.AutoWrap
                );

                line.RecreateTextGeometry();

                line.DisposeCaches();

                // 左上角坐标
                line.TopLeftPosition = new Vector2(0, (float)currentY);
                // 注音层
                line.TertiaryPosition = line.TopLeftPosition;
                if (line.TertiaryTextLayout != null)
                {
                    currentY += line.TertiaryTextLayout.LayoutBounds.Height;
                    // 间距
                    currentY += (line.TertiaryTextLayout.LayoutBounds.Height / line.TertiaryTextLayout.LineCount) * 0.1;

                    actualWidth = Math.Max(actualWidth, line.TertiaryTextLayout.LayoutBounds.Width);
                }

                // 原文层
                line.PrimaryPosition = new Vector2(0, (float)currentY);
                if (line.PrimaryTextLayout != null)
                {
                    currentY += line.PrimaryTextLayout.LayoutBounds.Height;

                    actualWidth = Math.Max(actualWidth, line.PrimaryTextLayout.LayoutBounds.Width);
                }

                // 翻译层
                if (line.PrimaryTextLayout != null && line.SecondaryTextLayout != null)
                {
                    // 间距
                    currentY += (line.SecondaryTextLayout.LayoutBounds.Height / line.SecondaryTextLayout.LineCount) * 0.1;
                }
                line.SecondaryPosition = new Vector2(0, (float)currentY);
                if (line.SecondaryTextLayout != null)
                {
                    currentY += line.SecondaryTextLayout.LayoutBounds.Height;

                    actualWidth = Math.Max(actualWidth, line.SecondaryTextLayout.LayoutBounds.Width);
                }

                // 右下角坐标
                line.BottomRightPosition = new Vector2(0 + (float)actualWidth, (float)currentY);

                // 行间距
                if (line.PrimaryTextLayout != null && line.PrimaryTextLayout != null)
                {
                    currentY += (line.PrimaryTextLayout.LayoutBounds.Height / line.PrimaryTextLayout.LineCount) * style.LyricsLineSpacingFactor;
                }

                line.TopLeftPosition = style.LyricsAlignmentType switch
                {
                    TextAlignmentType.Left => line.TopLeftPosition,
                    TextAlignmentType.Center => line.TopLeftPosition.AddX((float)((lyricsWidth - actualWidth) / 2)),
                    TextAlignmentType.Right => line.TopLeftPosition.AddX((float)(lyricsWidth - actualWidth)),
                    _ => line.TopLeftPosition
                };

                line.BottomRightPosition = style.LyricsAlignmentType switch
                {
                    TextAlignmentType.Left => line.BottomRightPosition,
                    TextAlignmentType.Center => line.BottomRightPosition.AddX((float)((lyricsWidth - actualWidth) / 2)),
                    TextAlignmentType.Right => line.BottomRightPosition.AddX((float)(lyricsWidth - actualWidth)),
                    _ => line.BottomRightPosition
                };

                // 更新中心点
                double centerY = (line.TopLeftPosition.Y + line.BottomRightPosition.Y) / 2;

                line.CenterPosition = style.LyricsAlignmentType switch
                {
                    TextAlignmentType.Left => new Vector2(0, (float)centerY),
                    TextAlignmentType.Center => new Vector2((float)(lyricsWidth / 2), (float)centerY),
                    TextAlignmentType.Right => new Vector2((float)(lyricsWidth), (float)centerY),
                    _ => line.CenterPosition,
                };

                line.RecreateRenderChars(style.LyricsFontStrokeWidth);
            }
        }

        /// <summary>
        /// 计算为了让当前歌词行的竖直几何中心点对齐到 0（原点），画布应该移动的距离（从画布最初始状态计算的值）
        /// </summary>
        public static double? CalculateTargetScrollOffset(
            IList<RenderLyricsLine>? lines,
            int playingLineIndex)
        {
            if (lines == null || lines.Count == 0) return null;

            var currentLine = lines.ElementAtOrDefault(playingLineIndex);

            if (currentLine?.PrimaryTextLayout == null) return null;

            return -currentLine.CenterPosition.Y;
        }

        /// <summary>
        /// 计算当前屏幕可见的行范围
        /// 返回值: (StartVisibleIndex, EndVisibleIndex)
        /// </summary>
        public static (int Start, int End) CalculateVisibleRange(
            IList<RenderLyricsLine>? lines,
            double currentScrollOffset,
            double lyricsY,
            double lyricsHeight,
            double canvasHeight,
            double playingLineTopOffsetFactor
        )
        {
            if (lines == null || lines.Count == 0) return (-1, -1);

            double offset = currentScrollOffset + lyricsY + lyricsHeight * playingLineTopOffsetFactor;

            int start = FindFirstVisibleLine(lines, offset, lyricsY);
            int end = FindLastVisibleLine(lines, offset, lyricsY, lyricsHeight, canvasHeight);

            // 修正边界情况
            if (start != -1 && end == -1)
            {
                end = lines.Count - 1;
            }

            return (start, end);
        }

        public static (int Start, int End) CalculateMaxRange(IList<RenderLyricsLine>? lines)
        {
            if (lines == null || lines.Count == 0) return (-1, -1);

            return (0, lines.Count - 1);
        }

        public static double CalculateActualHeight(IList<RenderLyricsLine>? lines)
        {
            if (lines == null || lines.Count == 0) return 0;

            return lines.Last().BottomRightPosition.Y;
        }

        public static void CalculateLanes(IList<RenderLyricsLine>? lines, int toleranceMs = 50)
        {
            if (lines == null) return;
            var lanesEndMs = new List<int> { 0 };

            foreach (var line in lines)
            {
                var start = line.StartMs;
                var end = line.EndMs;

                int assignedLane = -1;
                for (int i = 0; i < lanesEndMs.Count; i++)
                {
                    if (lanesEndMs[i] <= start + toleranceMs)
                    {
                        assignedLane = i;
                        break;
                    }
                }

                if (assignedLane == -1)
                {
                    assignedLane = lanesEndMs.Count;
                    lanesEndMs.Add(0);
                }

                lanesEndMs[assignedLane] = end ?? 0;
                line.LaneIndex = assignedLane;
            }
        }

        public static int FindMouseHoverLineIndex(
            IList<RenderLyricsLine>? lines,
            bool isMouseInLyricsArea,
            Point mousePosition,
            double currentScrollOffset,
            double lyricsHeight,
            double playingLineTopOffsetFactor
        )
        {
            if (!isMouseInLyricsArea) return -1;

            if (lines == null || lines.Count == 0) return -1;

            double yOffset = currentScrollOffset + lyricsHeight * playingLineTopOffsetFactor;

            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                if (line.PrimaryTextLayout == null) break;
                double lineBottomY = yOffset + line.BottomRightPosition.Y;
                if (lineBottomY >= mousePosition.Y)
                {
                    result = mid;
                    right = mid - 1;
                }
                else { left = mid + 1; }
            }

            if (result != -1)
            {
                var line = lines[result];
                double lineLeftX = line.TopLeftPosition.X;
                double lineRightX = line.BottomRightPosition.X;
                double lineTopY = yOffset + line.TopLeftPosition.Y;
                if (mousePosition.X < lineLeftX || mousePosition.X > lineRightX || mousePosition.Y < lineTopY)
                {
                    result = -1;
                }
            }

            return result;
        }

        private static int FindFirstVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsY)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                if (line.PrimaryTextLayout == null) break;
                double value = offset + line.BottomRightPosition.Y;
                // 理论上说应该使用下面这一行来精确计算视野内的首个可见行，但是考虑到动画视觉效果，还是注释掉了
                //if (value >= lyricsY) { result = mid; right = mid - 1; }
                if (value >= 0) { result = mid; right = mid - 1; }
                else { left = mid + 1; }
            }
            return result;
        }

        private static int FindLastVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsY, double lyricsHeight, double canvasHeight)
        {
            int left = 0, right = lines.Count - 1, result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var line = lines[mid];
                if (line.PrimaryTextLayout == null) break;
                double value = offset + line.BottomRightPosition.Y;
                // 同理
                //if (value >= lyricsY + lyricsHeight) { result = mid; right = mid - 1; }
                if (value >= canvasHeight) { result = mid; right = mid - 1; }
                else { left = mid + 1; }
            }
            return result;
        }
    }
}
