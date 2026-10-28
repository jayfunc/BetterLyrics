using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

/// <summary>
/// 一段无歌词间隙中需要绘制呼吸圆点的信息
/// </summary>
/// <param name="StartMs">间隙开始时间</param>
/// <param name="EndMs">间隙结束时间</param>
/// <param name="ReferenceLineIndex">用于取滚动位移、对齐与圆点大小的参考歌词行索引</param>
/// <param name="FlowOffset">圆点在排版方向上的绝对坐标（横排为 Y，竖排为 X）</param>
/// <param name="LineHeight">参考行高，用于计算圆点大小</param>
public readonly record struct GapIndicatorState(
    double StartMs,
    double EndMs,
    int ReferenceLineIndex,
    double FlowOffset,
    double LineHeight);

/// <summary>
/// 无歌词间隙中的呼吸圆点指示器（参考 Apple Music 的三点动画）
/// </summary>
public sealed class GapIndicatorRenderer
{
    private const double FadeInMs = 300;
    private const double FadeOutMs = 250;

    private const double PulsePeriodMs = 2000;
    private const int DotCount = 3;

    // 相位错开 1/6 周期，形成从左到右流动的呼吸波
    private const double DotPhaseOffset = 1.0 / 6.0;

    private const double MinDensity = 0.3;
    private const double DotRadiusRatio = 0.13;
    private const double DotSpacingRatio = 3.0;

    // 呼吸时的缩放区间：最暗时缩到 70%，最亮时回到 100%
    private const double MinDotScale = 0.7;

    private const double MinDotRadius = 3.5;
    private const double MaxDotRadius = 16;

    /// <summary>
    /// 圆点组的整体宽度，用于对齐
    /// </summary>
    public static double MeasureWidth(double lineHeight)
    {
        var radius = GetRadius(lineHeight);
        return radius * DotSpacingRatio * (DotCount - 1) + radius * 2;
    }

    public void Draw(
        CanvasDrawingSession ds,
        LyricsWindowStatus status,
        Vector2 center,
        bool isVertical,
        double timeMs,
        GapIndicatorState indicator)
    {
        var opacity = CalculateOpacity(timeMs, indicator.StartMs, indicator.EndMs);
        if (opacity <= 0.001) return;

        var radius = GetRadius(indicator.LineHeight);
        var spacing = radius * DotSpacingRatio;
        var phase = (timeMs - indicator.StartMs) / PulsePeriodMs;

        var baseColor = ColorExtensions.FromAppColor(status.WindowPalette.PlayedCurrentLineFillColor);

        for (var i = 0; i < DotCount; i++)
        {
            var wave = 0.5 - 0.5 * Math.Cos(2 * Math.PI * (phase - i * DotPhaseOffset));
            var alpha = (MinDensity + (1 - MinDensity) * wave) * opacity;

            var color = Color.FromArgb(
                (byte)Math.Clamp(alpha * 255, 0, 255),
                baseColor.R,
                baseColor.G,
                baseColor.B);

            var dotRadius = (float)(radius * (MinDotScale + (1 - MinDotScale) * wave));
            var offset = (i - (DotCount - 1) / 2.0) * spacing;

            var position = isVertical
                ? new Vector2(center.X, center.Y + (float)offset)
                : new Vector2(center.X + (float)offset, center.Y);

            ds.FillCircle(position, dotRadius, color);
        }
    }

    private static double GetRadius(double lineHeight)
    {
        return Math.Clamp(lineHeight * DotRadiusRatio, MinDotRadius, MaxDotRadius);
    }

    private static double CalculateOpacity(double timeMs, double startMs, double endMs)
    {
        var fadeIn = FadeInMs > 0 ? Math.Clamp((timeMs - startMs) / FadeInMs, 0, 1) : 1;
        var fadeOut = endMs > startMs ? Math.Clamp((endMs - timeMs) / FadeOutMs, 0, 1) : 1;

        return Math.Min(fadeIn, fadeOut);
    }
}
