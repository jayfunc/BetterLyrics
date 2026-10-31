using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.LyricsWindow;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class ScreenKeeperService : IScreenKeeperService,
    IRecipient<LyricsWindowKeepScreenOpenChangedMessage>,
    IRecipient<LyricsWindowStateChangedMessage>,
    IRecipient<LyricsWindowDisplayStateChangedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly IMonitorProvider _monitorProvider;
    private readonly ILogger<ScreenKeeperService> _logger;

    public ScreenKeeperService(
        ISettingsService settingsService,
        IMonitorProvider monitorProvider,
        ILogger<ScreenKeeperService> logger)
    {
        _settingsService = settingsService;
        _monitorProvider = monitorProvider;
        _logger = logger;

        WeakReferenceMessenger.Default.RegisterAll(this);
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        // 检测已打开且实际可见的窗口中是否存在配置为不休眠的窗口。
        // 最小化到任务栏的窗口仍是 Opened 状态，需要额外排除。
        var isKeepScreenOpen = _settingsService.AppSettings.WindowBoundsRecords
            .Where(x => x.WindowState == LyricsWindowState.Opened)
            .Where(x => x.IsKeepScreenOpen)
            .Any(x => !x.IsMinimized);

        _monitorProvider.SetKeepScreenOn(isKeepScreenOpen);

        _logger.LogDebug("Keep-screen state refreshed. KeepScreenOn: {KeepScreenOn}.", isKeepScreenOpen);
    }

    public void Receive(LyricsWindowKeepScreenOpenChangedMessage message) => RefreshStatus();
    public void Receive(LyricsWindowStateChangedMessage message) => RefreshStatus();
    public void Receive(LyricsWindowDisplayStateChangedMessage message) => RefreshStatus();
}
