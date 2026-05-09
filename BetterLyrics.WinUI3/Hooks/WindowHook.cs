// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Hooks
{
    public static class WindowHook
    {
        private static List<object> _activeWindows = [];
        private static List<object> _activeAppBars = [];

        public static void HideWindow(this Window window, WindowStatus hiddenBy = WindowStatus.HiddenByUser)
        {
            if (hiddenBy is WindowStatus.Closed or WindowStatus.Opened)
            {
                throw new ArgumentOutOfRangeException(nameof(hiddenBy));
            }

            if (window is NowPlayingWindow nowPlayingWindow)
            {
                if (nowPlayingWindow.LyricsWindowStatus.IsWorkArea && GetWindowHandle(window) is IntPtr hwnd)
                {
                    _activeAppBars.Remove(window);
                    UnregisterAppBar(hwnd);
                }
                nowPlayingWindow.LyricsWindowStatus.WindowStatus = hiddenBy;
            }
            window.Hide();
        }

        public static void PrepareWindowClosing(this Window window)
        {
            if (window is NowPlayingWindow nowPlayingWindow)
            {
                if (nowPlayingWindow.LyricsWindowStatus.IsWorkArea && GetWindowHandle(window) is IntPtr hwnd)
                {
                    _activeAppBars.Remove(window);
                    UnregisterAppBar(hwnd);
                }
                if (nowPlayingWindow.LyricsWindowStatus.IsWallpaper)
                {
                    // 先取消固定至桌面以防后续关闭该窗口时报错
                    WorkerWHook.UnpinFromDesktop(nowPlayingWindow);
                }
                nowPlayingWindow.LyricsWindowStatus.WindowStatus = WindowStatus.Closed;
            }
            _activeWindows.Remove(window);
        }
        
        public static void CloseWindow(this Window window)
        {
            window.PrepareWindowClosing();
            window.Close();
        }

        public static void MinimizeWindow(this Window window)
        {
            window.Minimize();
        }

        public static T? GetWindow<T>()
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

        public static NowPlayingWindow? GetNowPlayingWindow(LyricsWindowStatus status)
        {
            return (NowPlayingWindow?)GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == status);
        }

        public static List<T> GetWindows<T>()
        {
            var windows = new List<T>();
            foreach (var window in _activeWindows)
            {
                if (window is T castedWindow)
                {
                    windows.Add(castedWindow);
                }
            }
            return windows;
        }

        public static IntPtr? GetWindowHandle(object? obj)
        {
            if (obj is FrameworkElement frameworkElement)
            {
                return frameworkElement.XamlRoot.ContentIslandEnvironment.AppWindowId.GetWindowHandle();
            }
            else if (obj is Window window)
            {
                return WindowNative.GetWindowHandle(window);
            }
            else
            {
                return null;
            }
        }

        public static IntPtr? GetWindowHandle<T>()
        {
            return GetWindowHandle(GetWindow<T>());
        }

        public static T OpenOrShowWindow<T>(LyricsWindowStatus? status = null)
        {
            var window = _activeWindows.Find(w =>
                (typeof(T) != typeof(NowPlayingWindow) && w is T) ||
                (typeof(T) == typeof(NowPlayingWindow) && w is T && ((NowPlayingWindow)w).LyricsWindowStatus == status)
            );

            if (window == null)
            {
                if (typeof(T) == typeof(NowPlayingWindow))
                {
                    if (status == null)
                    {
                        throw new NullReferenceException(nameof(status));
                    }
                    window = new NowPlayingWindow(status);
                }
                else if (typeof(T) == typeof(SettingsWindow))
                {
                    window = new SettingsWindow();
                }
                else if (typeof(T) == typeof(MusicGalleryWindow))
                {
                    window = new MusicGalleryWindow();
                }
                else if (typeof(T) == typeof(LyricsSearchWindow))
                {
                    window = new LyricsSearchWindow();
                }
                else if (typeof(T) == typeof(LyricsWindowSwitchWindow))
                {
                    window = new LyricsWindowSwitchWindow();
                }
                else if (typeof(T) == typeof(LyricsShareWindow))
                {
                    window = new LyricsShareWindow();
                }
                else if (typeof(T) == typeof(StatsDashboardWindow))
                {
                    window = new StatsDashboardWindow();
                }
                else
                {
                    throw new ArgumentException("Unsupported window type", nameof(T));
                }

                TrackWindow(window);

                var castedWindow = (Window)window;

                if (typeof(T) != typeof(LyricsWindowSwitchWindow) && typeof(T) != typeof(NowPlayingWindow))
                {
                    castedWindow.Activate();
                }
            }
            else
            {
                if (typeof(T) == typeof(NowPlayingWindow))
                {
                    ((NowPlayingWindow)window).LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
                }

                var castedWindow = (Window)window;
                castedWindow.Activate();
                castedWindow.SetForegroundWindow();
            }

            return (T)window;
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
            EnsureAllWorkAreasReleased();
            Environment.Exit(0);
        }

        private static void EnsureAllWorkAreasReleased()
        {
            foreach (var item in _activeAppBars)
            {
                if (GetWindowHandle(item) is IntPtr hwnd)
                {
                    UnregisterAppBar(hwnd);
                }
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
            var window = (Window)sender;
            window.Closed -= WindowHelper_Closed;

            _activeWindows.Remove(sender);

            MemoryLeakDetector.Track(window);
            MemoryLeakDetector.ScheduleCheck(4000);
        }

        public static void SetIsAppBar(this NowPlayingWindow window, bool enable)
        {
            if (window == null) return;

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (enable)
            {
                RegisterAppBar(hwnd, window.LyricsWindowStatus);
            }
            else
            {
                UnregisterAppBar(hwnd);
            }
        }

        public static void SetIsClickThrough(this Window window, bool enable)
        {
            nint hwnd = window.GetWindowHandle();
            int style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

            if (enable)
            {
                style |= (int)(ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent);
            }
            else
            {
                style &= ~(int)(ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent);
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
        }

        public static void SetIsBorderless(this Window window, bool enable)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            int style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE);

            if (enable)
            {
                style &= ~(int)(User32.WindowStyles.WS_CAPTION | User32.WindowStyles.WS_THICKFRAME);
            }
            else
            {
                style |= (int)(User32.WindowStyles.WS_CAPTION | User32.WindowStyles.WS_THICKFRAME);
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE, style);
        }

        public static void SetIsChildWindow(this Window window, bool enable)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            int style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE);

            if (enable)
            {
                style &= ~unchecked((int)User32.WindowStyles.WS_POPUP);
                style |= (int)User32.WindowStyles.WS_CHILD;
            }
            else
            {
                style |= unchecked((int)User32.WindowStyles.WS_POPUP);
                style &= ~(int)User32.WindowStyles.WS_CHILD;
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE, style);
        }

        public static void SetIsAlwaysOnTop(this NowPlayingWindow window, bool enable)
        {
            if (window.AppWindow is AppWindow appWindow && appWindow.Presenter.Kind == AppWindowPresenterKind.Overlapped)
            {
                ((Window)window).SetIsAlwaysOnTop(enable);
            }
        }

        public static void MoveAndResize(this Window window, Rect rect)
        {
            if (window == null) return;
            if (window.AppWindow == null) return;

            window.AppWindow.Move(new Windows.Graphics.PointInt32((int)rect.X, (int)rect.Y));
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)rect.Width, (int)rect.Height));
        }

        /// <summary>
        /// 注册应用栏
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="status"></param>
        private static void RegisterAppBar(IntPtr hwnd, LyricsWindowStatus status)
        {
            if (_activeAppBars.Contains(hwnd)) return;

            var uEdge = status.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

            double top = status.DockPlacement == DockPlacement.Top ? status.MonitorBounds.Top : status.MonitorBounds.Bottom - status.DockHeight;
            double bottom = top + status.DockHeight;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = (int)status.MonitorBounds.Left,
                    Top = (int)top,
                    Right = (int)status.MonitorBounds.Right,
                    Bottom = (int)bottom,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_NEW, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

            _activeAppBars.Add(hwnd);
        }
        /// <summary>
        /// 取消注册应用栏
        /// </summary>
        /// <param name="hwnd"></param>
        private static void UnregisterAppBar(IntPtr hwnd)
        {
            if (!_activeAppBars.Contains(hwnd))
                return;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_REMOVE, ref abd);

            _activeAppBars.Remove(hwnd);
        }
        /// <summary>
        /// 更新应用栏
        /// </summary>
        /// <param name="window"></param>
        public static void UpdateAppBar(this NowPlayingWindow window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);

            if (!_activeAppBars.Contains(hwnd))
                return;

            var status = window.LyricsWindowStatus;

            var uEdge = status.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

            double top = status.DockPlacement == DockPlacement.Top ?
                status.MonitorBounds.Top :
                status.MonitorBounds.Bottom - status.DockHeight;

            double bottom = top + status.DockHeight;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = (int)status.MonitorBounds.Left,
                    Top = (int)top,
                    Right = (int)status.MonitorBounds.Right,
                    Bottom = (int)bottom,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);
        }

    }
}
