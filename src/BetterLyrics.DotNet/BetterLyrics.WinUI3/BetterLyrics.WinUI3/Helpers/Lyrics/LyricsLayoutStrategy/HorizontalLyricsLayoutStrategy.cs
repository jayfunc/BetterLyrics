using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Helpers.Lyrics.LyricsLayoutStrategy;

public class HorizontalLyricsLayoutStrategy : LyricsLayoutStrategyBase
{
    /// <summary>
    ///     重排歌词，Y 轴从 0 刻度开始算
    /// </summary>
    /// <param name="resourceCreator"></param>
    /// <param name="lyricsData"></param>
    /// <param name="status"></param>
    /// <param name="appSettings"></param>
    /// <param name="canvasWidth"></param>
    /// <param name="canvasHeight"></param>
    /// <param name="lyricsWidth"></param>
    /// <param name="lyricsHeight"></param>
    public override void MeasureAndArrange(
        ICanvasAnimatedControl? resourceCreator,
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

        // 排版 (横排 Y 从 0 开始往正数递增)
        double currentX = 0;
        double currentY = 0;

        foreach (var line in lines)
        {
            if (line == null) continue;

            double actualWidth = 0;

            var alignment = style.UseInternalLyricsAlignment
                ? line.HorizontalAlignmentType ?? style.LyricsAlignmentType
                : style.LyricsAlignmentType;

            if (alignment == TextAlignmentType.LeftRight)
            {
                alignment = lines.IndexOf(line) % 2 == 0 ? TextAlignmentType.Left : TextAlignmentType.Right;
            }

            line.RecreateTextLayout(
                resourceCreator,
                appSettings.TranslationSettings.IsMandarinRomanizationEnabled ||
                appSettings.TranslationSettings.IsCantoneseRomanizationEnabled ||
                appSettings.TranslationSettings.IsJapaneseRomanizationEnabled ||
                appSettings.TranslationSettings.IsKoreanRomanizationEnabled,
                appSettings.TranslationSettings.IsTranslationEnabled,
                phoneticFontSize, originalFontSize, translatedFontSize,
                fontWeight,
                style.LyricsCJKFontFamily, style.LyricsWesternFontFamily,
                lyricsWidth, lyricsHeight,
                alignment, style.AutoWrap,
                style.LyricsLayoutOrientation
            );

            line.RecreateTextGeometry();
            line.DisposeCaches();

            var startY = currentY; // 记录本行的初始顶部位置

            // 找出当前行实际存在的图层，并按设置里的顺序排列
            var validLayers = new List<(LyricsLayerConfig Type, CanvasTextLayout Layout, Rect Bounds)>();
            foreach (var layer in style.LyricsLayerOrder)
            {
                var layout = layer.LyricsLayerType switch
                {
                    LyricsLayerType.Primary => line.PrimaryTextLayout,
                    LyricsLayerType.Secondary => line.SecondaryTextLayout,
                    LyricsLayerType.Tertiary => line.TertiaryTextLayout,
                    _ => null
                };

                if (layout != null) validLayers.Add((layer, layout, layout.LayoutBounds));
            }

            // 按顺序从上往下排
            for (var i = 0; i < validLayers.Count; i++)
            {
                var (layer, layout, bounds) = validLayers[i];
                var type = layer.LyricsLayerType;

                var pos = new Vector2((float)(currentX - bounds.X), (float)(currentY - bounds.Y));

                // 赋值给对应的图层
                if (type == LyricsLayerType.Primary) line.PrimaryPosition = pos;
                else if (type == LyricsLayerType.Secondary) line.SecondaryPosition = pos;
                else if (type == LyricsLayerType.Tertiary) line.TertiaryPosition = pos;

                currentY += bounds.Height;
                actualWidth = Math.Max(actualWidth, bounds.Width);

                // 如果不是最后一个图层，则加上层间距 (避免底部多出空隙)
                if (i < validLayers.Count - 1)
                    currentY += bounds.Height / layout.LineCount * style.LyricsLineInnerSpacingFactor;
            }

            // 初始包围盒上下边界
            line.TopLeftPosition = new Vector2((float)currentX, (float)startY);
            line.BottomRightPosition = new Vector2((float)(currentX + actualWidth), (float)currentY);

            // 行间距
            if (line.PrimaryTextLayout != null)
                currentY += line.PrimaryTextLayout.LayoutBounds.Height / line.PrimaryTextLayout.LineCount *
                            style.LyricsLineOverallSpacingFactor;

            // 计算全局 X 轴偏移量
            var offsetX = alignment switch
            {
                TextAlignmentType.Left => 0,
                TextAlignmentType.Center => (lyricsWidth - actualWidth) / 2,
                TextAlignmentType.Right => lyricsWidth - actualWidth,
                _ => 0
            };

            // 应用包围盒 X 轴偏移
            line.TopLeftPosition = line.TopLeftPosition.AddX((float)offsetX);
            line.BottomRightPosition = line.BottomRightPosition.AddX((float)offsetX);

            // 计算每个子层内部的相对 X 轴偏移，让长短文本相互对齐
            if (line.TertiaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.TertiaryTextLayout.LayoutBounds.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.TertiaryTextLayout.LayoutBounds.Width,
                    _ => 0
                };
                line.TertiaryPosition = line.TertiaryPosition.AddX((float)(offsetX + relativeX));
            }

            if (line.PrimaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.PrimaryTextLayout.LayoutBounds.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.PrimaryTextLayout.LayoutBounds.Width,
                    _ => 0
                };
                line.PrimaryPosition = line.PrimaryPosition.AddX((float)(offsetX + relativeX));
            }

            if (line.SecondaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.SecondaryTextLayout.LayoutBounds.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.SecondaryTextLayout.LayoutBounds.Width,
                    _ => 0
                };
                line.SecondaryPosition = line.SecondaryPosition.AddX((float)(offsetX + relativeX));
            }

            // 更新旋转与缩放的中心点
            double centerY = (line.TopLeftPosition.Y + line.BottomRightPosition.Y) / 2;

            line.CenterPosition = alignment switch
            {
                TextAlignmentType.Left => new Vector2(0, (float)centerY),
                TextAlignmentType.Center => new Vector2((float)(lyricsWidth / 2), (float)centerY),
                TextAlignmentType.Right => new Vector2((float)lyricsWidth, (float)centerY),
                _ => line.CenterPosition
            };

            line.RecreateRenderChars(style.LyricsFontStrokeWidth);
        }
    }

    /// <summary>
    ///     计算为了让当前歌词行的竖直几何中心点对齐到 0（原点），画布应该移动的距离（从画布最初始状态计算的值）
    /// </summary>
    public override double? CalculateTargetScrollOffset(IList<RenderLyricsLine>? lines, int playingLineIndex)
    {
        if (lines == null || lines.Count == 0) return null;
        var currentLine = lines.ElementAtOrDefault(playingLineIndex);
        if (currentLine?.PrimaryTextLayout == null) return null;

        return -currentLine.CenterPosition.Y;
    }

    /// <summary>
    ///     计算当前屏幕可见的行范围
    ///     返回值: (StartVisibleIndex, EndVisibleIndex)
    /// </summary>
    public override (int Start, int End) CalculateVisibleRange(
        IList<RenderLyricsLine>? lines,
        double currentScrollOffset,
        double lyricsY,
        double lyricsHeight,
        double playingLineTopOffsetFactor
    )
    {
        if (lines == null || lines.Count == 0) return (-1, -1);

        var offset = currentScrollOffset + lyricsY + lyricsHeight * playingLineTopOffsetFactor;

        var start = FindFirstVisibleLine(lines, offset, lyricsY);
        var end = FindLastVisibleLine(lines, offset, lyricsY, lyricsHeight);

        // 修正边界情况
        if (start != -1 && end == -1) end = lines.Count - 1;

        return (start, end);
    }

    public override double CalculateActualSize(IList<RenderLyricsLine>? lines)
    {
        if (lines == null || lines.Count == 0) return 0;

        return lines.Last().BottomRightPosition.Y;
    }

    public override int FindMouseHoverLineIndex(
        IList<RenderLyricsLine>? lines,
        bool isMouseInLyricsArea,
        Point mousePosition,
        double currentScrollOffset,
        double lyricsY,
        double lyricsHeight,
        double playingLineTopOffsetFactor
    )
    {
        if (!isMouseInLyricsArea) return -1;

        if (lines == null || lines.Count == 0) return -1;

        var yOffset = currentScrollOffset + lyricsHeight * playingLineTopOffsetFactor;

        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            if (line.PrimaryTextLayout == null) break;
            var lineBottomY = yOffset + line.BottomRightPosition.Y;
            if (lineBottomY >= mousePosition.Y)
            {
                result = mid;
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        if (result != -1)
        {
            var line = lines[result];
            double lineLeftX = line.TopLeftPosition.X;
            double lineRightX = line.BottomRightPosition.X;
            var lineTopY = yOffset + line.TopLeftPosition.Y;
            if (mousePosition.X < lineLeftX || mousePosition.X > lineRightX || mousePosition.Y < lineTopY) result = -1;
        }

        return result;
    }

    private static int FindFirstVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsY)
    {
        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            if (line.PrimaryTextLayout == null) break;
            var value = offset + line.BottomRightPosition.Y;
            if (value >= lyricsY)
            {
                result = mid;
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return result;
    }

    private static int FindLastVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsY,
        double lyricsHeight)
    {
        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            if (line.PrimaryTextLayout == null) break;
            var value = offset + line.BottomRightPosition.Y;
            if (value >= lyricsY + lyricsHeight)
            {
                result = mid;
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return result;
    }
}