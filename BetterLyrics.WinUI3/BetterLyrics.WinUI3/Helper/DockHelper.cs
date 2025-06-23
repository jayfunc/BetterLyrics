// 2025/6/23 by Zhe Fang

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="DockHelper" />
    /// </summary>
    public static class DockHelper
    {
        #region Constants

        /// <summary>
        /// Defines the ABE_TOP
        /// </summary>
        private const int ABE_TOP = 1;

        /// <summary>
        /// Defines the ABM_NEW
        /// </summary>
        private const uint ABM_NEW = 0x00000000;

        /// <summary>
        /// Defines the ABM_REMOVE
        /// </summary>
        private const uint ABM_REMOVE = 0x00000001;

        /// <summary>
        /// Defines the ABM_SETPOS
        /// </summary>
        private const uint ABM_SETPOS = 0x00000003;

        /// <summary>
        /// Defines the SM_CXSCREEN
        /// </summary>
        private const int SM_CXSCREEN = 0;

        /// <summary>
        /// Defines the SM_CYSCREEN
        /// </summary>
        private const int SM_CYSCREEN = 0;

        /// <summary>
        /// Defines the SWP_NOACTIVATE
        /// </summary>
        private const int SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// Defines the SWP_NOOWNERZORDER
        /// </summary>
        private const int SWP_NOOWNERZORDER = 0x0200;

        /// <summary>
        /// Defines the SWP_SHOWWINDOW
        /// </summary>
        private const int SWP_SHOWWINDOW = 0x0040;

        #endregion

        #region Fields

        /// <summary>
        /// Defines the _originalPositions
        /// </summary>
        private static readonly Dictionary<IntPtr, RECT> _originalPositions = [];

        /// <summary>
        /// Defines the _originalWindowStyle
        /// </summary>
        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyle = [];

        /// <summary>
        /// Defines the _registered
        /// </summary>
        private static readonly HashSet<IntPtr> _registered = [];

        #endregion

        #region Methods

        /// <summary>
        /// The Disable
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
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

        /// <summary>
        /// The Enable
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        /// <param name="appBarHeight">The appBarHeight<see cref="int"/></param>
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

        /// <summary>
        /// 更改已注册 AppBar 的高度。
        /// </summary>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
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

        /// <summary>
        /// The GetSystemMetrics
        /// </summary>
        /// <param name="nIndex">The nIndex<see cref="int"/></param>
        /// <returns>The <see cref="int"/></returns>
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// The GetWindowRect
        /// </summary>
        /// <param name="hWnd">The hWnd<see cref="IntPtr"/></param>
        /// <param name="lpRect">The lpRect<see cref="RECT"/></param>
        /// <returns>The <see cref="bool"/></returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// The RegisterAppBar
        /// </summary>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
        /// <param name="height">The height<see cref="int"/></param>
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

        /// <summary>
        /// The SetWindowPos
        /// </summary>
        /// <param name="hWnd">The hWnd<see cref="IntPtr"/></param>
        /// <param name="hWndInsertAfter">The hWndInsertAfter<see cref="IntPtr"/></param>
        /// <param name="X">The X<see cref="int"/></param>
        /// <param name="Y">The Y<see cref="int"/></param>
        /// <param name="cx">The cx<see cref="int"/></param>
        /// <param name="cy">The cy<see cref="int"/></param>
        /// <param name="uFlags">The uFlags<see cref="uint"/></param>
        /// <returns>The <see cref="bool"/></returns>
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
        /// The SHAppBarMessage
        /// </summary>
        /// <param name="dwMessage">The dwMessage<see cref="uint"/></param>
        /// <param name="pData">The pData<see cref="APPBARDATA"/></param>
        /// <returns>The <see cref="uint"/></returns>
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        /// <summary>
        /// The UnregisterAppBar
        /// </summary>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
        private static void UnregisterAppBar(IntPtr hwnd)
        {
            if (!_registered.Contains(hwnd))
                return;

            APPBARDATA abd = new() { cbSize = Marshal.SizeOf<APPBARDATA>(), hWnd = hwnd };

            SHAppBarMessage(ABM_REMOVE, ref abd);
            _registered.Remove(hwnd);
        }

        #endregion

        /// <summary>
        /// Defines the <see cref="APPBARDATA" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            #region Fields

            /// <summary>
            /// Defines the cbSize
            /// </summary>
            public int cbSize;

            /// <summary>
            /// Defines the hWnd
            /// </summary>
            public IntPtr hWnd;

            /// <summary>
            /// Defines the lParam
            /// </summary>
            public int lParam;

            /// <summary>
            /// Defines the rc
            /// </summary>
            public RECT rc;

            /// <summary>
            /// Defines the uCallbackMessage
            /// </summary>
            public uint uCallbackMessage;

            /// <summary>
            /// Defines the uEdge
            /// </summary>
            public uint uEdge;

            #endregion
        }

        /// <summary>
        /// Defines the <see cref="RECT" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            #region Fields

            /// <summary>
            /// Defines the left, top, right, bottom
            /// </summary>
            public int left,
                top,
                right,
                bottom;

            #endregion
        }
    }
}
