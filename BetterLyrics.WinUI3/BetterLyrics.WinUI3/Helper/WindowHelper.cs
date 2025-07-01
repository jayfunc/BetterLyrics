// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
            var window = _activeWindows.Find(w => w is T);
            if (window is Window w)
            {
                w.Close();
                _activeWindows.Remove(w);
            }
        }

        public static void ExitAllWindows()
        {
            while (_activeWindows.Count > 0)
            {
                var window = _activeWindows[0];
                ((Window)window).Close();
                _activeWindows.Remove(window);
            }
            App.Current.Exit();
        }

        public static T GetWindowByWindowType<T>()
        {
            foreach (var window in _activeWindows)
            {
                if (window is T castedWindow)
                {
                    return castedWindow;
                }
            }
            throw new InvalidOperationException($"No window of type {typeof(T).Name} found.");
        }
        public static void OpenOrShowWindow<T>()
        {
            var window = _activeWindows.Find(w => w is T);
            if (window != null)
            {
                var castedWindow = (Window)window;
                castedWindow.Restore();
            }
            else
            {
                object newWindow;
                if (typeof(T) == typeof(LyricsWindow))
                {
                    newWindow = new LyricsWindow();
                }
                else if (typeof(T) == typeof(SettingsWindow))
                {
                    newWindow = new SettingsWindow();
                }
                else
                {
                    throw new ArgumentException("Unsupported window type", nameof(T));
                }
                ((Window)newWindow).Activate();
                TrackWindow(newWindow);
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

        private static void TrackWindow(object window)
        {
            if (!_activeWindows.Contains(window))
                _activeWindows.Add(window);
        }
    }
}
