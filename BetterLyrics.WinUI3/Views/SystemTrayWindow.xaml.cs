using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;
using WinUIEx.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SystemTrayWindow : Window,
    IRecipient<PropertyChangedMessage<List<string>>>,
    IRecipient<PropertyChangedMessage<bool>>
{
    private ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();

    private WindowMessageMonitor _wmm;

    public SystemTrayWindow()
    {
        InitializeComponent();
        SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

        _wmm = new WindowMessageMonitor(this);
        _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

        WeakReferenceMessenger.Default.RegisterAll(this);

        InitShortcuts();

        EnsureLyricsWindowStatus();
    }

    private void InitShortcuts()
    {
        UpdateLyricsWindowSwitchShortcut();
        UpdatePlayOrPauseSongShortcut();
        UpdatePreviousSongShortcut();
        UpdateNextSongShortcut();
        UpdateLyricsWindowShowHideShortcut();
    }

    private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if ((User32.WindowMessage)e.Message.MessageId == User32.WindowMessage.WM_HOTKEY)
        {
            int id = (int)e.Message.WParam;
            GlobalHotKeyHook.TryInvokeAction(id);
        }
    }

    private void EnsureLyricsWindowStatus()
    {
        var records = _settingsService.AppSettings.WindowBoundsRecords;
        if (records.Count == 0)
        {
            var defaultStatus = LyricsWindowStatusExtensions.StandardMode(this);
            defaultStatus.IsDefault = true;
            records.Add(defaultStatus);
            records.Add(LyricsWindowStatusExtensions.DesktopMode(this));
            records.Add(LyricsWindowStatusExtensions.DockedMode(this));
            records.Add(LyricsWindowStatusExtensions.NarrowMode(this));
            records.Add(LyricsWindowStatusExtensions.FullscreenMode(this));
            records.Add(LyricsWindowStatusExtensions.TaskbarMode(this));
        }
    }

    private void UpdateLyricsWindowSwitchShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutId.LyricsWindowSwitch,
            _settingsService.AppSettings.GeneralSettings.LyricsWindowSwitchShortcut,
            () =>
            {
                WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
            }
        );
    }

    private void UpdatePlayOrPauseSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutId.PlayOrPauseSong, _settingsService.AppSettings.GeneralSettings.PlayOrPauseShortcut, (() =>
        {
            if (_gsmtcService.CurrentIsPlaying)
            {
                _ = _gsmtcService.PauseAsync();
            }
            else
            {
                _ = _gsmtcService.PlayAsync();
            }
        }));
    }

    private void UpdatePreviousSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutId.PreviousSong, _settingsService.AppSettings.GeneralSettings.PreviousSongShortcut, () =>
        {
            _ = _gsmtcService.PreviousAsync();
        });
    }

    private void UpdateNextSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutId.NextSong, _settingsService.AppSettings.GeneralSettings.NextSongShortcut, () =>
        {
            _ = _gsmtcService.NextAsync();
        });
    }

    private void UpdateLyricsWindowShowHideShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutId.LyricsWindowShowOrHide,
            _settingsService.AppSettings.GeneralSettings.ShowOrHideLyricsWindowShortcut,
            () =>
            {
                var windows = WindowHook.GetWindows<NowPlayingWindow>();

                foreach (var window in windows)
                {
                    if (window.Visible)
                    {
                        window.HideWindow();
                    }
                    else
                    {
                        WindowHook.OpenOrShowWindow<NowPlayingWindow>(window.LyricsWindowStatus);
                    }
                }
            }
        );
    }

    private void UpdateScreenKeeperStatus()
    {
        // 检测已打开的窗口中是否存在配置为不休眠的窗口
        var isKeepScreenOpen = _settingsService.AppSettings.WindowBoundsRecords.Where(x => x.IsOpened).Any(x => x.IsKeepScreenOpen);
        ScreenKeeper.SetState(isKeepScreenOpen);
    }

    public void Receive(PropertyChangedMessage<List<string>> message)
    {
        if (message.Sender is GeneralSettings)
        {
            if (message.PropertyName == nameof(GeneralSettings.LyricsWindowSwitchShortcut))
            {
                UpdateLyricsWindowSwitchShortcut();
            }
            else if (message.PropertyName == nameof(GeneralSettings.PlayOrPauseShortcut))
            {
                UpdatePlayOrPauseSongShortcut();
            }
            else if (message.PropertyName == nameof(GeneralSettings.PreviousSongShortcut))
            {
                UpdatePreviousSongShortcut();
            }
            else if (message.PropertyName == nameof(GeneralSettings.NextSongShortcut))
            {
                UpdateNextSongShortcut();
            }
            else if (message.PropertyName == nameof(GeneralSettings.ShowOrHideLyricsWindowShortcut))
            {
                UpdateLyricsWindowShowHideShortcut();
            }
        }
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.IsKeepScreenOpen))
            {
                UpdateScreenKeeperStatus();
            }
            else if (message.PropertyName == nameof(LyricsWindowStatus.IsOpened))
            {
                UpdateScreenKeeperStatus();
            }
        }
    }
}
