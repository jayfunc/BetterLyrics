using System;
using System.Collections.Generic;
using Windows.System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Hooks;

public class GlobalHotKeyHook
{
    private static readonly Dictionary<int, Action> _actions = [];
    private static readonly Dictionary<int, List<string>> _keys = [];

    /// <summary>
    ///     Register a global hotkey for a specific window type
    /// </summary>
    /// <typeparam name="T">Target window type</typeparam>
    /// <param name="id"></param>
    /// <param name="keys"></param>
    /// <param name="action"></param>
    private static void RegisterHotKey(Window window, ShortcutId id, List<string> keys, Action action)
    {
        if (keys.Count == 0) return;

        HWND hwnd = WindowNative.GetWindowHandle(window);
        var modifiers = User32.HotKeyModifiers.MOD_NONE;
        var key = VirtualKey.None;
        foreach (var item in keys)
            if (item == "Ctrl")
                modifiers |= User32.HotKeyModifiers.MOD_CONTROL;
            else if (item == "Shift")
                modifiers |= User32.HotKeyModifiers.MOD_SHIFT;
            else if (item == "Alt")
                modifiers |= User32.HotKeyModifiers.MOD_ALT;
            else if (item == "Win")
                modifiers |= User32.HotKeyModifiers.MOD_WIN;
            else
                key = (VirtualKey)Enum.Parse(typeof(VirtualKey), item, true);

        var success = User32.RegisterHotKey(hwnd, (int)id, modifiers, (uint)key);
        if (success)
        {
            _actions[(int)id] = action;
            _keys[(int)id] = keys;
        }
    }

    private static void UnregisterHotKey(Window window, ShortcutId id)
    {
        HWND hwnd = WindowNative.GetWindowHandle(window);
        User32.UnregisterHotKey(hwnd, (int)id);
        _actions.Remove((int)id);
        _keys.Remove((int)id);
    }

    public static void UpdateHotKey(Window window, ShortcutId id, List<string> keys, Action action)
    {
        UnregisterHotKey(window, id);
        RegisterHotKey(window, id, keys, action);
    }

    public static bool IsHotKeyRegistered(ShortcutId id)
    {
        return _actions.ContainsKey((int)id);
    }

    public static bool IsHotKeyRegistered(List<string> keys)
    {
        return _keys.ContainsValue(keys);
    }

    public static bool TryInvokeAction(ShortcutId id)
    {
        return TryInvokeAction((int)id);
    }

    public static bool TryInvokeAction(int id)
    {
        if (_actions.TryGetValue(id, out var action))
        {
            action?.Invoke();
            return true;
        }

        return false;
    }
}