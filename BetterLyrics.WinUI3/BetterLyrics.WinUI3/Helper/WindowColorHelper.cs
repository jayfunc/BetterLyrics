using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowColorHelper
    {
        public static Color GetDominantColorBelow(IntPtr myHwnd)
        {
            if (!GetWindowRect(myHwnd, out RECT myRect))
                return Color.Transparent;

            int screenWidth = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
            int sampleHeight = 1;
            int sampleY = myRect.Bottom + 1;

            return GetAverageColorFromScreenRegion(0, sampleY, screenWidth, sampleHeight);
        }

        private static Color GetAverageColorFromScreenRegion(int x, int y, int width, int height)
        {
            using Bitmap bmp = new(width, height, PixelFormat.Format32bppArgb);
            using Graphics gDest = Graphics.FromImage(bmp);

            IntPtr hdcDest = gDest.GetHdc();
            IntPtr hdcSrc = GetDC(IntPtr.Zero); // Entire screen

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, SRCCOPY);

            gDest.ReleaseHdc(hdcDest);
            ReleaseDC(IntPtr.Zero, hdcSrc);

            return ComputeAverageColor(bmp);
        }

        private static Color ComputeAverageColor(Bitmap bmp)
        {
            long r = 0,
                g = 0,
                b = 0;
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

            if (count == 0)
                return Color.Transparent;
            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }

        #region Win32 Imports & Structs
        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            int dwRop
        );

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric smIndex);

        private enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion
    }
}
