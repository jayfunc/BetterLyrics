// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using Vanara.PInvoke;
using Windows.UI;

using Color = Windows.UI.Color;

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

        public static Color GetInterpolatedColor(double progress, Color startColor, Color targetColor)
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

        public static Color WithAlpha(this Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static Color WithOpacity(this Color color, float opacity)
        {
            return Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
        }

        public static Color WithBrightness(this Color color, double brightness)
        {
            // 确保亮度因子在合理范围内
            brightness = Math.Max(0, Math.Min(1, brightness));

            var hsl = CommunityToolkit.WinUI.Helpers.ColorHelper.ToHsl(color);
            double h = hsl.H;
            double s = hsl.S;

            return CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(h, s, brightness);
        }

        public static Vector3 ToVector3RGB(this Color color)
        {
            return new Vector3((float)color.R / 0xff, (float)color.G / 0xff, (float)color.B / 0xff);
        }

        public static System.Drawing.Color GetAccentColor(IntPtr myHwnd, string monitorDeviceName, WindowPixelSampleMode mode)
        {
            if (!User32.GetWindowRect(myHwnd, out RECT myRect)) return System.Drawing.Color.Transparent;

            var monitorInfo = MonitorHelper.GetMonitorInfoExFromDeviceName(monitorDeviceName);
            int screenWidth = monitorInfo.rcMonitor.Width;
            switch (mode)
            {
                case WindowPixelSampleMode.BelowWindow:
                    {
                        return GetAverageColorFromScreenRegion(myRect.Left, myRect.Bottom + 2, screenWidth, 1);
                    }
                case WindowPixelSampleMode.AboveWindow:
                    {
                        return GetAverageColorFromScreenRegion(myRect.Left, myRect.Top - 3, screenWidth, 1);
                    }
                case WindowPixelSampleMode.WindowArea:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0)
                            return System.Drawing.Color.Transparent;
                        // 采集窗口区域的平均色
                        return GetAverageColorFromScreenRegion(myRect.Left, myRect.Top, width, height);
                    }
                case WindowPixelSampleMode.WindowEdge:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0)
                            return System.Drawing.Color.Transparent;

                        var edgeThickness = new Thickness(36, 0, 36, 0);
                        List<System.Drawing.Color> edgeColors = [];

                        // Top edge
                        if (edgeThickness.Top > 0 && edgeThickness.Top < height)
                            edgeColors.Add(
                                GetAverageColorFromScreenRegion(
                                    myRect.Left,
                                    myRect.Top,
                                    width,
                                    (int)edgeThickness.Top
                                )
                            );
                        // Bottom edge
                        if (edgeThickness.Bottom > 0 && edgeThickness.Bottom < height)
                            edgeColors.Add(
                                GetAverageColorFromScreenRegion(
                                    myRect.Left,
                                    myRect.Bottom - (int)edgeThickness.Bottom,
                                    width,
                                    (int)edgeThickness.Bottom
                                )
                            );
                        // Left edge
                        if (edgeThickness.Left > 0 && edgeThickness.Left < width)
                            edgeColors.Add(
                                GetAverageColorFromScreenRegion(
                                    myRect.Left,
                                    myRect.Top + (int)edgeThickness.Top,
                                    (int)edgeThickness.Left,
                                    height - (int)edgeThickness.Top - (int)edgeThickness.Bottom
                                )
                            );
                        // Right edge
                        if (edgeThickness.Right > 0 && edgeThickness.Right < width)
                            edgeColors.Add(
                                GetAverageColorFromScreenRegion(
                                    myRect.Right - (int)edgeThickness.Right,
                                    myRect.Top + (int)edgeThickness.Top,
                                    (int)edgeThickness.Right,
                                    height - (int)edgeThickness.Top - (int)edgeThickness.Bottom
                                )
                            );

                        // 合并四边平均色
                        if (edgeColors.Count == 0)
                            return System.Drawing.Color.Transparent;
                        long r = 0,
                            g = 0,
                            b = 0;
                        foreach (var c in edgeColors)
                        {
                            r += c.R;
                            g += c.G;
                            b += c.B;
                        }
                        return System.Drawing.Color.FromArgb(
                            255,
                            (int)(r / edgeColors.Count),
                            (int)(g / edgeColors.Count),
                            (int)(b / edgeColors.Count)
                        );
                    }
                default:
                    return System.Drawing.Color.Transparent;
            }
        }

        private static System.Drawing.Color GetAverageColorFromScreenRegion(int x, int y, int width, int height)
        {
            using Bitmap bmp = new(width, height, PixelFormat.Format32bppArgb);
            using Graphics gDest = Graphics.FromImage(bmp);

            IntPtr hdcDest = gDest.GetHdc();
            IntPtr hdcSrc = (nint)User32.GetDC(IntPtr.Zero); // Entire screen

            Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, Gdi32.RasterOperationMode.SRCCOPY);

            gDest.ReleaseHdc(hdcDest);
            User32.ReleaseDC(IntPtr.Zero, hdcSrc);

            return ComputeAverageColor(bmp);
        }

        private static System.Drawing.Color ComputeAverageColor(Bitmap bmp)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    System.Drawing.Color pixel = bmp.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    count++;
                }
            }

            if (count == 0) return System.Drawing.Color.Transparent;
            return System.Drawing.Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }
    }
}
