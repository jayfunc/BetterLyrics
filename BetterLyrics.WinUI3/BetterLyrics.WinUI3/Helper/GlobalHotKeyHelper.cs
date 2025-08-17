using BetterLyrics.WinUI3.Enums;
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

        /// <summary>
        /// Register a global hotkey for a specific window type
        /// </summary>
        /// <typeparam name="T">Target window type</typeparam>
        /// <param name="id"></param>
        /// <param name="keys"></param>
        /// <param name="action"></param>
        public static void RegisterHotKey<T>(ShortcutID id, List<string> keys, Action action)
        {
            if (keys.Count == 0) return;

            var window = WindowHelper.GetWindowByWindowType<T>();
            if (window == null) return;

            HWND hwnd = WindowNative.GetWindowHandle(window);
            User32.HotKeyModifiers modifiers = User32.HotKeyModifiers.MOD_NONE;
            VirtualKey key = VirtualKey.None;
            foreach (var item in keys)
            {
                if (item == "Ctrl")
                {
                    modifiers |= User32.HotKeyModifiers.MOD_CONTROL;
                }
                else if (item == "Shift")
                {
                    modifiers |= User32.HotKeyModifiers.MOD_SHIFT;
                }
                else if (item == "Alt")
                {
                    modifiers |= User32.HotKeyModifiers.MOD_ALT;
                }
                else if (item == "Win")
                {
                    modifiers |= User32.HotKeyModifiers.MOD_WIN;
                }
                else
                {
                    key = (VirtualKey)Enum.Parse(typeof(VirtualKey), item, true);
                }
            }
            User32.RegisterHotKey(hwnd, (int)id, modifiers, (uint)key);
            _hotKeyActions[(int)id] = action;
        }

        public static void UnregisterHotKey<T>(ShortcutID id)
        {
            var window = WindowHelper.GetWindowByWindowType<T>();
            if (window == null) return;

            HWND hwnd = WindowNative.GetWindowHandle(window);
            User32.UnregisterHotKey(hwnd, (int)id);
            _hotKeyActions.Remove((int)id);
        }

        public static void UpdateHotKey<T>(ShortcutID id, List<string> keys, Action action)
        {
            UnregisterHotKey<T>(id);
            RegisterHotKey<T>(id, keys, action);
        }

        public static bool TryInvokeAction(ShortcutID id)
        {
            return TryInvokeAction((int)id);
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
