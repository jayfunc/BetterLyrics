using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Media.TextFormatting;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Models.Lyrics;

namespace BetterLyrics.Avalonia.Helpers.Lyrics.LyricsLayoutStrategy;

public class HorizontalLyricsLayoutStrategy : LyricsLayoutStrategyBase
{
    public override void MeasureAndArrange(
        IList<RenderLyricsLine>? lines,
        LyricsWindowStatus status,
        AppSettings appSettings,
        double canvasWidth,
        double canvasHeight,
        double lyricsWidth,
        double lyricsHeight)
    {
        if (lines == null) return;

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

        double currentX = 0;
        double currentY = 0;

        foreach (var line in lines)
        {
            if (line == null) continue;

            double actualWidth = 0;

            var alignment = style.UseInternalLyricsAlignment
                ? line.HorizontalAlignmentType ?? style.LyricsAlignmentType
                : style.LyricsAlignmentType;

            // Notice ICanvasAnimatedControl parameter is gone
            line.RecreateTextLayout(
                appSettings.TranslationSettings.IsChineseRomanizationEnabled ||
                appSettings.TranslationSettings.IsJapaneseRomanizationEnabled,
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

            var startY = currentY;

            var validLayers = new List<(LyricsLayerConfig Type, TextLayout Layout, Rect Bounds)>();
            foreach (var layer in style.LyricsLayerOrder)
            {
                var layout = layer.LyricsLayerType switch
                {
                    LyricsLayerType.Primary => line.PrimaryTextLayout,
                    LyricsLayerType.Secondary => line.SecondaryTextLayout,
                    LyricsLayerType.Tertiary => line.TertiaryTextLayout,
                    _ => null
                };

                // Avalonia's TextLayout uses Width/Height directly for bounds representation
                if (layout != null) validLayers.Add((layer, layout, new Rect(0, 0, layout.Width, layout.Height)));
            }

            for (var i = 0; i < validLayers.Count; i++)
            {
                var (layer, layout, bounds) = validLayers[i];
                var type = layer.LyricsLayerType;

                var pos = new Vector2((float)(currentX - bounds.X), (float)(currentY - bounds.Y));

                if (type == LyricsLayerType.Primary) line.PrimaryPosition = pos;
                else if (type == LyricsLayerType.Secondary) line.SecondaryPosition = pos;
                else if (type == LyricsLayerType.Tertiary) line.TertiaryPosition = pos;

                currentY += bounds.Height;
                actualWidth = Math.Max(actualWidth, bounds.Width);

                if (i < validLayers.Count - 1)
                {
                    // Swapped LineCount to Avalonia's TextLines.Count
                    currentY += bounds.Height / layout.TextLines.Count * style.LyricsLineInnerSpacingFactor;
                }
            }

            line.TopLeftPosition = new Vector2((float)currentX, (float)startY);
            line.BottomRightPosition = new Vector2((float)(currentX + actualWidth), (float)currentY);

            if (line.PrimaryTextLayout != null)
                currentY += line.PrimaryTextLayout.Height / line.PrimaryTextLayout.TextLines.Count *
                            style.LyricsLineOverallSpacingFactor;

            var offsetX = alignment switch
            {
                TextAlignmentType.Left => 0,
                TextAlignmentType.Center => (lyricsWidth - actualWidth) / 2,
                TextAlignmentType.Right => lyricsWidth - actualWidth,
                _ => 0
            };

            line.TopLeftPosition = line.TopLeftPosition.AddX((float)offsetX);
            line.BottomRightPosition = line.BottomRightPosition.AddX((float)offsetX);

            if (line.TertiaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.TertiaryTextLayout.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.TertiaryTextLayout.Width,
                    _ => 0
                };
                line.TertiaryPosition = line.TertiaryPosition.AddX((float)(offsetX + relativeX));
            }

            if (line.PrimaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.PrimaryTextLayout.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.PrimaryTextLayout.Width,
                    _ => 0
                };
                line.PrimaryPosition = line.PrimaryPosition.AddX((float)(offsetX + relativeX));
            }

            if (line.SecondaryTextLayout != null)
            {
                var relativeX = alignment switch
                {
                    TextAlignmentType.Center => (actualWidth - line.SecondaryTextLayout.Width) / 2,
                    TextAlignmentType.Right => actualWidth - line.SecondaryTextLayout.Width,
                    _ => 0
                };
                line.SecondaryPosition = line.SecondaryPosition.AddX((float)(offsetX + relativeX));
            }

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

    public override double? CalculateTargetScrollOffset(IList<RenderLyricsLine>? lines, int playingLineIndex)
    {
        if (lines == null || lines.Count == 0) return null;
        var currentLine = lines.ElementAtOrDefault(playingLineIndex);
        if (currentLine?.PrimaryTextLayout == null) return null;

        return -currentLine.CenterPosition.Y;
    }

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