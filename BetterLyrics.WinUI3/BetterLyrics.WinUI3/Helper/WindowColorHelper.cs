using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowColorHelper
    {
        public static Color GetDominantColorBelow(
            IntPtr myHwnd,
            int sampleWidth = 64,
            int sampleHeight = 64
        )
        {
            // 获取屏幕坐标中，在窗口下方的某个点
            if (!GetWindowRect(myHwnd, out RECT myRect))
                return Color.Transparent;

            POINT pt = new()
            {
                x = (myRect.Left + myRect.Right) / 2,
                y = myRect.Bottom + 1, // 紧贴窗口底部
            };

            IntPtr hwndBelow = WindowFromPoint(pt);

            if (hwndBelow == myHwnd || hwndBelow == IntPtr.Zero)
                return Color.Transparent;

            return GetAverageColorFromWindow(hwndBelow, sampleWidth, sampleHeight);
        }

        private static Color GetAverageColorFromWindow(IntPtr hwnd, int width, int height)
        {
            if (!GetWindowRect(hwnd, out RECT rect))
                return Color.Transparent;

            int w = Math.Min(width, rect.Right - rect.Left);
            int h = Math.Min(height, rect.Bottom - rect.Top);

            using Bitmap bmp = new(w, h, PixelFormat.Format32bppArgb);
            using Graphics gDest = Graphics.FromImage(bmp);

            IntPtr hdcDest = gDest.GetHdc();
            IntPtr hdcSrc = GetWindowDC(hwnd);

            BitBlt(hdcDest, 0, 0, w, h, hdcSrc, rect.Left, rect.Top, SRCCOPY);

            gDest.ReleaseHdc(hdcDest);
            ReleaseDC(hwnd, hdcSrc);

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
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
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
