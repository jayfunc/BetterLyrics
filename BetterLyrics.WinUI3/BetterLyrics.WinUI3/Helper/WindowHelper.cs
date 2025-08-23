// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowHelper
    {
        private static List<object> _activeWindows = [];

        public static void CloseWindow<T>()
        {
            if (typeof(T) == typeof(LyricsWindow))
            {
                EnsureDockModeReleased();
            }
            var window = _activeWindows.Find(w => w is T);
            if (window is Window w)
            {
                w.Close();
                _activeWindows.Remove(w);
            }
        }

        public static T? GetWindowByWindowType<T>()
        {
            foreach (var window in _activeWindows)
            {
                if (window is T castedWindow)
                {
                    return castedWindow;
                }
            }
            return default;
        }
        public static void OpenWindow<T>()
        {
            var window = _activeWindows.Find(w => w is T);
            if (window == null)
            {
                if (typeof(T) == typeof(LyricsWindow))
                {
                    window = new LyricsWindow();
                    ((LyricsWindow)window).SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);
                }
                else if (typeof(T) == typeof(SettingsWindow))
                {
                    window = new SettingsWindow();
                }
                else if (typeof(T) == typeof(MusicGalleryWindow))
                {
                    window = new MusicGalleryWindow();
                }
                else
                {
                    throw new ArgumentException("Unsupported window type", nameof(T));
                }
                TrackWindow(window);
                var castedWindow = (Window)window;
                castedWindow.Restore();
                castedWindow.Activate();

                if (typeof(T) == typeof(LyricsWindow))
                {
                    var lyricsWindow = (LyricsWindow)window;
                    lyricsWindow.ViewModel.InitShortcuts();
                    lyricsWindow.AutoSelectLyricsMode();
                    lyricsWindow.ViewModel.SetIsAlwaysOnTop();
                }
            }
            else
            {
                var castedWindow = (Window)window;
                if (typeof(T) == typeof(LyricsWindow))
                {
                    var lyricsWindow = (LyricsWindow)window;
                    lyricsWindow.Show();
                }
                else
                {
                    castedWindow.Restore();
                    castedWindow.Activate();
                }
            }
        }

        public static void RestartApp(string args = "")
        {
            // The restart will be executed immediately.
            AppRestartFailureReason failureReason =
                Microsoft.Windows.AppLifecycle.AppInstance.Restart(args);

            // If the restart fails, handle it here.
            switch (failureReason)
            {
                case AppRestartFailureReason.RestartPending:
                    break;
                case AppRestartFailureReason.NotInForeground:
                    break;
                case AppRestartFailureReason.InvalidUser:
                    break;
                default: //AppRestartFailureReason.Other
                    break;
            }
        }

        public static void ExitApp()
        {
            EnsureDockModeReleased();
            Environment.Exit(0);
        }

        private static void EnsureDockModeReleased()
        {
            LyricsWindow? lyricsWindow = GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow != null)
            {
                DockModeHelper.Disable(lyricsWindow);
            }
        }

        private static void TrackWindow(object window)
        {
            if (!_activeWindows.Contains(window))
            {
                _activeWindows.Add(window);
                var castedWindow = (Window)window;
                castedWindow.Closed += WindowHelper_Closed;
            }
        }

        private static void WindowHelper_Closed(object sender, WindowEventArgs args)
        {
            if (_activeWindows.Contains(sender))
            {
                _activeWindows.Remove(sender);
            }
        }
    }
}
