using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class GlobalShortcutService : IGlobalShortcutService,
    IRecipient<GlobalShortcutChangedMessage>
{
    private readonly IGlobalShortcutProvider _shortcutProvider;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IGsmtcService _gsmtcService;
    private readonly ILogger<GlobalShortcutService> _logger;

    public GlobalShortcutService(
        IGlobalShortcutProvider shortcutProvider,
        ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider,
        IGsmtcService gsmtcService,
        ILogger<GlobalShortcutService> logger)
    {
        _shortcutProvider = shortcutProvider;
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _gsmtcService = gsmtcService;
        _logger = logger;

        WeakReferenceMessenger.Default.RegisterAll(this);
        RefreshAllHotKeys();
    }

    public bool IsHotKeyRegistered(List<string> keys)
    {
        return _shortcutProvider.IsHotKeyRegistered(keys);
    }
    public void RefreshAllHotKeys()
    {
        _logger.LogInformation("Refreshing all global shortcuts.");
        UpdateLyricsWindowSwitchShortcut();
        UpdatePlayOrPauseSongShortcut();
        UpdatePreviousSongShortcut();
        UpdateNextSongShortcut();
        UpdateLyricsWindowShowHideShortcut();
    }

    private void LogShortcutUpdate(ShortcutId shortcutId, List<string> keys)
    {
        var registered = _shortcutProvider.IsHotKeyRegistered(keys);
        if (registered)
        {
            _logger.LogDebug("Registered shortcut {ShortcutId} with keys {Keys}.", shortcutId,
                string.Join("+", keys));
        }
        else
        {
            _logger.LogWarning("Failed to register shortcut {ShortcutId} with keys {Keys}.", shortcutId,
                string.Join("+", keys));
        }
    }
    private void UpdateLyricsWindowSwitchShortcut()
    {
        var keys = _settingsService.AppSettings.GeneralSettings.LyricsWindowSwitchShortcut;
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.LyricsWindowSwitch,
            keys,
            () => _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow)
        );
        LogShortcutUpdate(ShortcutId.LyricsWindowSwitch, keys);
    }
    private void UpdatePlayOrPauseSongShortcut()
    {
        var keys = _settingsService.AppSettings.GeneralSettings.PlayOrPauseShortcut;
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.PlayOrPauseSong,
            keys,
            () =>
            {
                if (_gsmtcService.CurrentIsPlaying)
                {
                    _ = _gsmtcService.PauseAsync();
                }
                else
                {
                    _ = _gsmtcService.PlayAsync();
                }
            }
        );
        LogShortcutUpdate(ShortcutId.PlayOrPauseSong, keys);
    }
    private void UpdatePreviousSongShortcut()
    {
        var keys = _settingsService.AppSettings.GeneralSettings.PreviousSongShortcut;
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.PreviousSong,
            keys,
            () => { _ = _gsmtcService.PreviousAsync(); }
        );
        LogShortcutUpdate(ShortcutId.PreviousSong, keys);
    }
    private void UpdateNextSongShortcut()
    {
        var keys = _settingsService.AppSettings.GeneralSettings.NextSongShortcut;
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.NextSong,
            keys,
            () => { _ = _gsmtcService.NextAsync(); }
        );
        LogShortcutUpdate(ShortcutId.NextSong, keys);
    }
    private void UpdateLyricsWindowShowHideShortcut()
    {
        var keys = _settingsService.AppSettings.GeneralSettings.ShowOrHideLyricsWindowShortcut;
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.LyricsWindowShowOrHide,
            keys,
            () => _windowManagerProvider.ToggleWindows(WindowType.NowPlayingWindow)
        );
        LogShortcutUpdate(ShortcutId.LyricsWindowShowOrHide, keys);
    }

    public void Receive(GlobalShortcutChangedMessage message)
    {
        switch (message.ShortcutId)
        {
            case ShortcutId.LyricsWindowSwitch:
                UpdateLyricsWindowSwitchShortcut();
                break;
            case ShortcutId.PlayOrPauseSong:
                UpdatePlayOrPauseSongShortcut();
                break;
            case ShortcutId.PreviousSong:
                UpdatePreviousSongShortcut();
                break;
            case ShortcutId.NextSong:
                UpdateNextSongShortcut();
                break;
            case ShortcutId.LyricsWindowShowOrHide:
                UpdateLyricsWindowShowHideShortcut();
                break;
        }
    }
}
