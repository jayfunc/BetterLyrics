// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class WindowHelper
    {
        private static List<object> _activeWindows = [];
        private static List<object> _workAreas = [];

        private static readonly Dictionary<HWND, WindowStyle> _defaultWindowStyle = [];
        private static readonly Dictionary<HWND, ExtendedWindowStyle> _defaultExtendedWindowStyle = [];

        private static readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();
        private static readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();

        private static DispatcherQueueTimer? _setLyricsWindowVisibilityByPlayingStatusTimer;

        public static void HideWindow<T>()
        {
            var window = _activeWindows.Find(w => w is T);
            var castedWindow = window as Window;
            castedWindow?.Hide();
        }

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

        public static IntPtr? GetWindowHandle(object? obj)
        {
            if (obj is FrameworkElement frameworkElement)
            {
                return frameworkElement.XamlRoot.ContentIslandEnvironment.AppWindowId.GetWindowHandle();
            }
            return null;
        }

        public static void OpenOrShowWindow<T>()
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
                else if (typeof(T) == typeof(LyricsSearchWindow))
                {
                    window = new LyricsSearchWindow();
                }
                else if (typeof(T) == typeof(LyricsWindowSwitchWindow))
                {
                    window = new LyricsWindowSwitchWindow();
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
                    _liveStatesService.InitLyricsWindowStatus();

                    var hwnd = WindowNative.GetWindowHandle(castedWindow);
                    _defaultWindowStyle.Add(hwnd, castedWindow.GetWindowStyle());
                    _defaultExtendedWindowStyle.Add(hwnd, castedWindow.GetExtendedWindowStyle());

                    var lyricsWindow = (LyricsWindow)window;
                    lyricsWindow.ViewModel.InitShortcuts();
                    lyricsWindow.ViewModel.InitFgWindowWatcher();

                    _mediaSessionsService.InitPlaybackShortcuts();
                }
            }
            else
            {
                var castedWindow = (Window)window;
                castedWindow.Activate();
                castedWindow.AppWindow.MoveInZOrderAtTop();
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
            SetIsWorkArea<LyricsWindow>(false);
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

                var hwnd = WindowNative.GetWindowHandle(sender);
                _defaultWindowStyle.Remove(hwnd);
                _defaultExtendedWindowStyle.Remove(hwnd);
            }
        }

        public static void SetIsClickThrough<T>(bool enable)
        {
            Window? window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (enable)
            {
                window.SetExtendedWindowStyle(_defaultExtendedWindowStyle[hwnd] | ExtendedWindowStyle.Transparent | ExtendedWindowStyle.Layered);
            }
            else
            {
                window.SetExtendedWindowStyle(_defaultExtendedWindowStyle[hwnd]);
            }
        }

        public static void SetIsWorkArea<T>(bool enable)
        {
            Window? window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (enable)
            {
                RegisterWorkArea(hwnd);
            }
            else
            {
                UnregisterWorkArea(hwnd);
            }
        }

        public static void SetIsBorderless<T>(bool enable)
        {
            var window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (enable)
            {
                window.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            }
            else
            {
                window.SetWindowStyle(_defaultWindowStyle[hwnd]);
            }
        }

        public static void SetIsShowInSwitchers<T>(bool enable)
        {
            var window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            window.AppWindow.IsShownInSwitchers = enable;
        }

        public static void SetIsAlwaysOnTop<T>(bool enable)
        {
            var window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = enable;
            }
        }

        public static void MoveAndResize<T>(Rect rect)
        {
            var window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            window.AppWindow.Move(new Windows.Graphics.PointInt32((int)rect.X, (int)rect.Y));
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)rect.Width, (int)rect.Height));
        }

        public static void SetTitleBarArea<T>(TitleBarArea titleBarArea)
        {
            if (typeof(T) == typeof(LyricsWindow))
            {
                LyricsWindow? lyricsWindow = GetWindowByWindowType<LyricsWindow>();
                lyricsWindow?.SetTitleBarArea(titleBarArea);
            }
            else
            {
                throw new Exception($"Unsupported window type: {typeof(T).FullName}");
            }
        }

        private static void RegisterWorkArea(IntPtr hwnd)
        {
            if (_workAreas.Contains(hwnd)) return;

            var uEdge = _liveStatesService.LiveStates.LyricsWindowStatus.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

            double top = _liveStatesService.LiveStates.LyricsWindowStatus.DockPlacement == DockPlacement.Top ? _liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Top : _liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Bottom - _liveStatesService.LiveStates.LyricsWindowStatus.DockHeight;
            double bottom = top + _liveStatesService.LiveStates.LyricsWindowStatus.DockHeight;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = (int)_liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Left,
                    Top = (int)top,
                    Right = (int)_liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Right,
                    Bottom = (int)bottom,
                },
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_NEW, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
            Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);

            _workAreas.Add(hwnd);
        }

        private static void UnregisterWorkArea(IntPtr hwnd)
        {
            if (!_workAreas.Contains(hwnd))
                return;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd
            };

            Shell32.SHAppBarMessage(Shell32.ABM.ABM_REMOVE, ref abd);

            _workAreas.Remove(hwnd);
        }

        public static void UpdateWorkArea<T>()
        {
            var window = GetWindowByWindowType<T>() as Window;
            if (window == null) return;

            var hwnd = WindowNative.GetWindowHandle(window);

            if (!_workAreas.Contains(hwnd))
                return;

            var uEdge = _liveStatesService.LiveStates.LyricsWindowStatus.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

            double top = _liveStatesService.LiveStates.LyricsWindowStatus.DockPlacement == DockPlacement.Top ?
                _liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Top :
                _liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Bottom - _liveStatesService.LiveStates.LyricsWindowStatus.DockHeight;

            double bottom = top + _liveStatesService.LiveStates.LyricsWindowStatus.DockHeight;

            Shell32.APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = uEdge,
                rc = new RECT
                {
                    Left = (int)_liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Left,
                    Top = (int)top,
                    Right = (int)_liveStatesService.LiveStates.LyricsWindowStatus.MonitorBounds.Right,
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
        public static void SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue dispatcherQueue)
        {
            _setLyricsWindowVisibilityByPlayingStatusTimer ??= dispatcherQueue.CreateTimer();

            _setLyricsWindowVisibilityByPlayingStatusTimer.Debounce(() =>
            {
                var window = GetWindowByWindowType<LyricsWindow>();
                if (window == null) return;

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AutoShowOrHideWindow && !_mediaSessionsService.IsPlaying)
                {
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        SetIsWorkArea<LyricsWindow>(false);
                    }
                    HideWindow<LyricsWindow>();
                }
                else if (_liveStatesService.LiveStates.LyricsWindowStatus.AutoShowOrHideWindow && _mediaSessionsService.IsPlaying)
                {
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        SetIsWorkArea<LyricsWindow>(true);
                    }
                    OpenOrShowWindow<LyricsWindow>();
                }
            }, Constants.Time.DebounceTimeout);
        }

    }
}
