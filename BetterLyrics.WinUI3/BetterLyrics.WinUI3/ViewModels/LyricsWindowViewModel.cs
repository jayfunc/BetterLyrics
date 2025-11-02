// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3
{
    public partial class LyricsWindowViewModel
        : BaseWindowViewModel,
            IRecipient<PropertyChangedMessage<List<string>>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ISettingsService _settingsService;
        private readonly ILiveStatesService _liveStatesService;

        private ForegroundWindowWatcher? _fgWindowWatcher = null;
        private DispatcherQueueTimer? _fgWindowWatcherTimer = null;

        public LyricsWindowViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _mediaSessionsService = mediaSessionsService;
            _liveStatesService = liveStatesService;

            AppSettings = _settingsService.AppSettings;
            LiveStates = _liveStatesService.LiveStates;

            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
        }

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            WindowHelper.SetLyricsWindowVisibilityByPlayingStatus(_dispatcherQueue);
        }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        [ObservableProperty] public partial LiveStates LiveStates { get; set; }

        /// <summary>
        /// 歌词窗口所在的背景主题色
        /// </summary>
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color BackdropAccentColor { get; set; }

        [ObservableProperty] public partial double TopCommandGridOpacity { get; set; } = 0;

        [ObservableProperty] public partial ElementTheme ThemeType { get; set; } = ElementTheme.Default;

        [ObservableProperty] public partial double TitleBarFontSize { get; set; } = 12;

        [ObservableProperty] public partial Visibility CloseButtonVisibility { get; set; } = Visibility.Visible;

        public void InitShortcuts()
        {
            UpdateLyricsWindowBorderlessShortcut();
            UpdateLyricsWindowClickThroughShortcut();
            UpdateLyricsWindowShowHideShortcut();
            UpdateLyricsWindowSwitchShortcut();
        }

        private void UpdateLyricsWindowShowHideShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.LyricsWindowShowOrHide,
                _settingsService.AppSettings.GeneralSettings.ShowOrHideLyricsWindowShortcut,
                () =>
                {
                    var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
                    if (window == null) return;

                    if (window.Visible)
                    {
                        window.Hide();
                    }
                    else
                    {
                        WindowHelper.OpenOrShowWindow<LyricsWindow>();
                    }
                }
            );
        }

        private void UpdateLyricsWindowBorderlessShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.Borderless,
                _settingsService.AppSettings.GeneralSettings.BorderlessShortcut,
                () =>
                {
                    LiveStates.LyricsWindowStatus.IsBorderless = !LiveStates.LyricsWindowStatus.IsBorderless;
                }
            );
        }

        private void UpdateLyricsWindowClickThroughShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.ClickThrough,
                _settingsService.AppSettings.GeneralSettings.ClickThroughShortcut,
                () =>
                {
                    LiveStates.LyricsWindowStatus.IsClickThrough = !LiveStates.LyricsWindowStatus.IsClickThrough;
                }
            );
        }

        private void UpdateLyricsWindowSwitchShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.LyricsWindowSwitch,
                _settingsService.AppSettings.GeneralSettings.LyricsWindowSwitchShortcut,
                () =>
                {
                    WindowHelper.OpenOrShowWindow<LyricsWindowSwitchWindow>();
                }
            );
        }

        public void InitFgWindowWatcher()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            _fgWindowWatcherTimer = _dispatcherQueue.CreateTimer();
            _fgWindowWatcher = new ForegroundWindowWatcher(
                hwnd,
                fgHwnd =>
                {
                    _fgWindowWatcherTimer.Debounce(() =>
                    {
                        if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAlwaysOnTop &&
                            _liveStatesService.LiveStates.LyricsWindowStatus.IsAlwaysOnTopPolling &&
                            window.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsAlwaysOnTop = true;
                        }
                        if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
                        {
                            UpdateBackdropAccentColor(hwnd);
                        }
                    }, Constants.Time.DebounceTimeout);
                }
            );
            _fgWindowWatcher.Start();
            UpdateBackdropAccentColor(hwnd);
        }

        public void UpdateBackdropAccentColor(nint hwnd)
        {
            BackdropAccentColor = ColorHelper.GetAccentColor(
                hwnd,
                _liveStatesService.LiveStates.LyricsWindowStatus.MonitorDeviceName,
                _liveStatesService.LiveStates.LyricsWindowStatus.EnvironmentSampleMode).ToColor();
        }

        public void ExitOrClose()
        {
            if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            {
                WindowHelper.ExitApp();
            }
            else
            {
                var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
                window?.Hide();
            }
        }

        public void RefreshLyricsWindowStatus()
        {
            _liveStatesService.RefreshLyricsWindowStatus();
        }

        public void Receive(PropertyChangedMessage<List<string>> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.ClickThroughShortcut))
                {
                    UpdateLyricsWindowClickThroughShortcut();
                }
            }
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.BorderlessShortcut))
                {
                    UpdateLyricsWindowBorderlessShortcut();
                }
            }
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.ShowOrHideLyricsWindowShortcut))
                {
                    UpdateLyricsWindowShowHideShortcut();
                }
            }
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.LyricsWindowSwitchShortcut))
                {
                    UpdateLyricsWindowSwitchShortcut();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.ThemeTypeSent))
                {
                    ThemeType = message.NewValue;
                }
            }
        }
    }
}
