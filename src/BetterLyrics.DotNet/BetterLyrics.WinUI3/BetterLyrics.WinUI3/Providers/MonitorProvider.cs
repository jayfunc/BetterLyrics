using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.System.Display;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public partial class MonitorProvider : IMonitorProvider, IDisposable
{
    private readonly MonitorHook _monitorHook;

    private readonly DisplayRequest _displayRequest = new();
    private bool _isKeepScreenOn;
    private readonly object _lock = new();

    public bool IsDisplayOn { get; private set; } = true;

    public event EventHandler<bool>? DisplayStatusChanged;
    public event EventHandler? MonitorsChanged;

    public MonitorProvider()
    {
        _monitorHook = new MonitorHook();
        _monitorHook.DisplayChanged += () => MonitorsChanged?.Invoke(this, EventArgs.Empty);
        _monitorHook.DisplayPowerStatusChanged += (isOn) =>
        {
            IsDisplayOn = isOn;
            DisplayStatusChanged?.Invoke(this, isOn);
        };
    }

    public IEnumerable<string> GetAllMonitorDeviceNames()
    {
        var deviceNames = new List<string>();
        uint devNum = 0;
        Gdi32.DISPLAY_DEVICE d = new Gdi32.DISPLAY_DEVICE();
        d.cb = (uint)Marshal.SizeOf(d);

        while (User32.EnumDisplayDevices(null, devNum, ref d, 0))
        {
            Gdi32.DISPLAY_DEVICE mon = new Gdi32.DISPLAY_DEVICE();
            mon.cb = (uint)Marshal.SizeOf(mon);

            // Check if there is at least one physical monitor attached to this adapter
            // This filters out ghost adapters, mirroring drivers, and unused virtual ports
            if (User32.EnumDisplayDevices(d.DeviceName, 0, ref mon, 0))
            {
                deviceNames.Add(d.DeviceName);
            }

            devNum++;
            d.cb = (uint)Marshal.SizeOf(d);
        }

        return deviceNames.Distinct();
    }

    public AppRect GetMonitorRectFromDeviceName(string deviceName)
    {
        DEVMODE devMode = new DEVMODE();
        devMode.dmSize = (ushort)Marshal.SizeOf(devMode);

        // Try getting current settings first
        if (User32.EnumDisplaySettings(deviceName, User32.ENUM_CURRENT_SETTINGS, ref devMode))
        {
            if (devMode.dmPelsWidth > 0 && devMode.dmPelsHeight > 0)
            {
                return new AppRect(devMode.dmPosition.X, devMode.dmPosition.Y, devMode.dmPelsWidth, devMode.dmPelsHeight);
            }
        }

        // If inactive/disabled, get registry settings (last known good)
        if (User32.EnumDisplaySettings(deviceName, User32.ENUM_REGISTRY_SETTINGS, ref devMode))
        {
            if (devMode.dmPelsWidth > 0 && devMode.dmPelsHeight > 0)
            {
                return new AppRect(devMode.dmPosition.X, devMode.dmPosition.Y, devMode.dmPelsWidth, devMode.dmPelsHeight);
            }
        }

        // Fallback if totally unavailable (avoid recursive call to GetPrimaryMonitorInfo)
        return new AppRect(0, 0, 1920, 1080);
    }

    public (string, AppRect) GetPrimaryMonitorInfo()
    {
        uint devNum = 0;
        Gdi32.DISPLAY_DEVICE d = new Gdi32.DISPLAY_DEVICE();
        d.cb = (uint)Marshal.SizeOf(d);

        while (User32.EnumDisplayDevices(null, devNum, ref d, 0))
        {
            if ((d.StateFlags & Gdi32.DISPLAY_DEVICE_FLAGS.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0)
            {
                var rect = GetMonitorRectFromDeviceName(d.DeviceName);
                return (d.DeviceName, rect);
            }
            devNum++;
            d.cb = (uint)Marshal.SizeOf(d);
        }

        return ("\\\\.\\DISPLAY1", new AppRect(0, 0, 1920, 1080));
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

        if (User32.GetMonitorInfo(hMonitor, ref monitorInfoEx))
        {
            return (monitorInfoEx.szDevice, monitorInfoEx.rcMonitor.ToAppRect());
        }

        return GetPrimaryMonitorInfo();
    }

    public void SetKeepScreenOn(bool keepOn)
    {
        lock (_lock)
        {
            if (keepOn && !_isKeepScreenOn)
            {
                _displayRequest.RequestActive();
                _isKeepScreenOn = true;
            }
            else if (!keepOn && _isKeepScreenOn)
            {
                _displayRequest.RequestRelease();
                _isKeepScreenOn = false;
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isKeepScreenOn)
            {
                _displayRequest.RequestRelease();
                _isKeepScreenOn = false;
            }
        }
        _monitorHook.Dispose();
    }
}