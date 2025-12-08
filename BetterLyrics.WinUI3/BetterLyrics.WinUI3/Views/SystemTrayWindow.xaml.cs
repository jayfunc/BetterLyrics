using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SystemTrayWindow : Window, IRecipient<PropertyChangedMessage<List<string>>>
{
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();

    private readonly WindowMessageMonitor _wmm;

    public SystemTrayWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<PropertyChangedMessage<List<string>>>(this);
        SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

        _wmm = new WindowMessageMonitor(this);
        _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;
    }

    public void InitShortcuts()
    {
        UpdateLyricsWindowSwitchShortcut();
        UpdatePlayOrPauseSongShortcut();
        UpdatePreviousSongShortcut();
        UpdateNextSongShortcut();
        UpdateLyricsWindowShowHideShortcut();
    }

    private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == (uint)User32.WindowMessage.WM_HOTKEY)
        {
            int id = (int)e.Message.WParam;
            GlobalHotKeyHook.TryInvokeAction(id);
        }
        else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_WININICHANGE)
        {
            Debug.WriteLine("==========");
        }
    }

    public void EnsureLyricsWindowStatus()
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
        }
    }

    private void UpdateLyricsWindowSwitchShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutID.LyricsWindowSwitch,
            _settingsService.AppSettings.GeneralSettings.LyricsWindowSwitchShortcut,
            () =>
            {
                WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
            }
        );
    }

    private void UpdatePlayOrPauseSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutID.PlayOrPauseSong, _settingsService.AppSettings.GeneralSettings.PlayOrPauseShortcut, (() =>
        {
            if (_mediaSessionsService.CurrentIsPlaying)
            {
                _ = _mediaSessionsService.PauseAsync();
            }
            else
            {
                _ = _mediaSessionsService.PlayAsync();
            }
        }));
    }

    private void UpdatePreviousSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutID.PreviousSong, _settingsService.AppSettings.GeneralSettings.PreviousSongShortcut, () =>
        {
            _ = _mediaSessionsService.PreviousAsync();
        });
    }

    private void UpdateNextSongShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutID.NextSong, _settingsService.AppSettings.GeneralSettings.NextSongShortcut, () =>
        {
            _ = _mediaSessionsService.NextAsync();
        });
    }

    private void UpdateLyricsWindowShowHideShortcut()
    {
        GlobalHotKeyHook.UpdateHotKey(this, ShortcutID.LyricsWindowShowOrHide,
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

}
