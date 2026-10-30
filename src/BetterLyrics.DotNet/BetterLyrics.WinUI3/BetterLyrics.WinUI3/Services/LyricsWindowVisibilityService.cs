using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Models.WallpaperEngine;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Services;

/// <summary>
/// Singleton Service
/// </summary>
public sealed class LyricsWindowVisibilityService : ILyricsWindowVisibilityService,
    IRecipient<CurrentIsPlayingChangedMessage>,
    IRecipient<CurrentMediaSourceProviderChangedMessage>,
    IRecipient<LyricsWindowAutoHideConfigChangedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IGsmtcService _gsmtcService;
    private readonly IMonitorProvider _monitorProvider;
    private readonly IWallpaperEngineService _wallpaperEngineService;
    private readonly ILogger<LyricsWindowVisibilityService> _logger;

    private readonly Dictionary<LyricsWindowStatus, Debouncer> _debouncers =
        new(ReferenceEqualityComparer.Instance);

    private volatile bool _initialized;

    public LyricsWindowVisibilityService(
        ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider,
        IAppUIThreadProvider appUIThreadProvider,
        IGsmtcService gsmtcService,
        IMonitorProvider monitorProvider,
        IWallpaperEngineService wallpaperEngineService,
        ILogger<LyricsWindowVisibilityService> logger)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _appUIThreadProvider = appUIThreadProvider;
        _gsmtcService = gsmtcService;
        _monitorProvider = monitorProvider;
        _wallpaperEngineService = wallpaperEngineService;
        _logger = logger;

        WeakReferenceMessenger.Default.RegisterAll(this);

        _monitorProvider.MonitorsChanged += OnMonitorsChanged;
        _wallpaperEngineService.ActiveWallpaperChanged += OnActiveWallpaperChanged;
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _appUIThreadProvider.Execute(() => ReevaluateAll(allowCreate: true));
    }

    public void NotifyBindingChanged(LyricsWindowStatus status)
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() => Reevaluate(status, allowCreate: true));
    }

    public void NotifyWallpaperChanged()
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() => ReevaluateAll(allowCreate: true));
    }

    public void NotifyWindowLoaded(LyricsWindowStatus status)
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() => Reevaluate(status, allowCreate: false));
    }

    public void Receive(CurrentIsPlayingChangedMessage message)
    {
        ScheduleAll(status => status.AutoShowOrHideWindowDelay, allowCreate: false);
    }

    public void Receive(CurrentMediaSourceProviderChangedMessage message)
    {
        ScheduleAll(status => status.AutoShowOrHideWindowDelay, allowCreate: false);
    }

    public void Receive(LyricsWindowAutoHideConfigChangedMessage message)
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() =>
        {
            if (!IsManaged(message.Sender)) return;
            Schedule(message.Sender, 0, allowCreate: false);
        });
    }

    private void OnActiveWallpaperChanged(object? sender, WallpaperEngineActiveWallpaperChangedEventArgs e)
    {
        NotifyWallpaperChanged();
    }

    private void OnMonitorsChanged(object? sender, EventArgs e)
    {
        if (!_initialized) return;

        // Give Windows a moment to settle the new display topology before
        // deciding whether a target monitor is available again.
        ScheduleAll(500, allowCreate: true);
    }

    private void ScheduleAll(int delayMilliseconds, bool allowCreate)
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() =>
        {
            foreach (var status in _settingsService.AppSettings.WindowBoundsRecords.ToList())
            {
                Schedule(status, delayMilliseconds, allowCreate);
            }
        });
    }

    private void ScheduleAll(Func<LyricsWindowStatus, int> delaySelector, bool allowCreate)
    {
        if (!_initialized) return;

        _appUIThreadProvider.Execute(() =>
        {
            foreach (var status in _settingsService.AppSettings.WindowBoundsRecords.ToList())
            {
                Schedule(status, delaySelector(status), allowCreate);
            }
        });
    }

    private void Schedule(LyricsWindowStatus status, int delayMilliseconds, bool allowCreate)
    {
        if (!IsManaged(status)) return;

        if (!_debouncers.TryGetValue(status, out var debouncer))
        {
            debouncer = new Debouncer();
            _debouncers[status] = debouncer;
        }

        _ = debouncer.RunAsync(
            () => _appUIThreadProvider.Execute(() => Reevaluate(status, allowCreate)),
            delayMilliseconds);
    }

    private void ReevaluateAll(bool allowCreate)
    {
        foreach (var status in _settingsService.AppSettings.WindowBoundsRecords.ToList())
        {
            Reevaluate(status, allowCreate);
        }
    }

    private void Reevaluate(LyricsWindowStatus status, bool allowCreate)
    {
        if (!IsManaged(status)) return;

        var window = _windowManagerProvider.GetNowPlayingWindow(status) as NowPlayingWindow;

        if (window != null && _windowManagerProvider.IsWindowMinimized(window))
        {
            _logger.LogDebug("Skipped lyrics window \"{Name}\" because the user minimized it.", status.Name);
            return;
        }

        var (reasons, canCreate) = Evaluate(status);

        if (LyricsWindowVisibilityPolicy.ShouldShow(reasons))
        {
            status.HideReasons = LyricsWindowHideReason.None;

            if (window == null)
            {
                if (!allowCreate || !canCreate) return;

                var createdWindow = _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
                if (!LyricsWindowVisibilityPolicy.ShouldShow(reasons))
                {
                    createdWindow.HideFromVisibilityPolicy(reasons);
                }

                _logger.LogDebug("Created lyrics window \"{Name}\" for the active wallpaper ({Reasons}).",
                    status.Name, reasons);
            }
            else
            {
                window.ShowFromVisibilityPolicy();
            }
        }
        else
        {
            if (window == null) return;

            status.HideReasons = reasons;
            window.HideFromVisibilityPolicy(reasons);

            _logger.LogDebug("Hid lyrics window \"{Name}\" ({Reasons}).", status.Name, reasons);
        }
    }

    private (LyricsWindowHideReason Reasons, bool CanCreate) Evaluate(LyricsWindowStatus status)
    {
        var binding = status.WallpaperEngineBindingPath;
        var isBound = !string.IsNullOrWhiteSpace(binding);
        var activeProjectPaths = _wallpaperEngineService.GetActiveWallpaperProjectPaths();

        // When Wallpaper Engine is not available or its state is unknown,
        // do not hide an existing bound window and do not create a new one.
        var isWallpaperMatched = !isBound || activeProjectPaths == null ||
                                 activeProjectPaths.Contains(binding.Replace('/', '\\'));

        var isUserHidden = status.IsUserHidden;
        var isTargetMonitorConnected = IsTargetMonitorConnected(status);

        // Creation is only driven by wallpaper/startup/binding/monitor events,
        // not by playback events, so a manually selected window is not recreated
        // unexpectedly. Media events only show or hide windows that already exist.
        var canCreate = isBound && activeProjectPaths != null && isWallpaperMatched &&
                        !isUserHidden && isTargetMonitorConnected;

        var reasons = LyricsWindowVisibilityPolicy.GetHideReasons(
            isWallpaperBound: isBound,
            isWallpaperMatched: isWallpaperMatched,
            hideWindowWhenPaused: status.HideWindowWhenPaused,
            isPlaying: _gsmtcService.CurrentIsPlaying,
            hideWindowWhenNullSession: status.HideWindowWhenNullSession,
            hasMediaSession: _gsmtcService.CurrentMediaSourceProviderInfo != null,
            isTargetMonitorConnected: isTargetMonitorConnected,
            isUserHidden: isUserHidden);

        return (reasons, canCreate);
    }

    private bool IsTargetMonitorConnected(LyricsWindowStatus status)
    {
        var targetMonitor = status.MonitorDeviceName;
        if (string.IsNullOrEmpty(targetMonitor)) return true;

        if (string.Equals(targetMonitor, _monitorProvider.GetPrimaryMonitorDeviceName(),
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _monitorProvider.GetAllMonitorDeviceNames()
            .Contains(targetMonitor, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsManaged(LyricsWindowStatus status) =>
        _settingsService.AppSettings.WindowBoundsRecords.Contains(status);
}
