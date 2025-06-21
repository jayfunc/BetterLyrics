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
    public static class DockHelper
    {
        private static readonly HashSet<IntPtr> _registered = [];

        private static readonly Dictionary<IntPtr, RECT> _originalPositions = [];

        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyle = [];

        public static void Disable(Window window)
        {
            window.SetIsShownInSwitchers(true);
            window.ExtendsContentIntoTitleBar = true;
            window.SetIsAlwaysOnTop(false);

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            window.SetWindowStyle(_originalWindowStyle[hwnd]);
            _originalWindowStyle.Remove(hwnd);

            if (_originalPositions.TryGetValue(hwnd, out var rect))
            {
                SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top,
                    SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW
                );
                _originalPositions.Remove(hwnd);
            }

            UnregisterAppBar(hwnd);
        }

        public static void Enable(Window window, int appBarHeight)
        {
            window.SetIsShownInSwitchers(false);
            window.ExtendsContentIntoTitleBar = false;
            window.SetIsAlwaysOnTop(true);

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (!_originalWindowStyle.ContainsKey(hwnd))
            {
                _originalWindowStyle[hwnd] = window.GetWindowStyle();
            }
            window.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);

            if (!_originalPositions.ContainsKey(hwnd))
            {
                if (GetWindowRect(hwnd, out var rect))
                {
                    _originalPositions[hwnd] = rect;
                }
            }

            RegisterAppBar(hwnd, appBarHeight);

            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                screenWidth,
                appBarHeight,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW
            );
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        #region AppBar registration
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

        #region Win32 Helper and Constants

        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOOWNERZORDER = 0x0200;
        private const int SWP_SHOWWINDOW = 0x0040;

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 0;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

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

        /// <summary>
        /// 更改已注册 AppBar 的高度。
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="newHeight">新的高度</param>
        public static void UpdateAppBarHeight(IntPtr hwnd, int newHeight)
        {
            if (!_registered.Contains(hwnd))
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
                    bottom = newHeight,
                },
            };

            SHAppBarMessage(ABM_SETPOS, ref abd);

            // 同步窗口实际高度
            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                GetSystemMetrics(SM_CXSCREEN),
                newHeight,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW
            );
        }
        #endregion
    }
}
