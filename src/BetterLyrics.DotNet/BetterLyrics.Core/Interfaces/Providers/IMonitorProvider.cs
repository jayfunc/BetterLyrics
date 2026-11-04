using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IMonitorProvider
    {
        bool IsDisplayOn { get; }
        event EventHandler<bool>? DisplayStatusChanged;
        event EventHandler? MonitorsChanged;
        IEnumerable<string> GetAllMonitorDeviceNames();

        /// <summary>
        /// Returns the active monitors together with their stable device paths and EDID identity.
        /// </summary>
        IReadOnlyList<MonitorInfo> GetMonitors();

        /// <summary>
        /// Invalidates the cached topology and returns the monitors again.
        /// </summary>
        IReadOnlyList<MonitorInfo> RefreshMonitors();

        /// <summary>
        /// Resolves a stored monitor binding (device path, EDID identity, legacy GDI name or bounds)
        /// to a monitor in the current topology.
        /// </summary>
        MonitorInfo? ResolveMonitor(string? devicePath, string? edidId, string? legacyDeviceName,
            AppRect? savedBounds = null);

        /// <summary>
        /// Returns whether a stored monitor binding is currently available. The primary monitor is
        /// always considered connected so that a window bound to it is never hidden by mistake.
        /// </summary>
        bool IsMonitorConnected(string? devicePath, string? edidId, string? legacyDeviceName,
            AppRect? savedBounds = null);

        AppRect GetMonitorRectFromDeviceName(string deviceName);
        (string, AppRect) GetPrimaryMonitorInfo();
        string GetPrimaryMonitorDeviceName();
        (string, AppRect) GetMonitorInfoFromWindow(object? window);
        void SetKeepScreenOn(bool keepOn);
    }
}
