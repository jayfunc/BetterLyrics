// 2025/6/23 by Zhe Fang

using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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

        public LyricsWindowViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            WeakReferenceMessenger.Default.Register<ShowNotificatonMessage>(
                this,
                async (r, m) =>
                {
                    Notification = m.Value;
                    if (!Notification.IsForeverDismissable)
                    {
                        Notification.Visibility = Notification.IsForeverDismissable ? Visibility.Visible : Visibility.Collapsed;
                        ShowInfoBar = true;
                        await Task.Delay(AnimationHelper.StackedNotificationsShowingDuration);
                        ShowInfoBar = false;
                    }
                }
            );
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
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontSize))
                {
                    if (IsDockMode)
                    {
                        var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
                        DockModeHelper.UpdateAppBarHeight(
                            WindowNative.GetWindowHandle(window),
                            message.NewValue * 3
                        );
                    }
                }
            }
        }

        public void StartWatchWindowColorChange(WindowColorSampleMode mode)
        {
            var hwnd = WindowNative.GetWindowHandle(
                WindowHelper.GetWindowByWindowType<LyricsWindow>()
            );
            _watcherHelper = new ForegroundWindowWatcherHelper(
                hwnd,
                onWindowChanged =>
                {
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
            DesktopModeHelper.Lock(window);
            IsLyricsWindowLocked = true;
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
            StopWatchWindowColorChange();

            IsDesktopMode = !IsDesktopMode;
            if (IsDesktopMode)
            {
                StartWatchWindowColorChange(WindowColorSampleMode.WindowEdge);
                DesktopModeHelper.Enable(window);
            }
            else
            {
                DesktopModeHelper.Disable(window);
                StopWatchWindowColorChange();
            }
        }

        [RelayCommand]
        private void ToggleDockMode()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            StopWatchWindowColorChange();

            IsDockMode = !IsDockMode;
            if (IsDockMode)
            {
                StartWatchWindowColorChange(WindowColorSampleMode.BelowWindow);
                DockModeHelper.Enable(window, _settingsService.LyricsFontSize * 3);
            }
            else
            {
                StartWatchWindowColorChange(WindowColorSampleMode.WindowEdge);
                DockModeHelper.Disable(window);
            }
        }
    }
}
