using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Helper
{
    public class GlobalHotKeyHelper
    {
        private static Dictionary<int, Action> _hotKeyActions = [];
        private static int _nextId = 0;

        public static void RegisterHotKey(Window window, User32.HotKeyModifiers modifiers, uint key, Action action)
        {
            HWND hwnd = WindowNative.GetWindowHandle(window);
            int id = _nextId++;
            User32.RegisterHotKey(hwnd, id, modifiers, key);
            _hotKeyActions[id] = action;
        }

        public static void UnregisterAllHotKeys(Window window)
        {
            HWND hwnd = WindowNative.GetWindowHandle(window);
            foreach (var id in _hotKeyActions.Keys.ToList())
            {
                User32.UnregisterHotKey(hwnd, id);
                _hotKeyActions.Remove(id);
            }
        }

        public static bool TryInvokeAction(int id)
        {
            if (_hotKeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }
    }
}
