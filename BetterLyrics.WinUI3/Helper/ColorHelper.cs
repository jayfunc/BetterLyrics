// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Hooks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Numerics;
using Vanara.PInvoke;
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

        public static Color GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode)
        {
            if (!User32.GetWindowRect(myHwnd, out RECT myRect)) return Colors.Transparent;

            switch (mode)
            {
                case WindowPixelSampleMode.BelowWindow:
                    using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Bottom + 1, myRect.Width, 1))
                        return ComputeDominantColor(bmp);

                case WindowPixelSampleMode.AboveWindow:
                    using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Top - 2, myRect.Width, 1))
                        return ComputeDominantColor(bmp);

                case WindowPixelSampleMode.WindowArea:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0) return Colors.Transparent;

                        int inset = 10;

                        if (width <= inset * 2 || height <= inset * 2)
                        {
                            using var bmp = CaptureScreenRegion(myRect.Left, myRect.Top, width, height);
                            return ComputeDominantColor(bmp);
                        }

                        List<System.Drawing.Bitmap> innerBmps = [];
                        try
                        {
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top, width, inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Bottom - inset, width, inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top + inset, inset, height - 2 * inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Right - inset, myRect.Top + inset, inset, height - 2 * inset));

                            return ComputeDominantColor([.. innerBmps]);
                        }
                        finally
                        {
                            foreach (var bmp in innerBmps) bmp.Dispose();
                        }
                    }

                case WindowPixelSampleMode.WindowEdge:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0) return Colors.Transparent;

                        var edgeThickness = new Thickness(36, 36, 36, 36);
                        List<System.Drawing.Bitmap> edgeBmps = [];

                        try
                        {
                            if (edgeThickness.Top > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top - (int)edgeThickness.Top, width, (int)edgeThickness.Top));
                            if (edgeThickness.Bottom > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Bottom, width, (int)edgeThickness.Bottom));
                            if (edgeThickness.Left > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left - (int)edgeThickness.Left, myRect.Top, (int)edgeThickness.Left, height));
                            if (edgeThickness.Right > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Right, myRect.Top, (int)edgeThickness.Right, height));

                            return ComputeDominantColor([.. edgeBmps]);
                        }
                        finally
                        {
                            foreach (var bmp in edgeBmps) bmp.Dispose();
                        }
                    }

                case WindowPixelSampleMode.Wallpaper:
                    {
                        string wallpaperPath = GetCurrentWallpaper();
                        return GetDominantColorFromImage(wallpaperPath);
                    }
                default:
                    return Colors.Transparent;
            }
        }

        private static Color ComputeDominantColor(params System.Drawing.Bitmap[] bmps)
        {
            if (bmps == null || bmps.Length == 0) return Colors.Transparent;

            Dictionary<int, int> colorFrequencies = [];
            int dominantColorRgb = 0;
            int maxFrequency = 0;

            long fallbackR = 0, fallbackG = 0, fallbackB = 0;
            int totalCount = 0;

            foreach (var bmp in bmps)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        System.Drawing.Color pixel = bmp.GetPixel(x, y);

                        // 用于兜底的平均色统计
                        fallbackR += pixel.R;
                        fallbackG += pixel.G;
                        fallbackB += pixel.B;
                        totalCount++;

                        int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                        int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                        int saturation = max == 0 ? 0 : (max - min) * 255 / max;

                        // 过滤低饱和度或极端亮度的像素
                        if (saturation < 30 || max < 30 || max > 240)
                            continue;

                        // 颜色量化
                        int r = pixel.R & 0xF0;
                        int g = pixel.G & 0xF0;
                        int b = pixel.B & 0xF0;
                        int rgb = (r << 16) | (g << 8) | b;

                        if (colorFrequencies.TryGetValue(rgb, out int count))
                            colorFrequencies[rgb] = count + 1;
                        else
                            colorFrequencies[rgb] = 1;

                        if (colorFrequencies[rgb] > maxFrequency)
                        {
                            maxFrequency = colorFrequencies[rgb];
                            dominantColorRgb = rgb;
                        }
                    }
                }
            }

            if (maxFrequency == 0)
            {
                if (totalCount == 0) return Colors.Transparent;
                return Color.FromArgb(255, (byte)(fallbackR / totalCount), (byte)(fallbackG / totalCount), (byte)(fallbackB / totalCount));
            }

            byte finalR = (byte)Math.Min(255, ((dominantColorRgb >> 16) & 0xFF) + 8);
            byte finalG = (byte)Math.Min(255, ((dominantColorRgb >> 8) & 0xFF) + 8);
            byte finalB = (byte)Math.Min(255, (dominantColorRgb & 0xFF) + 8);

            return Color.FromArgb(255, finalR, finalG, finalB);
        }

        private static Color GetDominantColorFromImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
                return Colors.Transparent;

            try
            {
                using var originalBmp = new System.Drawing.Bitmap(imagePath);
                using var bmp = new System.Drawing.Bitmap(originalBmp, new System.Drawing.Size(64, 64));
                return ComputeDominantColor(bmp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取壁纸提取主题色失败: {ex.Message}");
                return Colors.Transparent;
            }
        }

        private static string GetCurrentWallpaper()
        {
            try
            {
                var desktopWallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();

                // 获取第一个显示器的 ID (通常索引为 0)
                // 如果你有多个显示器，可以遍历 GetMonitorDevicePathCount
                if (desktopWallpaper.GetMonitorDevicePathAt(0, out string? monitorId) == HRESULT.S_OK)
                {
                    // 获取该显示器的壁纸路径
                    if (desktopWallpaper.GetWallpaper(monitorId, out string wallpaperPath) == HRESULT.S_OK)
                    {
                        return wallpaperPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取壁纸失败: {ex.Message}");
            }
            return string.Empty;
        }

        private static System.Drawing.Bitmap CaptureScreenRegion(int x, int y, int width, int height)
        {
            int sampleWidth = Math.Min(width, 64);
            int sampleHeight = Math.Min(height, 64);
            sampleWidth = Math.Max(1, sampleWidth);
            sampleHeight = Math.Max(1, sampleHeight);

            var bmp = new System.Drawing.Bitmap(sampleWidth, sampleHeight, PixelFormat.Format32bppArgb);
            using var gDest = System.Drawing.Graphics.FromImage(bmp);

            IntPtr hdcDest = gDest.GetHdc();
            IntPtr hdcSrc = (nint)User32.GetDC(IntPtr.Zero);

            Gdi32.StretchBlt(hdcDest, 0, 0, sampleWidth, sampleHeight, hdcSrc, x, y, width, height, Gdi32.RasterOperationMode.SRCCOPY);

            gDest.ReleaseHdc(hdcDest);
            User32.ReleaseDC(IntPtr.Zero, hdcSrc);

            return bmp;
        }

        public static Color FromVector3(Vector3 vector3) => Color.FromArgb(255, (byte)vector3.X, (byte)vector3.Y, (byte)vector3.Z);
    }
}
