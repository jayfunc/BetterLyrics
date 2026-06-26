using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;
using WinUIEx;
using WinUIEx.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsWindowSwitchWindow : Window,
        IRecipient<PropertyChangedMessage<ElementTheme>>,
        IRecipient<PropertyChangedMessage<WindowStatus>>,
        IRecipient<PropertyChangedMessage<List<string>>>,
        IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly WindowMessageMonitor _wmm;
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();

        public LyricsWindowSwitchWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<LyricsWindowSwitchWindowViewModel>();

        public LyricsWindowSwitchWindow()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);

            _wmm = new WindowMessageMonitor(this);
            _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

            InitShortcuts();

            this.Init("LyricsWindowSwitchWindowTitle", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);
            this.SyncTheme();

            SetTitleBar(PlaceholderGrid);
            this.CenterOnScreen();
            this.SetIsBorderless(true);
            AppWindow.IsShownInSwitchers = false;
            this.SetIsAlwaysOnTop(true);

            AppWindow.Changed += AppWindow_Changed;
        }

        private void InitShortcuts()
        {
            UpdateLyricsWindowSwitchShortcut();
            UpdatePlayOrPauseSongShortcut();
            UpdatePreviousSongShortcut();
            UpdateNextSongShortcut();
            UpdateLyricsWindowShowHideShortcut();
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
            var isKeepScreenOpen = _settingsService.AppSettings.WindowBoundsRecords
                .Where(x => x.WindowStatus == WindowStatus.Opened)
                .Any(x => x.IsKeepScreenOpen);
            ScreenKeeper.SetState(isKeepScreenOpen);
        }

        private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            if ((User32.WindowMessage)e.Message.MessageId == User32.WindowMessage.WM_HOTKEY)
            {
                int id = (int)e.Message.WParam;
                GlobalHotKeyHook.TryInvokeAction(id);
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidVisibilityChange)
            {
                if (sender.IsVisible)
                {
                    ViewModel.RootGridOpacity = 1;
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                {
                    this.SyncTheme();
                }
            }
        }

        public void Receive(PropertyChangedMessage<WindowStatus> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowStatus))
                {
                    UpdateScreenKeeperStatus();
                }
            }
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
            }
        }

    }
}
