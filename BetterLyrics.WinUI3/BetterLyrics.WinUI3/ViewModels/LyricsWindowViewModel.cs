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
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
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
            IRecipient<PropertyChangedMessage<List<string>>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<string>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<DockPlacement>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ISettingsService _settingsService;
        private readonly ILiveStatesService _liveStatesService;

        private ForegroundWindowWatcher? _windowWatcher = null;
        private bool _ignoreFullscreenWindow;
        private bool _hideWindowWhenNotPlaying;

        private DockPlacement _dockPlacement;
        private int _dockWindowHeight;
        private string _dockMonitorDeviceName;

        public LyricsWindowViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService, ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _mediaSessionsService = mediaSessionsService;
            _liveStatesService = liveStatesService;

            AppSettings = _settingsService.AppSettings;
            LiveStates = _liveStatesService.LiveStates;

            _dockMonitorDeviceName = _settingsService.AppSettings.DockModeSettings.DockMonitorDeviceName;
            _ignoreFullscreenWindow = _settingsService.AppSettings.GeneralSettings.IgnoreFullscreenWindow;
            _hideWindowWhenNotPlaying = _settingsService.AppSettings.GeneralSettings.HideWindowWhenNotPlaying;
            IsImmersiveMode = _settingsService.AppSettings.GeneralSettings.IsImmersiveMode;
            _dockPlacement = _settingsService.AppSettings.DockModeSettings.DockPlacement;
            _dockWindowHeight = _settingsService.AppSettings.DockModeSettings.DockWindowHeight;
            OnIsImmersiveModeChanged(_settingsService.AppSettings.GeneralSettings.IsImmersiveMode);

            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
        }

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            UpdateDockOrDesktopWindow();
        }

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial LiveStates LiveStates { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color ActivatedWindowAccentColor { get; set; }

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

        [ObservableProperty] public partial Visibility AOTFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility FullScreenFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility LockButtonVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility DesktopFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility PIPFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility DockFlyoutItemVisibility { get; set; } = Visibility.Visible;

        [ObservableProperty] public partial bool IsAOTFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsFullScreenFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsDesktopFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsPIPFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsDockFlyoutItemChecked { get; set; } = false;

        private void UpdateDockOrDesktopWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode || LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
            {
                if (_hideWindowWhenNotPlaying && !_mediaSessionsService.IsPlaying)
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, _dockMonitorDeviceName, 0, _dockPlacement);
                    }
                    window.Hide();
                }
                else
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
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
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.IgnoreFullscreenWindow))
                {
                    _ignoreFullscreenWindow = message.NewValue;
                }
                else if (message.PropertyName == nameof(GeneralSettings.HideWindowWhenNotPlaying))
                {
                    _hideWindowWhenNotPlaying = message.NewValue;
                    UpdateDockOrDesktopWindow();
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
            if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.DockWindowHeight))
                {
                    _dockWindowHeight = message.NewValue;
                    UpdateDockOrDesktopWindow();
                }
            }
        }

        public void InitShortcuts()
        {
            UpdateDesktopLockShortcut();
            UpdateDesktopToggleShortcut();
            UpdateDockToggleShortcut();
            UpdatePictureInPictureToggleShortcut();
        }

        private void UpdateDesktopLockShortcut()
        {
            GlobalHotKeyHelper.UnregisterHotKey<LyricsWindow>(ShortcutID.DesktopLock);
            GlobalHotKeyHelper.RegisterHotKey<LyricsWindow>(ShortcutID.DesktopLock,
                _settingsService.AppSettings.DesktopModeSettings.LockShortcut,
                () =>
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
                    {
                        ToggleLockWindow();
                    }
                }
            );
        }

        private void UpdateDesktopToggleShortcut()
        {
            GlobalHotKeyHelper.UnregisterHotKey<LyricsWindow>(ShortcutID.DesktopToggle);
            GlobalHotKeyHelper.RegisterHotKey<LyricsWindow>(ShortcutID.DesktopToggle,
                _settingsService.AppSettings.DesktopModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode ||
                        LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        ToggleDesktopMode();
                    }
                }
            );
        }

        private void UpdateDockToggleShortcut()
        {
            GlobalHotKeyHelper.UnregisterHotKey<LyricsWindow>(ShortcutID.DockToggle);
            GlobalHotKeyHelper.RegisterHotKey<LyricsWindow>(ShortcutID.DockToggle,
                _settingsService.AppSettings.DockModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode ||
                        LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        ToggleDockMode();
                    }
                }
            );
        }

        private void UpdatePictureInPictureToggleShortcut()
        {
            GlobalHotKeyHelper.UnregisterHotKey<LyricsWindow>(ShortcutID.PictureInPictureToggle);
            GlobalHotKeyHelper.RegisterHotKey<LyricsWindow>(ShortcutID.PictureInPictureToggle,
                _settingsService.AppSettings.PictureInPictureModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.PictureInPictureMode ||
                        LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        TogglePictureInPictureMode();
                    }
                }
            );
        }

        private void SetFullscreenTitleBarControlsStatus()
        {
            AOTFlyoutItemVisibility = LockButtonVisibility = DesktopFlyoutItemVisibility = PIPFlyoutItemVisibility = DockFlyoutItemVisibility = Visibility.Collapsed;
            IsFullScreenFlyoutItemChecked = true;
            IsImmersiveMode = true;
        }

        private void SetPIPModeTitleBarControlsStatus()
        {
            AOTFlyoutItemVisibility = DesktopFlyoutItemVisibility = FullScreenFlyoutItemVisibility = DockFlyoutItemVisibility = LockButtonVisibility = Visibility.Collapsed;
            IsImmersiveMode = true;
            IsPIPFlyoutItemChecked = true;
        }

        private void SetDockModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = false;
            AOTFlyoutItemVisibility = DesktopFlyoutItemVisibility = LockButtonVisibility = FullScreenFlyoutItemVisibility = PIPFlyoutItemVisibility = Visibility.Collapsed;
            IsImmersiveMode = true;
            IsDockFlyoutItemChecked = true;
        }

        private void SetDesktopModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = false;
            DockFlyoutItemVisibility = AOTFlyoutItemVisibility = FullScreenFlyoutItemVisibility = PIPFlyoutItemVisibility = Visibility.Collapsed;
            LockButtonVisibility = Visibility.Visible;
            IsDesktopFlyoutItemChecked = true;
        }

        public void SetStandardModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = true;
            AOTFlyoutItemVisibility = DesktopFlyoutItemVisibility = DockFlyoutItemVisibility = PIPFlyoutItemVisibility = FullScreenFlyoutItemVisibility = Visibility.Visible;
            LockButtonVisibility = Visibility.Collapsed;
            IsFullScreenFlyoutItemChecked = IsDesktopFlyoutItemChecked = IsDockFlyoutItemChecked = IsPIPFlyoutItemChecked = false;
            IsAOTFlyoutItemChecked = overlappedPresenter.IsAlwaysOnTop;
            IsImmersiveMode = _settingsService.AppSettings.GeneralSettings.IsImmersiveMode;
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
                        if ((LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode || LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode) && _ignoreFullscreenWindow && window.AppWindow.Presenter is OverlappedPresenter presenter)
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
            WindowPixelSampleMode mode = LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode ? WindowPixelSampleMode.WindowEdge : _dockPlacement.ToWindowPixelSampleMode();
            ActivatedWindowAccentColor = ColorHelper.GetAccentColor(hwnd, _settingsService.AppSettings.DockModeSettings.DockMonitorDeviceName, mode).ToColor();
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

        public void ToggleLockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            if (IsLyricsWindowLocked)
            {
                DesktopModeHelper.SetClickThrough(window, false);
                IsLyricsWindowLocked = false;
                IsImmersiveMode = _settingsService.AppSettings.GeneralSettings.IsImmersiveMode;
            }
            else
            {
                DesktopModeHelper.SetClickThrough(window, true);
                IsLyricsWindowLocked = true;
                IsImmersiveMode = true;
            }

            UpdateDockOrDesktopWindow();
        }

        public void ToggleDesktopMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            StopWatchWindowColorChange();

            LiveStates.ToggleLyricsWindowMode(LyricsWindowMode.DesktopMode);
            if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
            {
                DesktopModeHelper.Enable(window);
                StartWatchWindowColorChange();
                if (_settingsService.AppSettings.DesktopModeSettings.AutoLockOnDesktopMode)
                {
                    ToggleLockWindow();
                }
                SetDesktopModeTitleBarControlsStatus();
            }
            else
            {
                if (IsLyricsWindowLocked)
                {
                    ToggleLockWindow();
                }
                DesktopModeHelper.Disable(window);
                SetStandardModeTitleBarControlsStatus();
            }
        }

        public void ToggleDockMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            StopWatchWindowColorChange();

            LiveStates.ToggleLyricsWindowMode(LyricsWindowMode.DockMode);
            if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode)
            {
                window.Restore();
                DockModeHelper.Enable(window, _dockMonitorDeviceName, _dockWindowHeight, _dockPlacement);
                StartWatchWindowColorChange();
                SetDockModeTitleBarControlsStatus();
            }
            else
            {
                DockModeHelper.Disable(window);
                SetStandardModeTitleBarControlsStatus();
            }

            UpdateDockOrDesktopWindow();
        }

        public void TogglePictureInPictureMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            LiveStates.ToggleLyricsWindowMode(LyricsWindowMode.PictureInPictureMode);
            if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.PictureInPictureMode)
            {
                window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
                SetPIPModeTitleBarControlsStatus();
            }
            else
            {
                window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                SetStandardModeTitleBarControlsStatus();
            }
        }

        public void ToggleAlwaysOnTop()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = !presenter.IsAlwaysOnTop;
                IsAOTFlyoutItemChecked = presenter.IsAlwaysOnTop;
            }
        }

        public void ToggleFullscreen()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            switch (window.AppWindow.Presenter.Kind)
            {
                case AppWindowPresenterKind.FullScreen:
                    window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                    SetStandardModeTitleBarControlsStatus();
                    break;
                case AppWindowPresenterKind.Overlapped:
                    window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    SetFullscreenTitleBarControlsStatus();
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void OnImmersiveToggleButtonEnabledChanged()
        {
            _settingsService.AppSettings.GeneralSettings.IsImmersiveMode = IsImmersiveMode;
        }

        public void Receive(PropertyChangedMessage<DockPlacement> message)
        {
            if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.DockPlacement))
                {
                    _dockPlacement = message.NewValue;
                    UpdateDockOrDesktopWindow();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.DockMonitorDeviceName))
                {
                    _dockMonitorDeviceName = message.NewValue;
                    UpdateDockOrDesktopWindow();
                }
            }
        }

        public void Receive(PropertyChangedMessage<List<string>> message)
        {
            if (message.Sender is DesktopModeSettings)
            {
                if (message.PropertyName == nameof(DesktopModeSettings.LockShortcut))
                {
                    UpdateDesktopLockShortcut();
                }
            }
            else if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.ToggleShortcut))
                {
                    UpdateDockToggleShortcut();
                }
            }
            else if (message.Sender is PictureInPictureModeSettings)
            {
                if (message.PropertyName == nameof(PictureInPictureModeSettings.ToggleShortcut))
                {
                    UpdatePictureInPictureToggleShortcut();
                }
            }
            else if (message.Sender is DesktopModeSettings)
            {
                if (message.PropertyName == nameof(DesktopModeSettings.ToggleShortcut))
                {
                    UpdateDesktopToggleShortcut();
                }
            }
        }
    }
}
