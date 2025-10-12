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

        [ObservableProperty] public partial Visibility FullScreenFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility LockButtonVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility DesktopFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility PIPFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility DockFlyoutItemVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility MinimiseButtonVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility MaximiseButtonVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility RestoreButtonVisibility { get; set; } = Visibility.Visible;
        [ObservableProperty] public partial Visibility CloseButtonVisibility { get; set; } = Visibility.Visible;

        [ObservableProperty] public partial bool IsFullScreenFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsDesktopFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsPIPFlyoutItemChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsDockFlyoutItemChecked { get; set; } = false;

        private void UpdateDockOrDesktopWindow()
        {

            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode || LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode)
            {
                if (_hideWindowWhenNotPlaying && !_mediaSessionsService.IsPlaying)
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(hwnd, _dockMonitorDeviceName, 0, _dockPlacement);
                    }
                    window.Hide();
                }
                else
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode)
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
                    SetIsAlwaysOnTop();
                }
                else if (message.PropertyName == nameof(GeneralSettings.HideWindowWhenNotPlaying))
                {
                    _hideWindowWhenNotPlaying = message.NewValue;
                    UpdateDockOrDesktopWindow();
                }
            }
            else if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.IsAlwaysOnTop))
                {
                    SetIsAlwaysOnTop();
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
            UpdateDesktopLockUnlockShortcut();
            UpdateDesktopToggleShortcut();
            UpdateDockToggleShortcut();
            UpdatePictureInPictureToggleShortcut();
            UpdateLyricsWindowShowHideShortcut();
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
                        WindowHelper.OpenWindow<LyricsWindow>();
                    }
                }
            );
        }

        private void UpdateDesktopLockUnlockShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.DesktopLockOrUnlock,
                _settingsService.AppSettings.DesktopModeSettings.LockShortcut,
                () =>
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode)
                    {
                        ToggleLockWindow();
                    }
                }
            );
        }

        private void UpdateDesktopToggleShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.DesktopToggle,
                _settingsService.AppSettings.DesktopModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode ||
                        LiveStates.LyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        ToggleDesktopMode();
                    }
                }
            );
        }

        private void UpdateDockToggleShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.DockToggle,
                _settingsService.AppSettings.DockModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode ||
                        LiveStates.LyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        ToggleDockMode();
                    }
                }
            );
        }

        private void UpdatePictureInPictureToggleShortcut()
        {
            GlobalHotKeyHelper.UpdateHotKey<LyricsWindow>(ShortcutID.PictureInPictureToggle,
                _settingsService.AppSettings.PictureInPictureModeSettings.ToggleShortcut,
                () =>
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.PictureInPictureMode ||
                        LiveStates.LyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        TogglePictureInPictureMode();
                    }
                }
            );
        }

        private void SetFullscreenTitleBarControlsStatus()
        {
            LockButtonVisibility = DesktopFlyoutItemVisibility = PIPFlyoutItemVisibility = DockFlyoutItemVisibility = Visibility.Collapsed;
            MinimiseButtonVisibility = MaximiseButtonVisibility = RestoreButtonVisibility = Visibility.Collapsed;
            CloseButtonVisibility = Visibility.Visible;
            IsFullScreenFlyoutItemChecked = true;
            IsImmersiveMode = true;
        }

        private void SetPIPModeTitleBarControlsStatus()
        {
            DesktopFlyoutItemVisibility = FullScreenFlyoutItemVisibility = DockFlyoutItemVisibility = LockButtonVisibility = Visibility.Collapsed;
            MinimiseButtonVisibility = MaximiseButtonVisibility = RestoreButtonVisibility = Visibility.Collapsed;
            CloseButtonVisibility = Visibility.Visible;
            IsImmersiveMode = true;
            IsPIPFlyoutItemChecked = true;
        }

        private void SetDockModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = false;
            DesktopFlyoutItemVisibility = LockButtonVisibility = FullScreenFlyoutItemVisibility = PIPFlyoutItemVisibility = Visibility.Collapsed;
            MinimiseButtonVisibility = MaximiseButtonVisibility = RestoreButtonVisibility = Visibility.Collapsed;
            CloseButtonVisibility = Visibility.Visible;
            IsImmersiveMode = true;
            IsDockFlyoutItemChecked = true;
        }

        private void SetDesktopModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = false;
            DockFlyoutItemVisibility = FullScreenFlyoutItemVisibility = PIPFlyoutItemVisibility = Visibility.Collapsed;
            LockButtonVisibility = Visibility.Visible;
            MinimiseButtonVisibility = MaximiseButtonVisibility = RestoreButtonVisibility = Visibility.Collapsed;
            CloseButtonVisibility = Visibility.Visible;
            IsDesktopFlyoutItemChecked = true;
        }

        public void SetStandardModeTitleBarControlsStatus()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;
            var overlappedPresenter = (OverlappedPresenter)window.AppWindow.Presenter;

            overlappedPresenter.IsMinimizable = overlappedPresenter.IsMaximizable = true;
            DesktopFlyoutItemVisibility = DockFlyoutItemVisibility = PIPFlyoutItemVisibility = FullScreenFlyoutItemVisibility = Visibility.Visible;
            LockButtonVisibility = Visibility.Collapsed;
            MinimiseButtonVisibility = MaximiseButtonVisibility = CloseButtonVisibility = Visibility.Visible;
            RestoreButtonVisibility = Visibility.Collapsed;
            IsFullScreenFlyoutItemChecked = IsDesktopFlyoutItemChecked = IsDockFlyoutItemChecked = IsPIPFlyoutItemChecked = false;
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
                        if ((LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode || LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode) && _ignoreFullscreenWindow && window.AppWindow.Presenter is OverlappedPresenter presenter)
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
            WindowPixelSampleMode mode = LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode ? WindowPixelSampleMode.WindowEdge : _dockPlacement.ToWindowPixelSampleMode();
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
            if (LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode)
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
            SetIsAlwaysOnTop();
        }

        public void ToggleDockMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            StopWatchWindowColorChange();

            LiveStates.ToggleLyricsWindowMode(LyricsWindowMode.DockMode);
            if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode)
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
            SetIsAlwaysOnTop();
        }

        public void TogglePictureInPictureMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            LiveStates.ToggleLyricsWindowMode(LyricsWindowMode.PictureInPictureMode);
            if (LiveStates.LyricsWindowMode == LyricsWindowMode.PictureInPictureMode)
            {
                window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
                window.AppWindow.Move(AppSettings.PictureInPictureModeSettings.WindowPosition.ToPointInt32());
                SetPIPModeTitleBarControlsStatus();
            }
            else
            {
                window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                SetStandardModeTitleBarControlsStatus();
            }
            SetIsAlwaysOnTop();
        }

        public void SetIsAlwaysOnTop()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = _liveStatesService.LiveStates.IsAlwaysOnTop;
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
                    UpdateDesktopLockUnlockShortcut();
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
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.ShowOrHideLyricsWindowShortcut))
                {
                    UpdateLyricsWindowShowHideShortcut();
                }
            }
        }
    }
}
