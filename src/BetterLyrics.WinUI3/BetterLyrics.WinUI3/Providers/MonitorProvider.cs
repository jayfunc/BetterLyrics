using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Providers;

public class MonitorProvider : IMonitorProvider
{
    public IEnumerable<string> GetAllMonitorDeviceNames()
    {
        var deviceNames = new List<string>();
        User32.EnumDisplayMonitors(IntPtr.Zero, null, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
        {
            User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
            if (User32.GetMonitorInfo(hMonitor, ref monitorInfoEx)) deviceNames.Add(monitorInfoEx.szDevice);
            return true; // 继续枚举
        }, IntPtr.Zero);
        return deviceNames;
    }

    public AppRect GetMonitorRectFromDeviceName(string deviceName)
    {
        AppRect result = AppRect.Empty;
        User32.EnumDisplayMonitors(IntPtr.Zero, null, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
        {
            User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
            if (User32.GetMonitorInfo(hMonitor, ref monitorInfoEx))
                if (string.Equals(monitorInfoEx.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    result = monitorInfoEx.rcMonitor.ToAppRect();
                    return false; // 找到后停止枚举
                }

            return true; // 继续枚举
        }, IntPtr.Zero);
        return result ?? GetPrimaryMonitorInfo().Item2;
    }

    public (string, AppRect) GetPrimaryMonitorInfo()
    {
        // (0,0) 总是在主屏
        var ptZero = new POINT(0, 0);
        var hMonitor = User32.MonitorFromPoint(ptZero, User32.MonitorFlags.MONITOR_DEFAULTTOPRIMARY);
        User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
        User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
        return (monitorInfoEx.szDevice, monitorInfoEx.rcMonitor.ToAppRect());
    }

    public string GetPrimaryMonitorDeviceName()
    {
        var (name, _) = GetPrimaryMonitorInfo();
        return name;
    }

    public (string, AppRect) GetMonitorInfoFromWindow(object? window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var hMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        User32.MONITORINFOEX monitorInfoEx = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
        User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
        return (monitorInfoEx.szDevice, monitorInfoEx.rcMonitor.ToAppRect());
    }
}