using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.Messaging;

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

    public GlobalShortcutService(
        IGlobalShortcutProvider shortcutProvider,
        ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider,
        IGsmtcService gsmtcService)
    {
        _shortcutProvider = shortcutProvider;
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _gsmtcService = gsmtcService;

        WeakReferenceMessenger.Default.RegisterAll(this);
        RefreshAllHotKeys();
    }

    public bool IsHotKeyRegistered(List<string> keys)
    {
        return _shortcutProvider.IsHotKeyRegistered(keys);
    }
    public void RefreshAllHotKeys()
    {
        UpdateLyricsWindowSwitchShortcut();
        UpdatePlayOrPauseSongShortcut();
        UpdatePreviousSongShortcut();
        UpdateNextSongShortcut();
        UpdateLyricsWindowShowHideShortcut();
    }
    private void UpdateLyricsWindowSwitchShortcut()
    {
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.LyricsWindowSwitch,
            _settingsService.AppSettings.GeneralSettings.LyricsWindowSwitchShortcut,
            () => _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow)
        );
    }
    private void UpdatePlayOrPauseSongShortcut()
    {
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.PlayOrPauseSong,
            _settingsService.AppSettings.GeneralSettings.PlayOrPauseShortcut,
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
    }
    private void UpdatePreviousSongShortcut()
    {
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.PreviousSong,
            _settingsService.AppSettings.GeneralSettings.PreviousSongShortcut,
            () => { _ = _gsmtcService.PreviousAsync(); }
        );
    }
    private void UpdateNextSongShortcut()
    {
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.NextSong,
            _settingsService.AppSettings.GeneralSettings.NextSongShortcut,
            () => { _ = _gsmtcService.NextAsync(); }
        );
    }
    private void UpdateLyricsWindowShowHideShortcut()
    {
        _shortcutProvider.UpdateHotKey(
            (int)ShortcutId.LyricsWindowShowOrHide,
            _settingsService.AppSettings.GeneralSettings.ShowOrHideLyricsWindowShortcut,
            () => _windowManagerProvider.ToggleWindows(WindowType.NowPlayingWindow)
        );
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
