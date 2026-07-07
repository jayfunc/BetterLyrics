using System;
using System.Collections.Generic;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Avalonia.Providers;

public class WindowManagerProvider : IWindowManagerProvider
{
    public void HideWindow(object obj, WindowStatus hiddenBy = WindowStatus.HiddenByUser)
    {
        throw new NotImplementedException();
    }

    public void PrepareWindowClosing(object obj)
    {
        throw new NotImplementedException();
    }

    public void CloseWindow(object obj)
    {
        throw new NotImplementedException();
    }

    public void MinimizeWindow(object obj)
    {
        throw new NotImplementedException();
    }

    public IntPtr? GetWindowHandle(object? obj)
    {
        throw new NotImplementedException();
    }

    public IntPtr? GetWindowHandle<T>()
    {
        throw new NotImplementedException();
    }

    public List<object> GetWindows(WindowType windowType)
    {
        throw new NotImplementedException();
    }

    public object? GetWindow(WindowType windowType, object? windowParameter = null)
    {
        throw new NotImplementedException();
    }

    public object? OpenOrShowWindow(WindowType windowType, object? windowParameter = null)
    {
        throw new NotImplementedException();
    }

    public T? GetWindow<T>()
    {
        throw new NotImplementedException();
    }

    public T OpenOrShowWindow<T>(LyricsWindowStatus? status = null)
    {
        throw new NotImplementedException();
    }

    public List<T> GetWindows<T>()
    {
        throw new NotImplementedException();
    }

    public void RestartApp(string args = "")
    {
        throw new NotImplementedException();
    }

    public void ExitApp()
    {
        throw new NotImplementedException();
    }

    public void SetIsClickThrough(object obj, bool enable)
    {
        throw new NotImplementedException();
    }

    public void SetIsBorderless(object obj, bool enable)
    {
        throw new NotImplementedException();
    }

    public void SetIsChildWindow(object obj, bool enable)
    {
        throw new NotImplementedException();
    }

    public void MoveAndResize(object obj, AppRect rect)
    {
        throw new NotImplementedException();
    }

    public object? GetNowPlayingWindow(LyricsWindowStatus status)
    {
        throw new NotImplementedException();
    }

    public void SetIsAppBar(object obj, bool enable)
    {
        throw new NotImplementedException();
    }

    public void SetIsAlwaysOnTop(object obj, bool enable)
    {
        throw new NotImplementedException();
    }

    public void UpdateAppBar(object obj)
    {
        throw new NotImplementedException();
    }
}