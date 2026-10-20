using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface IWindowManagerProvider
{
    // For general windows

    void HideWindow(object obj, LyricsWindowHideReason reason = LyricsWindowHideReason.UserHidden);
    void ToggleWindow(object obj);
    void ToggleWindows(WindowType windowType);

    void PrepareWindowClosing(object obj);
    void OnWindowClosed(object obj);
    void CloseWindow(object obj);
    void MinimizeWindow(object obj);

    IntPtr? GetWindowHandle(object? obj);
    IntPtr? GetWindowHandle<T>();
    IntPtr? GetWindowHandle(WindowType windowType);

    List<object> GetWindows(WindowType windowType);
    object? GetWindow(WindowType windowType, object? windowParameter = null);
    object? OpenOrShowWindow(WindowType windowType, object? windowParameter = null);

    T? GetWindow<T>();
    T OpenOrShowWindow<T>(LyricsWindowStatus? status = null);

    List<T> GetWindows<T>();

    bool IsWindowFocused(object? obj);
    bool IsWindowFocused(WindowType windowType);
    bool IsWindowVisible(object? obj);

    void RestartApp(string args = "");
    void ExitApp();

    void SetIsClickThrough(object obj, bool enable);
    void SetIsBorderless(object obj, bool enable);
    void SetIsChildWindow(object obj, bool enable);
    void MoveAndResize(object obj, AppRect rect);

    void SetIsAlwaysOnTop(object obj, bool enable);
    void SetIsShownInSwitchers(object obj, bool enable);

    void SetIsAppBar(object obj, bool enable);
    void UpdateAppBar(object obj);

    void SetTaskbarProgressState(WindowType windowType, bool isPlaying);
    void SetTaskbarProgressValue(WindowType windowType, double percentage);

    // For NowPlayingWindow
    object? GetNowPlayingWindow(LyricsWindowStatus status);
}