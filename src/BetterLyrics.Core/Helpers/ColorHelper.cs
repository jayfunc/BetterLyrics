// 2025/6/23 by Zhe Fang

using System.Numerics;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Helpers;

public static class ColorHelper
{
    private static readonly ISystemUIProvider _systemUiProvider =
        Ioc.Default.GetRequiredService<ISystemUIProvider>();

    public static AppTheme GetElementThemeFromBackgroundColor(AppColor backgroundColor)
    {
        // 计算亮度（YIQ公式）
        var yiq =
            (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114)
            / 1000.0;
        return yiq >= 128 ? AppTheme.Light : AppTheme.Dark;
    }

    public static AppColor GetForegroundColor(AppColor background)
    {
        // 转为 HSL
        var (h, s, l) = ToHsl(background);

        // 目标亮度与背景错开，但不极端
        double targetL;
        if (l >= 0.7)
            targetL = 0.35; // 背景很亮，前景适中偏暗
        else if (l <= 0.3)
            targetL = 0.75; // 背景很暗，前景适中偏亮
        else
            targetL = l > 0.5 ? l - 0.35 : l + 0.35; // 其余情况适度错开

        // 保持色相，适当提升饱和度
        var targetS = Math.Min(1.0, s + 0.2);

        // 转回 Color
        var fg = FromHsl(h, targetS, targetL);

        // 保持不透明
        return new AppColor(255, fg.R, fg.G, fg.B);
    }

    public static AppColor GetInterpolatedColor(double progress, AppColor startColor, AppColor targetColor)
    {
        return new AppColor(
            Lerp(progress, startColor.A, targetColor.A),
            Lerp(progress, startColor.R, targetColor.R),
            Lerp(progress, startColor.G, targetColor.G),
            Lerp(progress, startColor.B, targetColor.B)
        );
    }

    public static AppColor FromVector3(Vector3 vector3)
    {
        return new AppColor(255, (byte)vector3.X, (byte)vector3.Y, (byte)vector3.Z);
    }

    public static AppColor GetHarmoniousColor(AppColor color, double factor = 0.2)
    {
        if (color.A == 0) return Colors.Transparent;

        var brightness = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;

        byte r, g, b;

        if (brightness > 128)
        {
            r = (byte)Math.Max(0, color.R * (1 - factor));
            g = (byte)Math.Max(0, color.G * (1 - factor));
            b = (byte)Math.Max(0, color.B * (1 - factor));
        }
        else
        {
            r = (byte)Math.Min(255, color.R + (255 - color.R) * factor);
            g = (byte)Math.Min(255, color.G + (255 - color.G) * factor);
            b = (byte)Math.Min(255, color.B + (255 - color.B) * factor);
        }

        return new AppColor(color.A, r, g, b);
    }

    public static AppColor GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode)
    {
        return _systemUiProvider.GetAccentColor(myHwnd, mode);
    }

    /// <summary>
    /// </summary>
    /// <param name="color"></param>
    /// <returns>H: 0-360, S: 0-1, L: 0-1</returns>
    public static (double, double, double) ToHsl(AppColor color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double h = 0;
        double s = 0;
        var l = (max + min) / 2.0;

        if (delta != 0)
        {
            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (max == r)
                h = (g - b) / delta + (g < b ? 6 : 0);
            else if (max == g)
                h = (b - r) / delta + 2;
            else
                h = (r - g) / delta + 4;

            h /= 6.0;
        }

        return (h * 360, s, l); // h * 360: 0-360, s: 0-1, l: 0-1
    }

    /// <summary>
    /// </summary>
    /// <param name="h">0-360</param>
    /// <param name="s">0-1</param>
    /// <param name="l">0-1</param>
    /// <param name="a">0-1</param>
    /// <returns></returns>
    public static AppColor FromHsl(double h, double s, double l, double a = 1)
    {
        var hNorm = h / 360.0;
        var sNorm = s;
        var lNorm = l;

        double r, g, b;

        if (sNorm == 0)
        {
            r = g = b = lNorm; // 非彩色（灰度）
        }
        else
        {
            var q = lNorm < 0.5 ? lNorm * (1 + sNorm) : lNorm + sNorm - lNorm * sNorm;
            var p = 2 * lNorm - q;

            r = HueToRgb(p, q, hNorm + 1.0 / 3.0);
            g = HueToRgb(p, q, hNorm);
            b = HueToRgb(p, q, hNorm - 1.0 / 3.0);
        }

        return new AppColor((byte)Math.Round(a * 255), (byte)Math.Round(r * 255), (byte)Math.Round(g * 255),
            (byte)Math.Round(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }

    private static byte Lerp(double progress, byte a, byte b)
    {
        return (byte)(a + progress * (b - a));
    }
}