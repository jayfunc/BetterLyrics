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

public class VerticalLyricsLayoutStrategy : LyricsLayoutStrategyBase
{
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

        // 排版
        double currentX = 0;
        double currentY = 0;

        foreach (var line in lines)
        {
            if (line == null) continue;

            double actualHeight = 0;

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

            var startX = currentX;

            // 动态图层收集
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

            // 按顺序从右往左排 (X递减)
            for (var i = 0; i < validLayers.Count; i++)
            {
                var (layer, layout, bounds) = validLayers[i];
                var type = layer.LyricsLayerType;

                var w = bounds.Width;
                var pos = new Vector2((float)(currentX - w - bounds.X), (float)(currentY - bounds.Y));

                // 赋值给对应的图层
                if (type == LyricsLayerType.Primary) line.PrimaryPosition = pos;
                else if (type == LyricsLayerType.Secondary) line.SecondaryPosition = pos;
                else if (type == LyricsLayerType.Tertiary) line.TertiaryPosition = pos;

                currentX -= w;
                actualHeight = Math.Max(actualHeight, bounds.Height);

                // 如果不是最后一个图层，则减去层间距
                if (i < validLayers.Count - 1) currentX -= w / layout.LineCount * style.LyricsLineInnerSpacingFactor;
            }

            // 初始左右边界坐标
            line.TopLeftPosition = new Vector2((float)currentX, (float)currentY);
            line.BottomRightPosition = new Vector2((float)startX, (float)(currentY + actualHeight));

            // 行间距
            if (line.PrimaryTextLayout != null)
                currentX -= line.PrimaryTextLayout.LayoutBounds.Width / line.PrimaryTextLayout.LineCount *
                            style.LyricsLineOverallSpacingFactor;

            // 计算全局 Y 轴偏移量
            var offsetY = alignment switch
            {
                TextAlignmentType.Left => 0, // 靠上
                TextAlignmentType.Center => (lyricsHeight - actualHeight) / 2, // 居中
                TextAlignmentType.Right => lyricsHeight - actualHeight, // 靠下
                _ => 0
            };

            // 应用包围盒 Y 轴偏移
            line.TopLeftPosition = line.TopLeftPosition.AddY((float)offsetY);
            line.BottomRightPosition = line.BottomRightPosition.AddY((float)offsetY);

            // 偏移量也应用到图层上，相对对齐
            if (line.TertiaryTextLayout != null)
            {
                var relativeY = alignment switch
                {
                    TextAlignmentType.Center => (actualHeight - line.TertiaryTextLayout.LayoutBounds.Height) /
                                                2,
                    TextAlignmentType.Right => actualHeight - line.TertiaryTextLayout.LayoutBounds.Height,
                    _ => 0
                };
                line.TertiaryPosition = line.TertiaryPosition.AddY((float)(offsetY + relativeY));
            }

            if (line.PrimaryTextLayout != null)
            {
                var relativeY = alignment switch
                {
                    TextAlignmentType.Center => (actualHeight - line.PrimaryTextLayout.LayoutBounds.Height) / 2,
                    TextAlignmentType.Right => actualHeight - line.PrimaryTextLayout.LayoutBounds.Height,
                    _ => 0
                };
                line.PrimaryPosition = line.PrimaryPosition.AddY((float)(offsetY + relativeY));
            }

            if (line.SecondaryTextLayout != null)
            {
                var relativeY = alignment switch
                {
                    TextAlignmentType.Center => (actualHeight - line.SecondaryTextLayout.LayoutBounds.Height) /
                                                2,
                    TextAlignmentType.Right => actualHeight - line.SecondaryTextLayout.LayoutBounds.Height,
                    _ => 0
                };
                line.SecondaryPosition = line.SecondaryPosition.AddY((float)(offsetY + relativeY));
            }

            // 更新中心点
            double centerX = (line.TopLeftPosition.X + line.BottomRightPosition.X) / 2;

            line.CenterPosition = alignment switch
            {
                TextAlignmentType.Left => new Vector2((float)centerX, 0),
                TextAlignmentType.Center => new Vector2((float)centerX, (float)(lyricsHeight / 2)),
                TextAlignmentType.Right => new Vector2((float)centerX, (float)lyricsHeight),
                _ => line.CenterPosition
            };

            line.RecreateRenderChars(style.LyricsFontStrokeWidth);
        }
    }

    public override double? CalculateTargetScrollOffset(IList<RenderLyricsLine>? lines, int playingLineIndex)
    {
        if (lines == null || lines.Count == 0) return null;
        var currentLine = lines.ElementAtOrDefault(playingLineIndex);
        if (currentLine?.PrimaryTextLayout == null) return null;

        return -currentLine.CenterPosition.X;
    }

    public override (int Start, int End) CalculateVisibleRange(
        IList<RenderLyricsLine>? lines,
        double currentScrollOffset,
        double lyricsX,
        double lyricsWidth,
        double playingLineOffsetFactor)
    {
        if (lines == null || lines.Count == 0) return (-1, -1);

        var offset = currentScrollOffset + lyricsX + lyricsWidth * (1 - playingLineOffsetFactor);

        var start = FindFirstVisibleLine(lines, offset, lyricsX, lyricsWidth);
        var end = FindLastVisibleLine(lines, offset, lyricsX);

        // 修正边界情况
        if (start != -1 && end == -1) end = lines.Count - 1;

        return (start, end);
    }

    public override int FindMouseHoverLineIndex(
        IList<RenderLyricsLine>? lines, bool isMouseInLyricsArea, Point mousePosition,
        double currentScrollOffset, double lyricsX, double lyricsWidth, double playingLineOffsetFactor)
    {
        if (!isMouseInLyricsArea) return -1;

        if (lines == null || lines.Count == 0) return -1;

        var xOffset = currentScrollOffset + lyricsWidth * (1 - playingLineOffsetFactor);

        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            if (line.PrimaryTextLayout == null) break;
            var lineLeftX = xOffset + line.TopLeftPosition.X;
            if (lineLeftX <= mousePosition.X)
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
            double lineTopY = line.TopLeftPosition.Y;
            double lineBottomY = line.BottomRightPosition.Y;
            var lineRightX = xOffset + line.BottomRightPosition.X;
            if (mousePosition.X > lineRightX || mousePosition.Y < lineTopY || mousePosition.Y > lineBottomY)
                result = -1;
        }

        return result;
    }

    public override double CalculateActualSize(IList<RenderLyricsLine>? lines)
    {
        if (lines == null || lines.Count == 0) return 0;
        return Math.Abs(lines.Last().TopLeftPosition.X);
    }

    private static int FindFirstVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsX,
        double lyricsWidth)
    {
        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            var value = offset + line.TopLeftPosition.X;
            if (value <= lyricsX + lyricsWidth)
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

    private static int FindLastVisibleLine(IList<RenderLyricsLine> lines, double offset, double lyricsX)
    {
        int left = 0, right = lines.Count - 1, result = -1;
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var line = lines[mid];
            var value = offset + line.TopLeftPosition.X;
            if (value <= lyricsX)
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