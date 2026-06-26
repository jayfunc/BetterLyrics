using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Providers
{
    public class SystemUIProvider : ISystemUIProvider
    {
        public void ShowToast(string localizedTitleKey, string? message = null,
            MessageSeverity severity = MessageSeverity.Informational, TimeSpan? duration = null)
        {
            GlobalToastManager.Show("Error", message, severity, duration);
        }

        public (string, AppRect) GetPrimaryMonitorInfo()
        {
            var primaryMonitorInfoEx = MonitorHook.GetPrimaryMonitorInfoEx();
            var monitorRect = primaryMonitorInfoEx.rcMonitor;

            return (primaryMonitorInfoEx.szDevice, monitorRect.ToAppRect());
        }

        public AppColor GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode)
        {
            if (!User32.GetWindowRect(myHwnd, out RECT myRect)) return Colors.Transparent;

            switch (mode)
            {
                case WindowPixelSampleMode.BelowWindow:
                    using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Bottom + 2, myRect.Width, 1))
                        return ComputeAverageColor(bmp);

                case WindowPixelSampleMode.AboveWindow:
                    using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Top - 2, myRect.Width, 1))
                        return ComputeAverageColor(bmp);

                case WindowPixelSampleMode.WindowArea:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0) return Colors.Transparent;

                        int inset = 10;

                        if (width <= inset * 2 || height <= inset * 2)
                        {
                            using var bmp = CaptureScreenRegion(myRect.Left, myRect.Top, width, height);
                            return ComputeAverageColor(bmp);
                        }

                        List<System.Drawing.Bitmap> innerBmps = [];
                        try
                        {
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top, width, inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Bottom - inset, width, inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top + inset, inset, height - 2 * inset));
                            innerBmps.Add(CaptureScreenRegion(myRect.Right - inset, myRect.Top + inset, inset,
                                height - 2 * inset));

                            return ComputeAverageColor([.. innerBmps]);
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

                        var edgeThickness = new AppThickness(36, 36, 36, 36);
                        List<System.Drawing.Bitmap> edgeBmps = [];

                        try
                        {
                            if (edgeThickness.Top > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Top - (int)edgeThickness.Top, width,
                                    (int)edgeThickness.Top));
                            if (edgeThickness.Bottom > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left, myRect.Bottom, width,
                                    (int)edgeThickness.Bottom));
                            if (edgeThickness.Left > 0)
                                edgeBmps.Add(CaptureScreenRegion(myRect.Left - (int)edgeThickness.Left, myRect.Top,
                                    (int)edgeThickness.Left, height));
                            if (edgeThickness.Right > 0)
                                edgeBmps.Add(
                                    CaptureScreenRegion(myRect.Right, myRect.Top, (int)edgeThickness.Right, height));

                            return ComputeAverageColor([.. edgeBmps]);
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

            Gdi32.StretchBlt(hdcDest, 0, 0, sampleWidth, sampleHeight, hdcSrc, x, y, width, height,
                Gdi32.RasterOperationMode.SRCCOPY);

            gDest.ReleaseHdc(hdcDest);
            User32.ReleaseDC(IntPtr.Zero, hdcSrc);

            return bmp;
        }

        private static AppColor ComputeAverageColor(params System.Drawing.Bitmap[] bmps)
        {
            if (bmps == null || bmps.Length == 0) return Colors.Transparent;

            long totalR = 0, totalG = 0, totalB = 0;
            long totalPixels = 0;

            foreach (var bmp in bmps)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        System.Drawing.Color pixel = bmp.GetPixel(x, y);

                        // 纯粹的累加所有像素的 RGB
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;
                        totalPixels++;
                    }
                }
            }

            if (totalPixels == 0) return Colors.Transparent;

            // 直接计算并返回平均值
            byte avgR = (byte)(totalR / totalPixels);
            byte avgG = (byte)(totalG / totalPixels);
            byte avgB = (byte)(totalB / totalPixels);

            return new AppColor(255, avgR, avgG, avgB);
        }

        private static AppColor GetDominantColorFromImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
                return Colors.Transparent;

            try
            {
                using var originalBmp = new System.Drawing.Bitmap(imagePath);
                using var bmp = new System.Drawing.Bitmap(originalBmp, new System.Drawing.Size(64, 64));
                return ComputeAverageColor(bmp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取壁纸提取主题色失败: {ex.Message}");
                return Colors.Transparent;
            }
        }
    }
}