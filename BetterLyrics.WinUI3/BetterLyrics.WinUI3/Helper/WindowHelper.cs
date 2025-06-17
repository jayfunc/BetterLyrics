using System.Collections.Generic;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowHelper
    {
        public static void OpenSystemTrayWindow()
        {
            var window = new OverlayWindow(clickThrough: true);
            TrackWindow(window);
            window.Navigate(typeof(SystemTrayPage));
            window.Activate();
        }

        public static void OpenDesktopLyricsWindow()
        {
            var window = new OverlayWindow(listenOnActivatedWindowChange: true);
            TrackWindow(window);
            window.Navigate(typeof(DesktopLyricsPage));
            AppBarHelper.Enable(window, 48);
            window.Activate();
        }

        public static void OpenSettingsWindow()
        {
            var window = new HostWindow();
            TrackWindow(window);
            window.Navigate(typeof(SettingsPage));
            window.Activate();
        }

        public static void OpenInAppLyricsWindow()
        {
            var window = new HostWindow();
            TrackWindow(window);
            window.Navigate(typeof(InAppLyricsPage));
            window.Activate();
        }

        public static void TrackWindow(Window window)
        {
            window.Closed += (sender, args) =>
            {
                _activeWindows.Remove(window);
            };
            _activeWindows.Add(window);
        }

        public static Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null;
        }

        // get dpi for an element
        static public double GetRasterizationScaleForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return element.XamlRoot.RasterizationScale;
                    }
                }
            }
            return 0.0;
        }

        public static List<Window> ActiveWindows
        {
            get { return _activeWindows; }
        }

        private static List<Window> _activeWindows = new List<Window>();
    }
}
