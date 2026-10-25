using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;
using Windows.System;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public partial class GlobalShortcutProvider : IGlobalShortcutProvider, IDisposable
{
    private readonly HWND _handle;
    private readonly User32.WindowProc _wndProc;
    private readonly string _className = "BetterLyricsGlobalShortcutWatcherClass";
    private bool _isDisposed;
    private readonly Dictionary<int, Action> _actions = [];
    private readonly Dictionary<int, List<string>> _keys = [];
    private readonly object _lock = new();

    public GlobalShortcutProvider()
    {
        _wndProc = CustomWndProc;
        var wndClass = new User32.WNDCLASS
        {
            lpszClassName = _className,
            lpfnWndProc = _wndProc
        };
        User32.RegisterClass(wndClass);
        // 创建专用于接收 WM_HOTKEY 广播的独立顶层原生窗口
        _handle = User32.CreateWindowEx(
            User32.WindowStylesEx.WS_EX_LEFT,
            _className,
            "Global Shortcut Watcher Virtual Window - BetterLyrics",
            User32.WindowStyles.WS_OVERLAPPED,
            0, 0, 0, 0,
            HWND.NULL,
            HMENU.NULL,
            HINSTANCE.NULL,
            IntPtr.Zero);
    }
    ~GlobalShortcutProvider()
    {
        Dispose(false);
    }

    private IntPtr CustomWndProc(HWND hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == (uint)User32.WindowMessage.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            Action? action = null;
            lock (_lock)
            {
                _actions.TryGetValue(id, out action);
            }
            action?.Invoke();
        }
        return User32.DefWindowProc(hWnd, msg, wParam, lParam);
    }
    public bool RegisterHotKey(int id, List<string> keys, Action action)
    {
        if (keys == null || keys.Count == 0 || _handle.IsNull || _handle == HWND.NULL)
        {
            return false;
        }
        var modifiers = User32.HotKeyModifiers.MOD_NONE;
        var key = VirtualKey.None;
        foreach (var item in keys)
        {
            if (item.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= User32.HotKeyModifiers.MOD_CONTROL;
            }
            else if (item.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= User32.HotKeyModifiers.MOD_SHIFT;
            }
            else if (item.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= User32.HotKeyModifiers.MOD_ALT;
            }
            else if (item.Equals("Win", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= User32.HotKeyModifiers.MOD_WIN;
            }
            else
            {
                if (Enum.TryParse<VirtualKey>(item, true, out var parsedKey))
                {
                    key = parsedKey;
                }
            }
        }
        if (key == VirtualKey.None) return false;
        var success = User32.RegisterHotKey(_handle, id, modifiers, (uint)key);
        if (success)
        {
            lock (_lock)
            {
                _actions[id] = action;
                _keys[id] = keys;
            }
            return true;
        }
        return false;
    }
    public void UnregisterHotKey(int id)
    {
        if (_handle.IsNull || _handle == HWND.NULL) return;
        User32.UnregisterHotKey(_handle, id);
        lock (_lock)
        {
            _actions.Remove(id);
            _keys.Remove(id);
        }
    }
    public void UpdateHotKey(int id, List<string> keys, Action action)
    {
        UnregisterHotKey(id);
        RegisterHotKey(id, keys, action);
    }
    public bool IsHotKeyRegistered(List<string> keys)
    {
        lock (_lock)
        {
            return _keys.Values.Any(registeredKeys => registeredKeys.SequenceEqual(keys, StringComparer.OrdinalIgnoreCase));
        }
    }
    public bool IsHotKeyRegistered(int id)
    {
        lock (_lock)
        {
            return _actions.ContainsKey(id);
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (_handle != HWND.NULL)
            {
                lock (_lock)
                {
                    foreach (var id in _actions.Keys)
                    {
                        User32.UnregisterHotKey(_handle, id);
                    }
                    _actions.Clear();
                    _keys.Clear();
                }
                User32.DestroyWindow(_handle);
            }
            User32.UnregisterClass(_className, HINSTANCE.NULL);
            _isDisposed = true;
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
