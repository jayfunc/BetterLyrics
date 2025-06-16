using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial bool IsDesktopLyricsOpened { get; set; } = false;

        [ObservableProperty]
        public partial bool IsInAppLyricsOpened { get; set; } = false;

        public SystemTrayPageViewModel(ISettingsService settingsService)
            : base(settingsService) { }

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            if (App.Current.SettingsWindow == null)
            {
                var settingsWindow = new HostWindow();
                WindowHelper.TrackWindow(settingsWindow);
                settingsWindow.Navigate(typeof(SettingsPage));
                settingsWindow.Activate();
            }
            else
            {
                App.Current.SettingsWindow.Activate();
            }
        }

        [RelayCommand]
        private void DesktopLyricsToggled()
        {
            if (IsDesktopLyricsOpened)
            {
                CloseDesktopLyricsWindow();
            }
            else
            {
                OpenDesktopLyricsWindow();
            }
            IsDesktopLyricsOpened = !IsDesktopLyricsOpened;
        }

        [RelayCommand]
        private void InAppLyricsToggled()
        {
            if (IsInAppLyricsOpened)
            {
                CloseInAppLyricsWindow();
            }
            else
            {
                OpenInAppLyricsWindow();
            }
            IsInAppLyricsOpened = !IsInAppLyricsOpened;
        }

        private void OpenDesktopLyricsWindow()
        {
            var overlayWindow = new OverlayWindow(
                clickThrough: true,
                listenOnActivatedWindowChange: true
            );
            WindowHelper.TrackWindow(overlayWindow);
            AppBarHelper.Enable(overlayWindow, 48);
            overlayWindow.Navigate(typeof(DesktopLyricsPage));
            overlayWindow.Activate();
            App.Current.DesktopLyricsWindow = overlayWindow;
        }

        private void CloseDesktopLyricsWindow()
        {
            var overlayWindow = App.Current.DesktopLyricsWindow!;
            AppBarHelper.Disable(overlayWindow);
            overlayWindow.Close();
            App.Current.DesktopLyricsWindow = null;
        }

        private void OpenInAppLyricsWindow()
        {
            var hostWindow = new HostWindow();
            WindowHelper.TrackWindow(hostWindow);
            hostWindow.Navigate(typeof(InAppLyricsPage));
            hostWindow.Activate();
            App.Current.InAppLyricsWindow = hostWindow;
        }

        private void CloseInAppLyricsWindow()
        {
            var hostWindow = App.Current.InAppLyricsWindow!;
            hostWindow.Close();
            App.Current.InAppLyricsWindow = null;
        }
    }
}
