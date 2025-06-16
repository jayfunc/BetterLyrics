using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class AppBarHelper
    {
        // 记录哪些 HWND 已经注册 AppBar
        private static readonly HashSet<IntPtr> _registered = [];

        /// <summary>
        /// Enable AppBar function
        /// </summary>
        /// <param name="window">Target window</param>
        /// <param name="appBarHeight">App bar height</param>
        /// <param name="clickThrough">Is click-through enabled</param>
        public static void Enable(Window window, int appBarHeight)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            RegisterAppBar(hwnd, appBarHeight);

            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            SetWindowPos(
                hwnd,
                HWND_TOPMOST,
                0,
                0,
                screenWidth,
                appBarHeight,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW
            );
        }

        /// <summary>
        /// 关闭并注销 AppBar，占位恢复。
        /// </summary>
        public static void Disable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            UnregisterAppBar(hwnd);

            // 移除 WS_EX_TRANSPARENT（可根据需求恢复其他样式）
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        #region AppBar 注册逻辑
        private const uint ABM_NEW = 0x00000000;
        private const uint ABM_REMOVE = 0x00000001;
        private const uint ABM_SETPOS = 0x00000003;
        private const int ABE_TOP = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left,
                top,
                right,
                bottom;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        private static void RegisterAppBar(IntPtr hwnd, int height)
        {
            if (_registered.Contains(hwnd))
                return;

            APPBARDATA abd = new()
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                uEdge = ABE_TOP,
                rc = new RECT
                {
                    left = 0,
                    top = 0,
                    right = GetSystemMetrics(SM_CXSCREEN),
                    bottom = height,
                },
            };

            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            _registered.Add(hwnd);
        }

        private static void UnregisterAppBar(IntPtr hwnd)
        {
            if (!_registered.Contains(hwnd))
                return;

            APPBARDATA abd = new() { cbSize = Marshal.SizeOf<APPBARDATA>(), hWnd = hwnd };

            SHAppBarMessage(ABM_REMOVE, ref abd);
            _registered.Remove(hwnd);
        }
        #endregion

        #region Win32 Helper & 常量

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;

        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOOWNERZORDER = 0x0200;
        private const int SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new(-1);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 0;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags
        );
        #endregion
    }
}
