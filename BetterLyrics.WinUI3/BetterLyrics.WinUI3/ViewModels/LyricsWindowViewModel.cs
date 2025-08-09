// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel;
using BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.System;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3
{
    public partial class LyricsWindowViewModel
        : BaseWindowViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<string>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<DockPlacement>>
    {
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private ForegroundWindowWatcher? _windowWatcher = null;
        private bool _ignoreFullscreenWindow;
        private bool _hideWindowWhenNotPlaying;

        private DockPlacement _dockPlacement;
        private int _dockWindowHeight;
        private string _dockMonitorDeviceName;

        public LyricsWindowViewModel(ISettingsService settingsService) : base(settingsService)
        {
            _dockMonitorDeviceName = _settingsService.AppSettings.DockMonitorDeviceName;
            _ignoreFullscreenWindow = _settingsService.AppSettings.IgnoreFullscreenWindow;
            _hideWindowWhenNotPlaying = _settingsService.AppSettings.HideWindowWhenNotPlaying;
            IsImmersiveMode = _settingsService.AppSettings.IsImmersiveMode;
            _dockPlacement = _settingsService.AppSettings.DockPlacement;
            _dockWindowHeight = _settingsService.AppSettings.DockWindowHeight;
            OnIsImmersiveModeChanged(_settingsService.AppSettings.IsImmersiveMode);

            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
        }

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            UpdateDockWindow();
        }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color ActivatedWindowAccentColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDesktopMode { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDockMode { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsWindowLocked { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsImmersiveMode { get; set; }

        [ObservableProperty]
        public partial double TopCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial ElementTheme ThemeType { get; set; } = ElementTheme.Default;

        [ObservableProperty]
        public partial double TitleBarFontSize { get; set; } = 11;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsMouseWithinWindow { get; set; } = false;

        [ObservableProperty]
        public partial string LockHotKey { get; set; } = "";

        private void UpdateDockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (IsDockMode || IsDesktopMode)
            {
                if (_hideWindowWhenNotPlaying && !_mediaSessionsService.IsPlaying)
                {
                    if (IsDockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, _dockMonitorDeviceName, 0, _dockPlacement);
                    }
                    window.Hide();
                }
                else
                {
                    if (IsDockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, _dockMonitorDeviceName, _dockWindowHeight, _dockPlacement);
                    }
                    window.Show();
                }
            }
        }

        partial void OnIsImmersiveModeChanged(bool value)
        {
            if (value)
            {
                TopCommandGridOpacity = 0f;
            }
            else
            {
                TopCommandGridOpacity = 1f;
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SystemTrayViewModel)
            {
                if (message.PropertyName == nameof(SystemTrayViewModel.IsLyricsWindowLocked))
                {
                    if (IsLyricsWindowLocked != message.NewValue)
                    {
                        IsLyricsWindowLocked = message.NewValue;
                    }
                }
            }
            else if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.IgnoreFullscreenWindow))
                {
                    _ignoreFullscreenWindow = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.HideWindowWhenNotPlaying))
                {
                    _hideWindowWhenNotPlaying = message.NewValue;
                    UpdateDockWindow();
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

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.DockWindowHeight))
                {
                    _dockWindowHeight = message.NewValue;
                    UpdateDockWindow();
                }
                else if (message.Sender is SettingsPageViewModel)
                {
                    if (message.PropertyName == nameof(SettingsPageViewModel.LockHotKeyIndex))
                    {
                        UpdateLockHotKey(message.NewValue);
                    }
                }
            }
        }

        private void UpdateLockHotKey(int hotKeyIndex)
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            GlobalHotKeyHelper.UnregisterAllHotKeys(window);
            GlobalHotKeyHelper.RegisterHotKey(
                window,
                User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT,
                (uint)(hotKeyIndex + (int)VirtualKey.A),
                () =>
                {
                    if (IsDesktopMode)
                    {
                        ToggleLockWindowCommand.Execute(null);
                    }
                }
            );
            LockHotKey = ((VirtualKey)(hotKeyIndex + (int)VirtualKey.A)).ToString();
        }

        public void StartWatchWindowColorChange()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);
            _windowWatcher = new ForegroundWindowWatcher(
                hwnd,
                fgHwnd =>
                {
                    _dispatcherQueueTimer.Debounce(() =>
                    {
                        if ((IsDockMode || IsDesktopMode) && _ignoreFullscreenWindow && window.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsAlwaysOnTop = true;
                        }
                        UpdateAccentColor(hwnd);
                    }, Constants.Time.DebounceTimeout);
                }
            );
            _windowWatcher.Start();
            UpdateAccentColor(hwnd);
        }

        private void StopWatchWindowColorChange()
        {
            _windowWatcher?.Stop();
            _windowWatcher = null;
        }

        public void UpdateAccentColor(nint hwnd)
        {
            WindowPixelSampleMode mode = IsDesktopMode ? WindowPixelSampleMode.WindowEdge : _dockPlacement.ToWindowPixelSampleMode();
            ActivatedWindowAccentColor = Helper.ColorHelper.GetAccentColor(hwnd, _settingsService.AppSettings.DockMonitorDeviceName, mode).ToColor();
        }

        public void InitLockHotKey()
        {
            UpdateLockHotKey(_settingsService.AppSettings.LockHotKeyIndex);
        }

        [RelayCommand]
        private void ToggleLockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            if (IsLyricsWindowLocked)
            {
                DesktopModeHelper.SetClickThrough(window, false);
                IsLyricsWindowLocked = false;
                IsImmersiveMode = _settingsService.AppSettings.IsImmersiveMode;
            }
            else
            {
                DesktopModeHelper.SetClickThrough(window, true);
                IsLyricsWindowLocked = true;
                IsImmersiveMode = true;
            }

            UpdateDockWindow();
        }

        [RelayCommand]
        private void ToggleDesktopMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            StopWatchWindowColorChange();

            IsDesktopMode = !IsDesktopMode;
            if (IsDesktopMode)
            {
                DesktopModeHelper.Enable(window);
                StartWatchWindowColorChange();
            }
            else
            {
                DesktopModeHelper.Disable(window);
            }
        }

        [RelayCommand]
        private void ToggleDockMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            StopWatchWindowColorChange();

            IsDockMode = !IsDockMode;
            if (IsDockMode)
            {
                window.Restore();
                DockModeHelper.Enable(window, _dockMonitorDeviceName, _dockWindowHeight, _dockPlacement);
                StartWatchWindowColorChange();
            }
            else
            {
                DockModeHelper.Disable(window);
            }

            UpdateDockWindow();
        }

        [RelayCommand]
        private void OnImmersiveToggleButtonEnabledChanged()
        {
            _settingsService.AppSettings.IsImmersiveMode = IsImmersiveMode;
        }

        public void Receive(PropertyChangedMessage<DockPlacement> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.DockPlacement))
                {
                    _dockPlacement = message.NewValue;
                    UpdateDockWindow();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SelectedDockMonitorDeviceName))
                {
                    _dockMonitorDeviceName = message.NewValue;
                    UpdateDockWindow();
                }
            }
        }
    }
}
