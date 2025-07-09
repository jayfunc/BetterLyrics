// 2025/6/23 by Zhe Fang

using System.Diagnostics;
using System.Threading.Tasks;
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
using Windows.UI;
using WinRT.Interop;

namespace BetterLyrics.WinUI3
{
    public partial class LyricsWindowViewModel
        : BaseWindowViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<bool>>
    {
        private ForegroundWindowWatcherHelper? _watcherHelper = null;

        public LyricsWindowViewModel(ISettingsService settingsService) : base(settingsService)
        {
            _ignoreFullscreenWindow = _settingsService.IgnoreFullscreenWindow;
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
        public partial Notification Notification { get; set; } = new();

        [ObservableProperty]
        public partial bool ShowInfoBar { get; set; } = false;

        [ObservableProperty]
        public partial ElementTheme ThemeType { get; set; } = ElementTheme.Default;

        [ObservableProperty]
        public partial double TitleBarFontSize { get; set; } = 11;

        [ObservableProperty]
        public partial double TitleBarHeight { get; set; } = 36;

        private bool _ignoreFullscreenWindow = false;

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
            }
        }

        public void StartWatchWindowColorChange(WindowColorSampleMode mode)
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);
            _watcherHelper = new ForegroundWindowWatcherHelper(
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
            _watcherHelper.Start();
            UpdateAccentColor(hwnd, mode);
        }

        public void UpdateAccentColor(nint hwnd, WindowColorSampleMode mode)
        {
            ActivatedWindowAccentColor = WindowColorHelper.GetDominantColor(hwnd, mode).ToColor();
        }

        [RelayCommand]
        private void LockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            DesktopModeHelper.Lock(window);
            IsLyricsWindowLocked = true;
            StartWatchWindowColorChange(WindowColorSampleMode.WindowEdge);
        }

        private void StopWatchWindowColorChange()
        {
            _watcherHelper?.Stop();
            _watcherHelper = null;
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
                StartWatchWindowColorChange(WindowColorSampleMode.BelowWindow);
            }
            else
            {
                DockModeHelper.Disable(window);
            }
        }
    }
}
