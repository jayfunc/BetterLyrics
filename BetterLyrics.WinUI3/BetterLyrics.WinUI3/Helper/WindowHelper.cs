using System;
using System.Collections.Generic;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowHelper
    {
        private static readonly Dictionary<Type, Window> _windowCache = new();

        public static void HideSystemTitleBar(this Window window)
        {
            window.ExtendsContentIntoTitleBar = true;
            window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        }

        public static void HideSystemTitleBarAndSetCustomTitleBar(
            this Window window,
            UIElement titleBar
        )
        {
            window.HideSystemTitleBar();
            window.SetTitleBar(titleBar);
        }

        public static void OpenSettingsWindow()
        {
            OpenOrShowWindow(typeof(SettingsPage));
        }

        public static void OpenLyricsWindow()
        {
            OpenOrShowWindow(typeof(LyricsPage));
        }

        private static void OpenOrShowWindow(Type pageType)
        {
            if (_windowCache.TryGetValue(pageType, out var window))
            {
                if (window is HostWindow hostWindow)
                {
                    hostWindow.Navigate(pageType);
                }
                window.TryShow();
            }
            else
            {
                var newWindow = new HostWindow();
                TrackWindow(newWindow, pageType);
                newWindow.Navigate(pageType);
                newWindow.Activate();
            }
        }

        public static void TrackWindow(Window window, Type pageType = null)
        {
            if (pageType != null)
            {
                _windowCache[pageType] = window;
            }

            if (!_activeWindows.Contains(window))
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

        public static void TryShow(this Window window)
        {
            if (window is not null)
            {
                window.Activate();
            }
        }

        public static void TryHide(this Window window)
        {
            if (window is not null)
            {
                window.Hide();
            }
        }
    }
}
