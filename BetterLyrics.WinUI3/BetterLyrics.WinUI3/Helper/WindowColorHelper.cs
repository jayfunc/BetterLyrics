using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowColorHelper
    {
        public static Color GetDominantColor(IntPtr myHwnd, WindowColorSampleMode mode)
        {
            if (!User32.GetWindowRect(myHwnd, out RECT myRect)) return Color.Transparent;

            switch (mode)
            {
                case WindowColorSampleMode.BelowWindow:
                    {
                        int screenWidth = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN);
                        int sampleHeight = 1;
                        int sampleY = myRect.Bottom + 1;
                        return GetAverageColorFromScreenRegion(0, sampleY, screenWidth, sampleHeight);
                    }
                case WindowColorSampleMode.WindowArea:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0)
                            return Color.Transparent;
                        // 采集窗口区域的平均色
                        return GetAverageColorFromScreenRegion(myRect.Left, myRect.Top, width, height);
                    }
                case WindowColorSampleMode.WindowEdge:
                    {
                        int width = myRect.Right - myRect.Left;
                        int height = myRect.Bottom - myRect.Top;
                        if (width <= 0 || height <= 0)
                            return Color.Transparent;

                        var edgeThickness = new Thickness(36, 0, 36, 0);
                        List<Color> edgeColors = [];

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
                            return Color.Transparent;
                        long r = 0,
                            g = 0,
                            b = 0;
                        foreach (var c in edgeColors)
                        {
                            r += c.R;
                            g += c.G;
                            b += c.B;
                        }
                        return Color.FromArgb(
                            255,
                            (int)(r / edgeColors.Count),
                            (int)(g / edgeColors.Count),
                            (int)(b / edgeColors.Count)
                        );
                    }
                default:
                    return Color.Transparent;
            }
        }

        private static Color GetAverageColorFromScreenRegion(int x, int y, int width, int height)
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

        private static Color ComputeAverageColor(Bitmap bmp)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    count++;
                }
            }

            if (count == 0) return Color.Transparent;
            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }
    }
}
