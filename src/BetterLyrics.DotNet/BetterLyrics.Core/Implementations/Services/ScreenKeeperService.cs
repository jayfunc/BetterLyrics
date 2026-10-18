using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.LyricsWindow;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class ScreenKeeperService : IScreenKeeperService,
    IRecipient<LyricsWindowKeepScreenOpenChangedMessage>,
    IRecipient<LyricsWindowStatusStateChangedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly IMonitorProvider _monitorProvider;

    public ScreenKeeperService(ISettingsService settingsService, IMonitorProvider monitorProvider)
    {
        _settingsService = settingsService;
        _monitorProvider = monitorProvider;

        WeakReferenceMessenger.Default.RegisterAll(this);
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        // 检测已打开的窗口中是否存在配置为不休眠的窗口
        var isKeepScreenOpen = _settingsService.AppSettings.WindowBoundsRecords
            .Where(x => x.WindowStatus == WindowStatus.Opened)
            .Any(x => x.IsKeepScreenOpen);

        _monitorProvider.SetKeepScreenOn(isKeepScreenOpen);
    }

    public void Receive(LyricsWindowKeepScreenOpenChangedMessage message) => RefreshStatus();
    public void Receive(LyricsWindowStatusStateChangedMessage message) => RefreshStatus();
}
