using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private readonly object _monitorsLock = new();
    private IReadOnlyList<MonitorInfo>? _monitors;

    public bool IsDisplayOn { get; private set; } = true;

    public event EventHandler<bool>? DisplayStatusChanged;
    public event EventHandler? MonitorsChanged;

    public MonitorProvider()
    {
        _monitorHook = new MonitorHook();
        _monitorHook.DisplayChanged += () =>
        {
            InvalidateMonitors();
            MonitorsChanged?.Invoke(this, EventArgs.Empty);
        };
        _monitorHook.DisplayPowerStatusChanged += (isOn) =>
        {
            IsDisplayOn = isOn;
            DisplayStatusChanged?.Invoke(this, isOn);
        };
    }

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        lock (_monitorsLock)
        {
            return _monitors ??= BuildMonitors();
        }
    }

    public IReadOnlyList<MonitorInfo> RefreshMonitors()
    {
        InvalidateMonitors();
        return GetMonitors();
    }

    private IReadOnlyList<MonitorInfo> BuildMonitors()
    {
        var monitors = DisplayConfigMonitorHook.GetMonitors(activeOnly: true);
        if (monitors.Count > 0) return monitors;

        // DisplayConfig is unavailable (for example in a remote session); fall back to the
        // legacy GDI enumeration. The resulting entries have no stable identity, but the
        // rest of the app keeps working.
        Debug.WriteLine("DisplayConfig returned no monitors; falling back to EnumDisplayDevices.");

        return EnumerateGdiDeviceNames()
            .Select(name => new MonitorInfo(name, string.Empty, string.Empty, string.Empty, true))
            .ToList();
    }

    private void InvalidateMonitors()
    {
        lock (_monitorsLock)
        {
            _monitors = null;
        }
    }

    public IEnumerable<string> GetAllMonitorDeviceNames() => EnumerateGdiDeviceNames();

    private static IEnumerable<string> EnumerateGdiDeviceNames()
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

    public MonitorInfo? ResolveMonitor(string? devicePath, string? edidId, string? legacyDeviceName,
        AppRect? savedBounds = null)
    {
        var monitors = GetMonitors();
        if (monitors.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(devicePath))
        {
            var byPath = monitors.FirstOrDefault(m =>
                string.Equals(m.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase));
            if (byPath is not null) return byPath;
        }

        if (!string.IsNullOrWhiteSpace(edidId))
        {
            var byEdid = monitors.FirstOrDefault(m =>
                string.Equals(m.EdidId, edidId, StringComparison.OrdinalIgnoreCase));
            if (byEdid is not null) return byEdid;
        }

        if (!string.IsNullOrWhiteSpace(legacyDeviceName))
        {
            var byName = monitors.FirstOrDefault(m =>
                string.Equals(m.GdiDeviceName, legacyDeviceName, StringComparison.OrdinalIgnoreCase));
            if (byName is not null) return byName;
        }

        // Last resort for settings written before device paths were persisted: match the
        // monitor by the bounds that were saved together with the binding.
        if (savedBounds is { IsEmpty: false } bounds)
        {
            var byBounds = monitors.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.GdiDeviceName) &&
                IsNearlyEqual(GetMonitorRectFromDeviceName(m.GdiDeviceName), bounds, 2));
            if (byBounds is not null) return byBounds;
        }

        return null;
    }

    public bool IsMonitorConnected(string? devicePath, string? edidId, string? legacyDeviceName,
        AppRect? savedBounds = null)
    {
        // Keep the existing primary-monitor escape hatch: a window bound to the primary display
        // must never be treated as disconnected.
        if (!string.IsNullOrWhiteSpace(legacyDeviceName) &&
            string.Equals(legacyDeviceName, GetPrimaryMonitorDeviceName(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ResolveMonitor(devicePath, edidId, legacyDeviceName, savedBounds) is not null;
    }

    private static bool IsNearlyEqual(AppRect a, AppRect b, double tolerance)
    {
        return Math.Abs(a.Left - b.Left) <= tolerance &&
               Math.Abs(a.Top - b.Top) <= tolerance &&
               Math.Abs(a.Right - b.Right) <= tolerance &&
               Math.Abs(a.Bottom - b.Bottom) <= tolerance;
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
