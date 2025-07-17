using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.WinUI;
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

            UnregisterAppBar(hwnd);
            RefreshWorkArea();

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
        }

        public static void Enable(Window window, int appBarHeight, DockPlacement dockPlacement)
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

            RegisterAppBar(hwnd, appBarHeight, dockPlacement);

            int screenWidth = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN);
            int screenHeight = User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN);
            int y = dockPlacement == DockPlacement.Top ? 0 : screenHeight - appBarHeight;
            User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                y,
                screenWidth,
                appBarHeight,
                User32.SetWindowPosFlags.SWP_SHOWWINDOW
            );

            RefreshWorkArea();
        }

        private static void RegisterAppBar(IntPtr hwnd, int height, DockPlacement dockPlacement)
        {
            if (_registered.Contains(hwnd)) return;

            var uEdge = dockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;
            int screenHeight = User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN);
            int top = dockPlacement == DockPlacement.Top ? 0 : screenHeight - height;
            int bottom = dockPlacement == DockPlacement.Top ? height : screenHeight;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = 0,
                    Top = top,
                    Right = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                    Bottom = bottom,
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

        private static void RefreshWorkArea()
        {
            User32.SendMessage(HWND.HWND_BROADCAST, User32.WindowMessage.WM_SETTINGCHANGE, IntPtr.Zero, IntPtr.Zero);
        }

        public static void UpdateAppBarHeight(IntPtr hwnd, int newHeight, DockPlacement dockPlacement)
        {
            App.DispatcherQueueTimer?.Debounce(() =>
            {
                if (!_registered.Contains(hwnd))
                    return;

                var uEdge = dockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;
                int screenHeight = User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN);
                int top = dockPlacement == DockPlacement.Top ? 0 : screenHeight - newHeight;
                int bottom = dockPlacement == DockPlacement.Top ? newHeight : screenHeight;

                Shell32.APPBARDATA abd = new()
                {
                    cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                    hWnd = hwnd,
                    uEdge = uEdge,
                    rc = new RECT
                    {
                        Left = 0,
                        Top = top,
                        Right = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                        Bottom = bottom,
                    },
                };

                Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

                // 同步窗口实际高度和位置
                int y = dockPlacement == DockPlacement.Top ? 0 : screenHeight - newHeight;
                User32.SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    0,
                    y,
                    User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN),
                    newHeight,
                    User32.SetWindowPosFlags.SWP_SHOWWINDOW
                );

                RefreshWorkArea();
            }, TimeSpan.FromMilliseconds(100));
        }
    }
}