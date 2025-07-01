using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;

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
                User32.SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top,
                    User32.SetWindowPosFlags.SWP_SHOWWINDOW
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
                if (User32.GetWindowRect(hwnd, out var rect))
                {
                    _originalPositions[hwnd] = rect;
                }
            }

            RegisterAppBar(hwnd, appBarHeight);

            int screenWidth = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN);
            int screenHeight = User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN);
            User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                screenWidth,
                appBarHeight,
                User32.SetWindowPosFlags.SWP_SHOWWINDOW
            );
        }

        private static void RegisterAppBar(IntPtr hwnd, int height)
        {
            if (_registered.Contains(hwnd)) return;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = Shell32.ABE.ABE_TOP,
                rc = new RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                    Bottom = height,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_NEW, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

            _registered.Add(hwnd);
        }

        private static void UnregisterAppBar(IntPtr hwnd)
        {
            if (!_registered.Contains(hwnd))
                return;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_REMOVE, ref abd);
            _registered.Remove(hwnd);
        }

        public static void UpdateAppBarHeight(IntPtr hwnd, int newHeight)
        {
            if (!_registered.Contains(hwnd))
                return;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = Shell32.ABE.ABE_TOP,
                rc = new RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                    Bottom = newHeight,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

            // 同步窗口实际高度
            User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                newHeight,
                User32.SetWindowPosFlags.SWP_SHOWWINDOW
            );
        }
    }
}