// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="WindowHelper" />
    /// </summary>
    public static class WindowHelper
    {
        #region Fields

        /// <summary>
        /// Defines the _windowCache
        /// </summary>
        private static readonly Dictionary<Type, Window> _windowCache = new();

        /// <summary>
        /// Defines the _activeWindows
        /// </summary>
        private static List<Window> _activeWindows = new List<Window>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ActiveWindows
        /// </summary>
        public static List<Window> ActiveWindows
        {
            get { return _activeWindows; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The GetWindowByFramePageType
        /// </summary>
        /// <param name="type">The type<see cref="Type"/></param>
        /// <returns>The <see cref="Window"/></returns>
        public static Window GetWindowByFramePageType(Type type)
        {
            foreach (var cachedWindow in _windowCache)
            {
                if (cachedWindow.Key == type)
                {
                    return cachedWindow.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// The GetWindowForElement
        /// </summary>
        /// <param name="element">The element<see cref="UIElement"/></param>
        /// <returns>The <see cref="Window"/></returns>
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

        /// <summary>
        /// The HideSystemTitleBar
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        public static void HideSystemTitleBar(this Window window)
        {
            window.ExtendsContentIntoTitleBar = true;
            window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        }

        /// <summary>
        /// The HideSystemTitleBarAndSetCustomTitleBar
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        /// <param name="titleBar">The titleBar<see cref="UIElement"/></param>
        public static void HideSystemTitleBarAndSetCustomTitleBar(
            this Window window,
            UIElement titleBar
        )
        {
            window.HideSystemTitleBar();
            window.SetTitleBar(titleBar);
        }

        /// <summary>
        /// The OpenLyricsWindow
        /// </summary>
        public static void OpenLyricsWindow()
        {
            OpenOrShowWindow(typeof(LyricsPage));
        }

        /// <summary>
        /// The OpenSettingsWindow
        /// </summary>
        public static void OpenSettingsWindow()
        {
            OpenOrShowWindow(typeof(SettingsPage));
        }

        /// <summary>
        /// The TrackWindow
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        /// <param name="pageType">The pageType<see cref="Type"/></param>
        public static void TrackWindow(Window window, Type pageType = null)
        {
            if (pageType != null)
            {
                _windowCache[pageType] = window;
            }

            if (!_activeWindows.Contains(window))
                _activeWindows.Add(window);

            window.Closed -= Window_Closed;
            window.Closed += Window_Closed;
        }

        private static void Window_Closed(object sender, WindowEventArgs e)
        {
            if (sender is Window closedWindow)
            {
                _activeWindows.Remove(closedWindow);

                // 从缓存移除
                foreach (var kvp in _windowCache)
                {
                    if (kvp.Value == closedWindow)
                    {
                        _windowCache.Remove(kvp.Key);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The TryHide
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        public static void TryHide(this Window window)
        {
            if (window is not null)
            {
                window.Hide();
            }
        }

        /// <summary>
        /// The TryShow
        /// </summary>
        /// <param name="window">The window<see cref="Window"/></param>
        public static void TryShow(this Window window)
        {
            if (window is not null)
            {
                window.Activate();
            }
        }

        /// <summary>
        /// The OpenOrShowWindow
        /// </summary>
        /// <param name="pageType">The pageType<see cref="Type"/></param>
        private static void OpenOrShowWindow(Type pageType)
        {
            if (_windowCache.TryGetValue(pageType, out var window))
            {
                window.TryShow();
            }
            else
            {
                var newWindow = new HostWindow();
                TrackWindow(newWindow, pageType);
                newWindow.ViewModel.FramePageType = pageType;
                newWindow.Navigate(pageType);
                newWindow.Activate();
            }
        }

        // get dpi for an element

        /// <summary>
        /// The GetRasterizationScaleForElement
        /// </summary>
        /// <param name="element">The element<see cref="UIElement"/></param>
        /// <returns>The <see cref="double"/></returns>
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

        #endregion
    }
}
