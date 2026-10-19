using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Vanara.PInvoke;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class SystemUIProvider : ISystemUIProvider
{
    public AppColor GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode)
    {
        if (!User32.GetWindowRect(myHwnd, out var myRect)) return Colors.Transparent;

        switch (mode)
        {
            case WindowPixelSampleMode.BelowWindow:
                using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Bottom + 2, myRect.Width, 1))
                {
                    return ComputeAverageColor(bmp);
                }

            case WindowPixelSampleMode.AboveWindow:
                using (var bmp = CaptureScreenRegion(myRect.Left, myRect.Top - 2, myRect.Width, 1))
                {
                    return ComputeAverageColor(bmp);
                }

            case WindowPixelSampleMode.WindowArea:
                {
                    var width = myRect.Right - myRect.Left;
                    var height = myRect.Bottom - myRect.Top;
                    if (width <= 0 || height <= 0) return Colors.Transparent;

                    var inset = 10;

                    if (width <= inset * 2 || height <= inset * 2)
                    {
                        using var bmp = CaptureScreenRegion(myRect.Left, myRect.Top, width, height);
                        return ComputeAverageColor(bmp);
                    }

                    List<Bitmap> innerBmps = [];
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
                    var width = myRect.Right - myRect.Left;
                    var height = myRect.Bottom - myRect.Top;
                    if (width <= 0 || height <= 0) return Colors.Transparent;

                    var edgeThickness = new AppThickness(36, 36, 36, 36);
                    List<Bitmap> edgeBmps = [];

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
                    var wallpaperPath = GetCurrentWallpaper();
                    return GetDominantColorFromImage(wallpaperPath);
                }
            default:
                return Colors.Transparent;
        }
    }

    public string GetWallpaperPath(AppRect monitorBounds)
    {
        return GetCurrentWallpaper(monitorBounds);
    }

    public AppTheme GetAppTheme()
    {
        return Application.Current.RequestedTheme switch
        {
            ApplicationTheme.Light => AppTheme.Light,
            ApplicationTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Default
        };
    }

    public AppTheme GetSystemTheme()
    {
        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = (int)key.GetValue("SystemUsesLightTheme", 1);
                return value == 1 ? AppTheme.Light : AppTheme.Dark;
            }
        }
        catch
        {
        }

        return AppTheme.Dark;
    }

    public void SetAppLanguage(string languageCode)
    {
        ApplicationLanguages.PrimaryLanguageOverride = languageCode;
    }

    private static string GetCurrentWallpaper(AppRect? monitorBounds = null)
    {
        string? firstWallpaperPath = null;

        try
        {
            var desktopWallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();

            for (uint index = 0; ; index++)
            {
                if (desktopWallpaper.GetMonitorDevicePathAt(index, out var monitorId) != HRESULT.S_OK)
                    break;

                // 获取该显示器的壁纸路径
                if (desktopWallpaper.GetWallpaper(monitorId, out var wallpaperPath) != HRESULT.S_OK ||
                    string.IsNullOrWhiteSpace(wallpaperPath))
                    continue;

                firstWallpaperPath ??= wallpaperPath;

                if (monitorBounds == null) return wallpaperPath;

                if (desktopWallpaper.GetMonitorRECT(monitorId, out var monitorRect) == HRESULT.S_OK &&
                    IsSameMonitor(monitorRect, monitorBounds))
                    return wallpaperPath;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取壁纸失败: {ex.Message}");
        }

        return firstWallpaperPath ?? GetTranscodedWallpaperPath();
    }

    private static bool IsSameMonitor(RECT monitorRect, AppRect monitorBounds)
    {
        const double tolerance = 1;

        return Math.Abs(monitorRect.Left - monitorBounds.X) <= tolerance &&
               Math.Abs(monitorRect.Top - monitorBounds.Y) <= tolerance &&
               Math.Abs(monitorRect.Width - monitorBounds.Width) <= tolerance &&
               Math.Abs(monitorRect.Height - monitorBounds.Height) <= tolerance;
    }

    private static string GetTranscodedWallpaperPath()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Themes",
            "TranscodedWallpaper");

        return File.Exists(path) ? path : string.Empty;
    }

    private static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
    {
        var sampleWidth = Math.Min(width, 64);
        var sampleHeight = Math.Min(height, 64);
        sampleWidth = Math.Max(1, sampleWidth);
        sampleHeight = Math.Max(1, sampleHeight);

        var bmp = new Bitmap(sampleWidth, sampleHeight, PixelFormat.Format32bppArgb);
        using var gDest = Graphics.FromImage(bmp);

        var hdcDest = gDest.GetHdc();
        var hdcSrc = (nint)User32.GetDC(IntPtr.Zero);

        Gdi32.StretchBlt(hdcDest, 0, 0, sampleWidth, sampleHeight, hdcSrc, x, y, width, height,
            Gdi32.RasterOperationMode.SRCCOPY);

        gDest.ReleaseHdc(hdcDest);
        User32.ReleaseDC(IntPtr.Zero, hdcSrc);

        return bmp;
    }

    private static AppColor ComputeAverageColor(params Bitmap[] bmps)
    {
        if (bmps == null || bmps.Length == 0) return Colors.Transparent;

        long totalR = 0, totalG = 0, totalB = 0;
        long totalPixels = 0;

        foreach (var bmp in bmps)
            for (var y = 0; y < bmp.Height; y++)
                for (var x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);

                    // 纯粹的累加所有像素的 RGB
                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    totalPixels++;
                }

        if (totalPixels == 0) return Colors.Transparent;

        // 直接计算并返回平均值
        var avgR = (byte)(totalR / totalPixels);
        var avgG = (byte)(totalG / totalPixels);
        var avgB = (byte)(totalB / totalPixels);

        return new AppColor(255, avgR, avgG, avgB);
    }

    private static AppColor GetDominantColorFromImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return Colors.Transparent;

        try
        {
            using var originalBmp = new Bitmap(imagePath);
            using var bmp = new Bitmap(originalBmp, new Size(64, 64));
            return ComputeAverageColor(bmp);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取壁纸提取主题色失败: {ex.Message}");
            return Colors.Transparent;
        }
    }
}