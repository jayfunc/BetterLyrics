using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
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
        private static readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        private static readonly HashSet<IntPtr> _registered = [];
        private static readonly Dictionary<IntPtr, RECT> _originalPositions = [];
        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyle = [];

        public static void Disable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (!_registered.Contains(hwnd)) return;

            window.SetIsShownInSwitchers(true);
            window.ExtendsContentIntoTitleBar = true;
            window.SetIsAlwaysOnTop(false);

            UnregisterAppBar(hwnd);

            window.SetWindowStyle(_originalWindowStyle[hwnd]);
            _originalWindowStyle.Remove(hwnd);

            window.ExtendsContentIntoTitleBar = true;

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

        public static void Enable(Window window, string monitorDeviceName, int appBarHeight, DockPlacement dockPlacement)
        {
            window.SetIsShownInSwitchers(false);
            window.SetIsAlwaysOnTop(true);

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (!_originalWindowStyle.ContainsKey(hwnd))
            {
                _originalWindowStyle[hwnd] = window.GetWindowStyle();
            }

            if (!_originalPositions.ContainsKey(hwnd))
            {
                if (User32.GetWindowRect(hwnd, out var rect))
                {
                    _originalPositions[hwnd] = rect;
                }
            }

            RegisterAppBar(hwnd, monitorDeviceName, appBarHeight, dockPlacement);

            var monitorInfo = MonitorHelper.GetMonitorInfoExFromDeviceName(_settingsService.AppSettings.DockMonitorDeviceName);

            int screenWidth = monitorInfo.rcMonitor.Width;
            int screenHeight = monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top;
            int y = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top : monitorInfo.rcMonitor.Bottom - appBarHeight;

            User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                monitorInfo.rcMonitor.Left,
                y,
                screenWidth,
                appBarHeight,
                User32.SetWindowPosFlags.SWP_HIDEWINDOW
            );
            window.ExtendsContentIntoTitleBar = false;
            window.ToggleWindowStyle(true, WindowStyle.Popup);
            window.Show();
        }

        private static void RegisterAppBar(IntPtr hwnd, string monitorDeviceName, int height, DockPlacement dockPlacement)
        {
            if (_registered.Contains(hwnd)) return;

            var uEdge = dockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

            var monitorInfo = MonitorHelper.GetMonitorInfoExFromDeviceName(monitorDeviceName);

            int top = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top : monitorInfo.rcMonitor.Bottom - height;
            int bottom = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top + height : monitorInfo.rcMonitor.Bottom;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = monitorInfo.rcMonitor.Left,
                    Top = top,
                    Right = monitorInfo.rcMonitor.Right,
                    Bottom = bottom,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_NEW, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
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

        public static void UpdateAppBarHeight(IntPtr hwnd, string monitorDeviceName, int newHeight, DockPlacement dockPlacement)
        {
            App.DispatcherQueueTimer?.Debounce(() =>
            {
                if (!_registered.Contains(hwnd))
                    return;

                var uEdge = dockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

                var monitorInfo = MonitorHelper.GetMonitorInfoExFromDeviceName(monitorDeviceName);

                int screenWidth = monitorInfo.rcMonitor.Width;
                int top = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top : monitorInfo.rcMonitor.Bottom - newHeight;
                int bottom = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top + newHeight : monitorInfo.rcMonitor.Bottom;

                Shell32.APPBARDATA abd = new()
                {
                    cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                    hWnd = hwnd,
                    uEdge = uEdge,
                    rc = new RECT
                    {
                        Left = monitorInfo.rcMonitor.Left,
                        Top = top,
                        Right = monitorInfo.rcMonitor.Right,
                        Bottom = bottom,
                    },
                };

                Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
                Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

                // 同步窗口实际高度和位置
                int y = dockPlacement == DockPlacement.Top ? monitorInfo.rcMonitor.Top : monitorInfo.rcMonitor.Bottom - newHeight;
                int repeatCount = 2;
                while (repeatCount > 0)
                {
                    repeatCount--;
                    User32.SetWindowPos(
                        hwnd,
                        IntPtr.Zero,
                        monitorInfo.rcMonitor.Left,
                        y,
                        screenWidth,
                        newHeight,
                        newHeight == 0 ? User32.SetWindowPosFlags.SWP_HIDEWINDOW : User32.SetWindowPosFlags.SWP_SHOWWINDOW
                    );
                }
            }, TimeSpan.FromMilliseconds(100));
        }
    }
}