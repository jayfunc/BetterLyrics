// 2025/6/23 by Zhe Fang

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="WindowColorHelper" />
    /// </summary>
    public static class WindowColorHelper
    {
        #region Constants

        /// <summary>
        /// Defines the SRCCOPY
        /// </summary>
        private const int SRCCOPY = 0x00CC0020;

        #endregion

        #region Enums

        /// <summary>
        /// Defines the SystemMetric
        /// </summary>
        private enum SystemMetric
        {
            /// <summary>
            /// Defines the SM_CXSCREEN
            /// </summary>
            SM_CXSCREEN = 0,

            /// <summary>
            /// Defines the SM_CYSCREEN
            /// </summary>
            SM_CYSCREEN = 1,
        }

        #endregion

        #region Methods

        /// <summary>
        /// The GetDominantColorBelow
        /// </summary>
        /// <param name="myHwnd">The myHwnd<see cref="IntPtr"/></param>
        /// <returns>The <see cref="Color"/></returns>
        public static Color GetDominantColorBelow(IntPtr myHwnd)
        {
            if (!GetWindowRect(myHwnd, out RECT myRect))
                return Color.Transparent;

            int screenWidth = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
            int sampleHeight = 1;
            int sampleY = myRect.Bottom + 1;

            return GetAverageColorFromScreenRegion(0, sampleY, screenWidth, sampleHeight);
        }

        /// <summary>
        /// The BitBlt
        /// </summary>
        /// <param name="hdcDest">The hdcDest<see cref="IntPtr"/></param>
        /// <param name="nXDest">The nXDest<see cref="int"/></param>
        /// <param name="nYDest">The nYDest<see cref="int"/></param>
        /// <param name="nWidth">The nWidth<see cref="int"/></param>
        /// <param name="nHeight">The nHeight<see cref="int"/></param>
        /// <param name="hdcSrc">The hdcSrc<see cref="IntPtr"/></param>
        /// <param name="nXSrc">The nXSrc<see cref="int"/></param>
        /// <param name="nYSrc">The nYSrc<see cref="int"/></param>
        /// <param name="dwRop">The dwRop<see cref="int"/></param>
        /// <returns>The <see cref="bool"/></returns>
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

        /// <summary>
        /// The ComputeAverageColor
        /// </summary>
        /// <param name="bmp">The bmp<see cref="Bitmap"/></param>
        /// <returns>The <see cref="Color"/></returns>
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

        /// <summary>
        /// The GetAverageColorFromScreenRegion
        /// </summary>
        /// <param name="x">The x<see cref="int"/></param>
        /// <param name="y">The y<see cref="int"/></param>
        /// <param name="width">The width<see cref="int"/></param>
        /// <param name="height">The height<see cref="int"/></param>
        /// <returns>The <see cref="Color"/></returns>
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

        /// <summary>
        /// The GetDC
        /// </summary>
        /// <param name="hWnd">The hWnd<see cref="IntPtr"/></param>
        /// <returns>The <see cref="IntPtr"/></returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// The GetSystemMetrics
        /// </summary>
        /// <param name="smIndex">The smIndex<see cref="SystemMetric"/></param>
        /// <returns>The <see cref="int"/></returns>
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric smIndex);

        /// <summary>
        /// The GetWindowRect
        /// </summary>
        /// <param name="hWnd">The hWnd<see cref="IntPtr"/></param>
        /// <param name="lpRect">The lpRect<see cref="RECT"/></param>
        /// <returns>The <see cref="bool"/></returns>
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// The ReleaseDC
        /// </summary>
        /// <param name="hWnd">The hWnd<see cref="IntPtr"/></param>
        /// <param name="hDC">The hDC<see cref="IntPtr"/></param>
        /// <returns>The <see cref="int"/></returns>
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        #endregion

        /// <summary>
        /// Defines the <see cref="RECT" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            #region Fields

            /// <summary>
            /// Defines the Bottom
            /// </summary>
            public int Bottom;

            /// <summary>
            /// Defines the Left
            /// </summary>
            public int Left;

            /// <summary>
            /// Defines the Right
            /// </summary>
            public int Right;

            /// <summary>
            /// Defines the Top
            /// </summary>
            public int Top;

            #endregion
        }
    }
}
