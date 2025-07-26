// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
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
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<DockPlacement>>
    {
        private readonly IPlaybackService _playbackService = Ioc.Default.GetRequiredService<IPlaybackService>();
        private ForegroundWindowWatcher? _windowWatcher = null;
        private bool _ignoreFullscreenWindow;
        private bool _hideWindowWhenNotPlaying;

        private DockPlacement _dockPlacement;
        private int _dockWindowHeight;

        public LyricsWindowViewModel(ISettingsService settingsService) : base(settingsService)
        {
            _ignoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;
            _hideWindowWhenNotPlaying = _settingsService.HideWindowWhenNotPlaying;
            IsImmersiveMode = _settingsService.IsImmersiveMode;
            _dockPlacement = _settingsService.DockPlacement;
            _dockWindowHeight = _settingsService.DockWindowHeight;
            OnIsImmersiveModeChanged(_settingsService.IsImmersiveMode);

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
        }

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            AutoHideOrShowWindow();
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

        private void AutoHideOrShowWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (IsDockMode || IsDesktopMode)
            {
                if (_hideWindowWhenNotPlaying && !_playbackService.IsPlaying)
                {
                    if (IsDockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, 0, _dockPlacement);
                    }
                    window.Hide();
                }
                else
                {
                    if (IsDockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, _dockWindowHeight, _dockPlacement);
                    }
                    window.Show();
                }
            }
        }

        private void UpdateDockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            if (!_hideWindowWhenNotPlaying || _playbackService.SongInfo != null)
            {
                DockModeHelper.UpdateAppBarHeight(WindowNative.GetWindowHandle(window), _dockWindowHeight, _dockPlacement);
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
                    AutoHideOrShowWindow();
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
                onWindowChanged =>
                {
                    _dispatcherQueueTimer.Debounce(() =>
                    {
                        if (_ignoreFullscreenWindow && window.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsAlwaysOnTop = true;
                        }
                        UpdateAccentColor(hwnd);
                    }, TimeSpan.FromMilliseconds(300));
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

            AutoHideOrShowWindow();
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
                DockModeHelper.Enable(window, _dockWindowHeight, _dockPlacement);
                StartWatchWindowColorChange();
            }
            else
            {
                DockModeHelper.Disable(window);
            }

            AutoHideOrShowWindow();
        }

        [RelayCommand]
        private void OnImmersiveToggleButtonEnabledChanged()
        {
            _settingsService.IsImmersiveMode = IsImmersiveMode;
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
    }
}
