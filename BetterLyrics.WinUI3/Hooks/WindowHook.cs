// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
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

        private static WindowStyle? _defaultWindowStyle;
        private static ExtendedWindowStyle? _defaultExtendedWindowStyle;

        public static void HideWindow(this Window window, bool hiddenByUser = true)
        {
            if (window is NowPlayingWindow nowPlayingWindow)
            {
                if (nowPlayingWindow.LyricsWindowStatus.IsWorkArea && GetWindowHandle(window) is IntPtr hwnd)
                {
                    _activeAppBars.Remove(window);
                    UnregisterAppBar(hwnd);
                }
                nowPlayingWindow.LyricsWindowStatus.WindowStatus = hiddenByUser ? WindowStatus.HiddenByUser : WindowStatus.HiddenBySystem;
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
                    WorkerWHook.UnpinFromDesktop(window);
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
                else if (typeof(T) == typeof(SystemTrayWindow))
                {
                    window = new SystemTrayWindow();
                }
                else if (typeof(T) == typeof(SplashWindow))
                {
                    window = new SplashWindow();
                }
                else if (typeof(T) == typeof(LyricsShareWindow))
                {
                    window = new LyricsShareWindow();
                }
                else
                {
                    throw new ArgumentException("Unsupported window type", nameof(T));
                }

                TrackWindow(window);
                var castedWindow = (Window)window;

                castedWindow.Restore();
                castedWindow.Activate();
                castedWindow.AppWindow.MoveInZOrderAtTop();

                if (typeof(T) == typeof(SystemTrayWindow))
                {
                    _defaultWindowStyle = castedWindow.GetWindowStyle();
                    _defaultExtendedWindowStyle = castedWindow.GetExtendedWindowStyle();
                    castedWindow.HideWindow();
                }

                if (typeof(T) == typeof(NowPlayingWindow))
                {
                    var lyricsWindow = (NowPlayingWindow)window;
                    lyricsWindow.InitStatus();
                    lyricsWindow.InitFgWindowWatcher();
                }
            }
            else
            {
                var castedWindow = (Window)window;
                castedWindow.Activate();
            }

            if (typeof(T) == typeof(NowPlayingWindow))
            {
                ((NowPlayingWindow)window).LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
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

        public static void SetIsWorkArea(this NowPlayingWindow window, bool enable)
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

        public static void SetIsLocked(this Window window, bool enable, bool removeBorder)
        {
            if (removeBorder)
            {
                SetIsBorderless(window, enable);
            }
            SetIsClickThrough(window, enable);
        }

        public static void SetIsClickThrough(this Window window, bool enable)
        {
            if (_defaultExtendedWindowStyle is ExtendedWindowStyle style)
            {
                if (enable)
                {
                    window.SetExtendedWindowStyle(style | ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent);
                }
                else
                {
                    window.SetExtendedWindowStyle(style);
                }
            }
        }

        public static void SetIsBorderless(this Window window, bool enable)
        {
            if (_defaultWindowStyle is WindowStyle style)
            {
                if (enable)
                {
                    window.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
                }
                else
                {
                    window.SetWindowStyle(style);
                }
            }
        }

        public static bool SetIsFullscreen(this Window window, bool enable, bool defaultExtendsContentIntoTitleBar = true)
        {
            if (window.AppWindow == null) return false;

            if (enable)
            {
                window.ExtendsContentIntoTitleBar = false;
                window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
            else
            {
                window.ExtendsContentIntoTitleBar = defaultExtendsContentIntoTitleBar;
                window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            }

            return true;
        }

        public static bool SetIsMaximized(this Window window, bool enable)
        {
            if (window.AppWindow == null) return false;

            if (enable)
            {
                window.Maximize();
            }
            else
            {
                window.Restore();
            }

            return true;
        }

        public static void SetIsShowInSwitchers(this Window window, bool enable)
        {
            if (window.AppWindow == null) return;

            window.AppWindow.IsShownInSwitchers = enable;
        }

        public static void SetIsAlwaysOnTop(this Window window, bool enable)
        {
            if (window.AppWindow == null) return;

            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = enable;
            }
        }

        public static void MoveAndResize(this Window window, Rect rect)
        {
            if (window.AppWindow == null) return;

            window.AppWindow.Move(new Windows.Graphics.PointInt32((int)rect.X, (int)rect.Y));
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)rect.Width, (int)rect.Height));
        }

        public static void SetTitleBarArea(this NowPlayingWindow window, TitleBarArea titleBarArea)
        {
            window.SetTitleBarArea(titleBarArea);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dispatcherQueue">请确保此参数指向同一个对象，建议传值 BaseViewModel._dispatcherQueue</param>
        public static void SetLyricsWindowVisibilityByPlayingStatus(this NowPlayingWindow window, bool isPlaying, DispatcherQueue dispatcherQueue)
        {
            var status = window.LyricsWindowStatus;

            status.VisibilityTimer ??= dispatcherQueue.CreateTimer();
            status.VisibilityTimer?.Debounce(() =>
            {
                if (status.AutoShowOrHideWindow && status.WindowStatus is WindowStatus.Opened or WindowStatus.HiddenBySystem)
                {
                    if (isPlaying)
                    {
                        OpenOrShowWindow<NowPlayingWindow>(status);
                        if (status.IsWorkArea)
                        {
                            window.SetIsWorkArea(true);
                        }
                        if (status.IsWorkArea)
                        {
                            window.MoveAndResize(status.GetWindowBoundsWhenWorkArea());
                        }
                    }
                    else
                    {
                        window.HideWindow(false);
                    }
                }
            }, TimeSpan.FromMilliseconds(window.LyricsWindowStatus.AutoShowOrHideWindowDelay));
        }

    }
}
