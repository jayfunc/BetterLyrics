using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public sealed class MonitorBindingService : IMonitorBindingService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IMonitorProvider _monitorProvider;
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly ILogger<MonitorBindingService> _logger;
    private readonly Debouncer _monitorsChangedDebouncer = new();

    public MonitorBindingService(
        ISettingsService settingsService,
        IMonitorProvider monitorProvider,
        IAppUIThreadProvider appUIThreadProvider,
        ILogger<MonitorBindingService> logger)
    {
        _settingsService = settingsService;
        _monitorProvider = monitorProvider;
        _appUIThreadProvider = appUIThreadProvider;
        _logger = logger;

        // Migrate settings written by older versions (only a GDI name was persisted) and refresh
        // the runtime GDI names of bindings that already have a stable device path.
        LogMonitorTopology();
        NormalizeAll();

        _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;
    }

    private void LogMonitorTopology()
    {
        try
        {
            foreach (var monitor in _monitorProvider.GetMonitors())
            {
                _logger.LogInformation(
                    "Monitor detected: GDI={Gdi}, name={Name}, path={Path}, EDID={Edid}, active={Active}.",
                    monitor.GdiDeviceName, monitor.FriendlyName, monitor.DevicePath, monitor.EdidId,
                    monitor.IsActive);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to log the monitor topology.");
        }
    }

    public void NormalizeAll()
    {
        foreach (var status in _settingsService.AppSettings.WindowBoundsRecords)
        {
            Normalize(status);
        }
    }

    public bool Normalize(LyricsWindowStatus status)
    {
        // Only use the saved bounds as a fallback for bindings written before a stable
        // identity existed. Otherwise a temporarily disconnected monitor could be confused
        // with another monitor that happens to have the same bounds.
        var hasStableIdentity =
            !string.IsNullOrEmpty(status.MonitorDevicePath) ||
            !string.IsNullOrEmpty(status.MonitorEdid);

        var monitor = _monitorProvider.ResolveMonitor(
            status.MonitorDevicePath,
            status.MonitorEdid,
            status.MonitorDeviceName,
            hasStableIdentity ? null : status.MonitorBounds);

        if (monitor is null) return false;

        // Keep an existing stable identity when DisplayConfig is temporarily unavailable and
        // the fallback monitor carries no device path.
        if (!string.IsNullOrEmpty(monitor.DevicePath)) status.MonitorDevicePath = monitor.DevicePath;
        if (!string.IsNullOrEmpty(monitor.EdidId)) status.MonitorEdid = monitor.EdidId;

        status.MonitorDeviceName = monitor.GdiDeviceName;
        status.MonitorBounds = _monitorProvider.GetMonitorRectFromDeviceName(monitor.GdiDeviceName);
        return true;
    }

    private void MonitorProvider_MonitorsChanged(object? sender, EventArgs e)
    {
        // Give Windows a moment to settle the new topology before resolving the bindings again.
        _ = _monitorsChangedDebouncer.RunAsync(async token =>
        {
            await Task.Delay(500, token);

            try
            {
                _appUIThreadProvider.Execute(NormalizeAll);
            }
            catch (InvalidOperationException)
            {
                // The dispatcher is not ready yet (very early startup); the next topology change
                // or the next window open resolves the binding again.
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to normalize monitor bindings after a display change.");
            }
        });
    }

    public void Dispose()
    {
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;
        _monitorsChangedDebouncer.Dispose();
    }
}
