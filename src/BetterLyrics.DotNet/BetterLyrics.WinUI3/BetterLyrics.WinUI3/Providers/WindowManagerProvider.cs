// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.Core;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class WindowManagerProvider : IWindowManagerProvider
{
    private static readonly List<object> _activeWindows = [];
    private static readonly List<object> _activeAppBars = [];

    private readonly ILogger<WindowManagerProvider> _logger;

    public WindowManagerProvider(ILogger<WindowManagerProvider> logger)
    {
        _logger = logger;
    }

    public void HideWindow(object obj, LyricsWindowHideReason reason = LyricsWindowHideReason.UserHidden)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        if (reason == LyricsWindowHideReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (obj is NowPlayingWindow nowPlayingWindow)
        {
            if (nowPlayingWindow.LyricsWindowStatus.IsWorkArea && GetWindowHandle(obj) is IntPtr hwnd)
            {
                UnregisterAppBar(hwnd);
            }

            var status = nowPlayingWindow.LyricsWindowStatus;
            if (reason.HasFlag(LyricsWindowHideReason.UserHidden)) status.IsUserHidden = true;
            status.HideReasons = reason;
            status.WindowState = LyricsWindowState.Hidden;
        }

        var window = (Window)obj;

        _logger.LogDebug("Hiding window {WindowType}. Reason: {Reason}.", obj.GetType().Name, reason);
        window.Hide();
    }

    public void ToggleWindow(object obj)
    {
        if (obj is not Window window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        if (window.Visible && !IsWindowMinimized(window))
        {
            HideWindow(window);
        }
        else
        {
            if (window is NowPlayingWindow nowPlayingWindow)
            {
                OpenOrShowWindow(WindowType.NowPlayingWindow, nowPlayingWindow.LyricsWindowStatus);
            }
            else
            {
                RestoreWindowIfMinimized(window);
                window.Show();
                window.Activate();
                window.SetForegroundWindow();
            }
        }
    }

    public void ToggleWindows(WindowType windowType)
    {
        var windows = GetWindows(windowType);
        foreach (var window in windows)
        {
            ToggleWindow(window);
        }
    }

    public void PrepareWindowClosing(object obj)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        if (obj is NowPlayingWindow nowPlayingWindow)
        {
            if (nowPlayingWindow.LyricsWindowStatus.IsWorkArea && GetWindowHandle(obj) is IntPtr hwnd)
            {
                UnregisterAppBar(hwnd);
            }

            if (nowPlayingWindow.LyricsWindowStatus.IsWallpaper)
            {
                // 先取消固定至桌面以防后续关闭该窗口时报错
                WorkerWHook.UnpinFromDesktop(nowPlayingWindow);
            }

            var status = nowPlayingWindow.LyricsWindowStatus;

            // OnWindowClosed can be invoked twice for a programmatic close
            // (CloseWindow and the later Closed event). Only the first
            // transition may update the user-hidden override.
            if (status.WindowState != LyricsWindowState.Closed)
            {
                if (!status.IsClosingProgrammatically) status.IsUserHidden = true;
                status.WindowState = LyricsWindowState.Closed;
                status.HideReasons = LyricsWindowHideReason.None;
            }

            status.IsClosingProgrammatically = false;
        }

        _activeWindows.Remove(obj);
    }

    public void OnWindowClosed(object obj)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        PrepareWindowClosing(obj);
    }

    public void CloseWindow(object obj)
    {
        if (obj is NowPlayingWindow nowPlayingWindow)
        {
            // Distinguish an application-driven close (single-window switch,
            // multi-window mode change, config deletion) from a user close.
            nowPlayingWindow.LyricsWindowStatus.IsClosingProgrammatically = true;
        }

        OnWindowClosed(obj);

        var window = (Window)obj;
        window.Close();
    }

    public void MinimizeWindow(object obj)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        var window = (Window)obj;
        window.Minimize();
    }

    public object? GetWindow(WindowType windowType, object? windowParameter = null) => windowType switch
    {
        WindowType.LyricsShareWindow => GetWindow<LyricsShareWindow>(),
        WindowType.MusicGalleryWindow => GetWindow<MusicGalleryWindow>(),
        WindowType.LyricsSearchWindow => GetWindow<LyricsSearchWindow>(),
        WindowType.LyricsWindowSwitchWindow => GetWindow<LyricsWindowSwitchWindow>(),
        WindowType.StatsDashboardWindow => GetWindow<StatsDashboardWindow>(),
        WindowType.SettingsWindow => GetWindow<SettingsWindow>(),
        WindowType.NowPlayingWindow => windowParameter is LyricsWindowStatus lyricsWindowStatus
            ? GetNowPlayingWindow(lyricsWindowStatus)
            : GetWindow<NowPlayingWindow>(),
        _ => null
    };

    public T? GetWindow<T>()
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

    public object? GetNowPlayingWindow(LyricsWindowStatus status)
    {
        return GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == status);
    }

    public List<T> GetWindows<T>()
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

    public List<object> GetWindows(WindowType windowType) => windowType switch
    {
        WindowType.SettingsWindow => GetWindows<SettingsWindow>().Select(x => (object)x).ToList(),
        WindowType.NowPlayingWindow => GetWindows<NowPlayingWindow>().Select(x => (object)x).ToList(),
        WindowType.MusicGalleryWindow => GetWindows<MusicGalleryWindow>().Select(x => (object)x).ToList(),
        WindowType.LyricsShareWindow => GetWindows<LyricsShareWindow>().Select(x => (object)x).ToList(),
        WindowType.LyricsSearchWindow => GetWindows<LyricsSearchWindow>().Select(x => (object)x).ToList(),
        WindowType.LyricsWindowSwitchWindow => GetWindows<LyricsWindowSwitchWindow>().Select(x => (object)x).ToList(),
        WindowType.StatsDashboardWindow => GetWindows<StatsDashboardWindow>().Select(x => (object)x).ToList(),
        _ => []
    };

    public IntPtr? GetWindowHandle(object? obj)
    {
        if (obj is FrameworkElement frameworkElement)
        {
            return frameworkElement.XamlRoot.ContentIslandEnvironment.AppWindowId.GetWindowHandle();
        }

        if (obj is Window window) return WindowNative.GetWindowHandle(window);

        return null;
    }

    public IntPtr? GetWindowHandle<T>()
    {
        return GetWindowHandle(GetWindow<T>());
    }

    public IntPtr? GetWindowHandle(WindowType windowType)
    {
        return GetWindowHandle(GetWindow(windowType));
    }

    public bool IsWindowFocused(object? obj)
    {
        if (obj == null) return false;
        var hwnd = GetWindowHandle(obj);
        if (!hwnd.HasValue) return false;

        var foregroundHwnd = User32.GetForegroundWindow();
        if (foregroundHwnd.IsNull || foregroundHwnd == HWND.NULL) return false;

        var rootHwnd = User32.GetAncestor(foregroundHwnd, User32.GetAncestorFlag.GA_ROOT);
        var rootOwnerHwnd = User32.GetAncestor(foregroundHwnd, User32.GetAncestorFlag.GA_ROOTOWNER);

        var targetHwnd = (HWND)hwnd.Value;
        return targetHwnd == foregroundHwnd || targetHwnd == rootHwnd || targetHwnd == rootOwnerHwnd;
    }

    public bool IsWindowFocused(WindowType windowType)
    {
        var targetWindows = GetWindows(windowType);
        return targetWindows.Any(IsWindowFocused);
    }

    public bool IsWindowVisible(object? obj)
    {
        return obj is Window window && window.Visible;
    }

    public bool IsWindowMinimized(object? obj)
    {
        return obj is Window window && User32.IsIconic(WindowNative.GetWindowHandle(window));
    }

    public T OpenOrShowWindow<T>(LyricsWindowStatus? status = null)
    {
        var window = _activeWindows.Find(w =>
            (typeof(T) != typeof(NowPlayingWindow) && w is T) ||
            (typeof(T) == typeof(NowPlayingWindow) && w is T && ((NowPlayingWindow)w).LyricsWindowStatus == status)
        );

        if (window == null)
        {
            if (typeof(T) == typeof(NowPlayingWindow))
            {
                if (status == null) throw new NullReferenceException(nameof(status));

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
            else if (typeof(T) == typeof(SystemTrayWindow))
            {
                window = new SystemTrayWindow();
            }
            else
            {
                throw new ArgumentException("Unsupported window type", nameof(T));
            }

            TrackWindow(window);

            var castedWindow = (Window)window;
            _logger.LogInformation("Created window {WindowType}.", typeof(T).Name);

            // Not activate NowPlayingWindow to avoid window flashing
            if (typeof(T) != typeof(LyricsWindowSwitchWindow) && typeof(T) != typeof(NowPlayingWindow))
            {
                castedWindow.Activate();
            }
        }
        else
        {
            if (typeof(T) == typeof(NowPlayingWindow))
            {
                var lyricsStatus = ((NowPlayingWindow)window).LyricsWindowStatus;
                lyricsStatus.WindowState = LyricsWindowState.Opened;
                lyricsStatus.HideReasons = LyricsWindowHideReason.None;
                lyricsStatus.IsUserHidden = false;
            }
            else if (typeof(T) == typeof(MusicGalleryWindow))
            {
                var musicGalleryPageViewModel = Ioc.Default.GetService<MusicGalleryPageViewModel>();
                musicGalleryPageViewModel?.HandleAutoPlay();
            }

            _logger.LogDebug("Showing existing window {WindowType}.", typeof(T).Name);
            var castedWindow = (Window)window;

            // An explicit show request outranks the minimized state. Activate
            // alone does not always leave the minimized state, so restore first.
            RestoreWindowIfMinimized(castedWindow);

            castedWindow.Activate();
            castedWindow.SetForegroundWindow();
        }

        return (T)window;
    }

    public object? OpenOrShowWindow(WindowType windowType, object? windowParameter = null) => windowType switch
    {
        WindowType.SettingsWindow => OpenOrShowWindow<SettingsWindow>(),
        WindowType.NowPlayingWindow => windowParameter is LyricsWindowStatus lyricsWindowStatus
            ? OpenOrShowWindow<NowPlayingWindow>(lyricsWindowStatus)
            : OpenOrShowWindow<NowPlayingWindow>(),
        WindowType.MusicGalleryWindow => OpenOrShowWindow<MusicGalleryWindow>(),
        WindowType.LyricsShareWindow => OpenOrShowWindow<LyricsShareWindow>(),
        WindowType.LyricsWindowSwitchWindow => OpenOrShowWindow<LyricsWindowSwitchWindow>(),
        WindowType.LyricsSearchWindow => OpenOrShowWindow<LyricsSearchWindow>(),
        WindowType.StatsDashboardWindow => OpenOrShowWindow<StatsDashboardWindow>(),
        _ => null
    };

    public void RestartApp(string args = "")
    {
        // The restart will be executed immediately.
        _logger.LogInformation("Restarting application. Arguments: {Arguments}.", args);
        var failureReason = AppInstance.Restart(args);
        _logger.LogInformation("Application restart requested. Result: {FailureReason}.", failureReason);

        // If the restart fails, handle it here.
        switch (failureReason)
        {
            case AppRestartFailureReason.RestartPending:
                break;
            case AppRestartFailureReason.NotInForeground:
                break;
            case AppRestartFailureReason.InvalidUser:
                break;
        }
    }

    public void ExitApp()
    {
        _logger.LogInformation("Exiting application.");
        try
        {
            var smtcService = Ioc.Default.GetService<ISmtcService>();
            smtcService?.SaveCurrentPlaybackPosition();

            var playbackStateService = Ioc.Default.GetService<IPlaybackStateService>();
            playbackStateService?.TryWriteImmediately();

            var settingsService = Ioc.Default.GetService<ISettingsService>();
            settingsService?.TryWriteImmediately();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist application state during exit.");
        }

        EnsureAllWorkAreasReleased();

        // 退出前释放 Ioc 容器，确保 IDisposable 单例（数据库、文件监视器、快捷键等）被正确清理
        Program.DisposeServices();

        Environment.Exit(0);
    }

    public void SetIsAppBar(object obj, bool enable)
    {
        if (obj == null) return;

        var hwnd = WindowNative.GetWindowHandle(obj);

        if (enable)
        {
            RegisterAppBar(hwnd, ((NowPlayingWindow)obj).LyricsWindowStatus);
        }
        else
        {
            UnregisterAppBar(hwnd);
        }
    }

    public void SetIsClickThrough(object obj, bool enable)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        var window = (Window)obj;

        var hwnd = window.GetWindowHandle();
        var style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

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

    public void SetIsBorderless(object obj, bool enable)
    {
        var hwnd = WindowNative.GetWindowHandle(obj);
        var style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE);

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

    public void SetIsChildWindow(object obj, bool enable)
    {
        var hwnd = WindowNative.GetWindowHandle(obj);
        var style = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_STYLE);

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

    public void SetIsAlwaysOnTop(object obj, bool enable)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        var window = (Window)obj;

        if (window.AppWindow is AppWindow appWindow && appWindow.Presenter.Kind == AppWindowPresenterKind.Overlapped)
        {
            window.SetIsAlwaysOnTop(enable);
        }
    }

    public void SetIsShownInSwitchers(object obj, bool enable)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        var window = (Window)obj;

        if (window.AppWindow == null) return;

        window.AppWindow.IsShownInSwitchers = enable;
    }

    public void MoveAndResize(object obj, AppRect rect)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        if (obj == null) return;

        var window = (Window)obj;

        if (window.AppWindow == null) return;

        window.AppWindow.Move(new PointInt32((int)rect.X, (int)rect.Y));
        window.AppWindow.Resize(new SizeInt32((int)rect.Width, (int)rect.Height));
    }

    /// <summary>
    /// 更新应用栏
    /// </summary>
    /// <param name="obj"></param>
    public void UpdateAppBar(object obj)
    {
        if (obj is not Window)
        {
            throw new ArgumentException($"Expected a {nameof(Window)} instance, but received {obj?.GetType().Name ?? "null"}.", nameof(obj));
        }

        var hwnd = WindowNative.GetWindowHandle(obj);

        if (!_activeAppBars.Contains(hwnd))
        {
            return;
        }

        var status = ((NowPlayingWindow)obj).LyricsWindowStatus;

        var uEdge = status.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

        var top = status.DockPlacement == DockPlacement.Top
            ? status.MonitorBounds.Top
            : status.MonitorBounds.Bottom - status.DockHeight;

        var bottom = top + status.DockHeight;

        Shell32.APPBARDATA abd = new()
        {
            cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
            hWnd = hwnd,
            uCallbackMessage = Message.WM_APPBAR_CALLBACK,
            uEdge = uEdge,
            rc = new RECT
            {
                Left = (int)status.MonitorBounds.Left,
                Top = (int)top,
                Right = (int)status.MonitorBounds.Right,
                Bottom = (int)bottom
            }
        };

        Shell32.SHAppBarMessage(Shell32.ABM.ABM_QUERYPOS, ref abd);
        Shell32.SHAppBarMessage(Shell32.ABM.ABM_SETPOS, ref abd);
    }

    public void SetTaskbarProgressState(WindowType windowType, bool isPlaying)
    {
        if (GetWindowHandle(windowType) is IntPtr hwnd)
        {
            TaskbarList.SetProgressState(hwnd, isPlaying ? TaskbarButtonProgressState.Normal : TaskbarButtonProgressState.Paused);
        }
    }

    public void SetTaskbarProgressValue(WindowType windowType, double percentage)
    {
        if (GetWindowHandle(windowType) is IntPtr hwnd)
        {
            TaskbarList.SetProgressValue(hwnd, (ulong)(percentage * 100), 100);
        }
    }

    // private

    /// <summary>
    /// 注册应用栏
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="status"></param>
    private static void RegisterAppBar(IntPtr hwnd, LyricsWindowStatus status)
    {
        if (_activeAppBars.Contains(hwnd)) return;

        var uEdge = status.DockPlacement == DockPlacement.Top ? Shell32.ABE.ABE_TOP : Shell32.ABE.ABE_BOTTOM;

        var top = status.DockPlacement == DockPlacement.Top
            ? status.MonitorBounds.Top
            : status.MonitorBounds.Bottom - status.DockHeight;
        var bottom = top + status.DockHeight;

        Shell32.APPBARDATA abd = new()
        {
            cbSize = (uint)Marshal.SizeOf<Shell32.APPBARDATA>(),
            hWnd = hwnd,
            uCallbackMessage = Message.WM_APPBAR_CALLBACK,
            uEdge = uEdge,
            rc = new RECT
            {
                Left = (int)status.MonitorBounds.Left,
                Top = (int)top,
                Right = (int)status.MonitorBounds.Right,
                Bottom = (int)bottom
            }
        };

        var result = Shell32.SHAppBarMessage(Shell32.ABM.ABM_NEW, ref abd);
        if (result != IntPtr.Zero) Debug.WriteLine("AppBar has been registered successfully.");

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
            hWnd = hwnd,
            uCallbackMessage = Message.WM_APPBAR_CALLBACK
        };

        Shell32.SHAppBarMessage(Shell32.ABM.ABM_REMOVE, ref abd);

        _activeAppBars.Remove(hwnd);
    }

    private void EnsureAllWorkAreasReleased()
    {
        foreach (var item in _activeAppBars)
        {
            if (GetWindowHandle(item) is IntPtr hwnd)
            {
                UnregisterAppBar(hwnd);
            }
        }
    }

    private static void RestoreWindowIfMinimized(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        if (!User32.IsIconic(hwnd)) return;

        if (window.AppWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }
        else
        {
            User32.ShowWindow(hwnd, ShowWindowCommand.SW_RESTORE);
        }
    }

    private static void TrackWindow(object window)
    {
        if (!_activeWindows.Contains(window))
        {
            _activeWindows.Add(window);
        }
    }
}