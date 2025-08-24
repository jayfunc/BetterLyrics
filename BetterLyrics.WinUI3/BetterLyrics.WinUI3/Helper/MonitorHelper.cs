using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Helper
{
    public static class MonitorHelper
    {
        public static IEnumerable<string> GetAllMonitorDeviceNames()
        {
            var deviceNames = new List<string>();
            User32.EnumDisplayMonitors(IntPtr.Zero, null, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
                if (User32.GetMonitorInfo(hMonitor, ref monitorInfoEx))
                {
                    deviceNames.Add(monitorInfoEx.szDevice);
                }
                return true; // 继续枚举
            }, IntPtr.Zero);
            return deviceNames;
        }

        public static User32.MONITORINFOEX GetMonitorInfoExFromDeviceName(string deviceName)
        {
            User32.MONITORINFOEX? result = null;
            User32.EnumDisplayMonitors(IntPtr.Zero, null, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
                if (User32.GetMonitorInfo(hMonitor, ref monitorInfoEx))
                {
                    if (string.Equals(monitorInfoEx.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        result = monitorInfoEx;
                        return false; // 找到后停止枚举
                    }
                }
                return true; // 继续枚举
            }, IntPtr.Zero);
            return result ?? GetPrimaryMonitorInfoEx();
        }

        public static User32.MONITORINFOEX GetPrimaryMonitorInfoEx()
        {
            // (0,0) 总是在主屏
            var ptZero = new POINT(0, 0);
            HMONITOR hMonitor = User32.MonitorFromPoint(ptZero, User32.MonitorFlags.MONITOR_DEFAULTTOPRIMARY);
            User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
            User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
            return monitorInfoEx;
        }

        public static string GetPrimaryMonitorDeviceName()
        {
            var primaryMonitorInfo = GetPrimaryMonitorInfoEx();
            return primaryMonitorInfo.szDevice;
        }

        public static User32.MONITORINFOEX GetMonitorInfoExFromWindow(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var hMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
            User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
            return monitorInfoEx;
        }
    }
}
