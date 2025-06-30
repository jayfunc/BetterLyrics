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
using static BetterLyrics.WinUI3.Helper.Win32Helper;

namespace BetterLyrics.WinUI3.Helper
{
    public static class DockModeHelper
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
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top,
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

        private static void RegisterAppBar(IntPtr hwnd, int height)
        {
            if (_registered.Contains(hwnd)) return;

            APPBARDATA abd = new()
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                uEdge = ABE_TOP,
                rc = new RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = GetSystemMetrics(SM_CXSCREEN),
                    Bottom = height,
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
                    Left = 0,
                    Top = 0,
                    Right = GetSystemMetrics(SM_CXSCREEN),
                    Bottom = newHeight,
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
    }
}