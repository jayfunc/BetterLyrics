// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics;
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
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<bool>>
    {
        private ForegroundWindowWatcher? _windowWatcher = null;
        private bool _ignoreFullscreenWindow = false;

        public LyricsWindowViewModel(ISettingsService settingsService) : base(settingsService)
        {
            _ignoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;
            IsImmersiveMode = _settingsService.IsImmersiveMode;
            OnIsImmersiveModeChanged(_settingsService.IsImmersiveMode);
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
        public partial float TopCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial ElementTheme ThemeType { get; set; } = ElementTheme.Default;

        [ObservableProperty]
        public partial double TitleBarFontSize { get; set; } = 11;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsMouseWithinWindow { get; set; } = false;

        [ObservableProperty]
        public partial string LockHotKey { get; set; } = "";

        partial void OnIsImmersiveModeChanged(bool value)
        {
            if (value)
            {
                TopCommandGridOpacity = 0f;
            }
            else
            {
                TopCommandGridOpacity = 0.5f;
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
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontSize))
                {
                    if (IsDockMode)
                    {
                        var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
                        if (window == null) return;

                        DockModeHelper.UpdateAppBarHeight(WindowNative.GetWindowHandle(window), message.NewValue * 4);
                    }
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

        public void StartWatchWindowColorChange(WindowPixelSampleMode mode)
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);
            _windowWatcher = new ForegroundWindowWatcher(
                hwnd,
                onWindowChanged =>
                {
                    if (_ignoreFullscreenWindow && window.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsAlwaysOnTop = true;
                    }
                    UpdateAccentColor(hwnd, mode);
                }
            );
            _windowWatcher.Start();
            UpdateAccentColor(hwnd, mode);
        }

        private void StopWatchWindowColorChange()
        {
            _windowWatcher?.Stop();
            _windowWatcher = null;
        }

        public void UpdateAccentColor(nint hwnd, WindowPixelSampleMode mode)
        {
            ActivatedWindowAccentColor = Helper.ColorHelper.GetAccentColor(hwnd, mode).ToColor();
        }

        public void InitLockHotKey()
        {
            UpdateLockHotKey(_settingsService.LockHotKeyIndex);
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
                IsImmersiveMode = _settingsService.IsImmersiveMode;
            }
            else
            {
                DesktopModeHelper.SetClickThrough(window, true);
                IsLyricsWindowLocked = true;
                IsImmersiveMode = true;
            }
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
                StartWatchWindowColorChange(WindowPixelSampleMode.WindowEdge);
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
                DockModeHelper.Enable(window, _settingsService.LyricsFontSize * 4);
                StartWatchWindowColorChange(WindowPixelSampleMode.BelowWindow);
            }
            else
            {
                DockModeHelper.Disable(window);
            }
        }

        [RelayCommand]
        private void OnImmersiveToggleButtonEnabledChanged()
        {
            _settingsService.IsImmersiveMode = IsImmersiveMode;
        }
    }
}
