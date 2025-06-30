// 2025/6/23 by Zhe Fang

using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public static class ColorHelper
    {
        public static ElementTheme GetElementThemeFromBackgroundColor(Color backgroundColor)
        {
            // 计算亮度（YIQ公式）
            double yiq =
                ((backgroundColor.R * 299) + (backgroundColor.G * 587) + (backgroundColor.B * 114))
                / 1000.0;
            return yiq >= 128 ? ElementTheme.Light : ElementTheme.Dark;
        }

        public static Color GetForegroundColor(Color background)
        {
            // 转为 HSL
            var hsl = CommunityToolkit.WinUI.Helpers.ColorHelper.ToHsl(background);
            double h = hsl.H;
            double s = hsl.S;
            double l = hsl.L;

            // 目标亮度与背景错开，但不极端
            double targetL;
            if (l >= 0.7)
                targetL = 0.35; // 背景很亮，前景适中偏暗
            else if (l <= 0.3)
                targetL = 0.75; // 背景很暗，前景适中偏亮
            else
                targetL = l > 0.5 ? l - 0.35 : l + 0.35; // 其余情况适度错开

            // 保持色相，适当提升饱和度
            double targetS = Math.Min(1.0, s + 0.2);

            // 转回 Color
            var fg = CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(h, targetS, targetL);

            // 保持不透明
            return Color.FromArgb(255, fg.R, fg.G, fg.B);
        }

        public static Color GetInterpolatedColor(float progress, Color startColor, Color targetColor)
        {
            byte Lerp(byte a, byte b) => (byte)(a + (progress * (b - a)));
            return Color.FromArgb(
                Lerp(startColor.A, targetColor.A),
                Lerp(startColor.R, targetColor.R),
                Lerp(startColor.G, targetColor.G),
                Lerp(startColor.B, targetColor.B)
            );
        }

        public static Color ToColor(this int argb)
        {
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;

            // 还原非预乘分量
            if (a == 0)
                return Color.FromArgb(0, 0, 0, 0);

            // 预乘解码
            // 这里 a+1 是编码时的分母
            int ap1 = a + 1;
            r = (byte)Math.Min(255, (r * 255 + (ap1 / 2)) / ap1);
            g = (byte)Math.Min(255, (g * 255 + (ap1 / 2)) / ap1);
            b = (byte)Math.Min(255, (b * 255 + (ap1 / 2)) / ap1);

            return Color.FromArgb(a, r, g, b);
        }

        public static Color ToColor(this System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
