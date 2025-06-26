using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;
using static System.Net.WebRequestMethods;

namespace BetterLyrics.WinUI3.Helper
{
    public static class DesktopModeHelper
    {
        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyles = [];
        private static readonly Dictionary<IntPtr, bool> _clickThroughStates = [];
        private static readonly Dictionary<IntPtr, bool> _originalTopmostStates = [];
        private static readonly Dictionary<IntPtr, nint> _oldWndProcs = [];
        private static readonly Dictionary<IntPtr, WndProcDelegate> _wndProcDelegates = [];

        // 子类化相关
        private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTTRANSPARENT = -1;
        private const int GWL_WNDPROC = -4;

        public static void Enable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 记忆原样式和透明度
            if (!_originalWindowStyles.ContainsKey(hwnd))
                _originalWindowStyles[hwnd] = window.GetWindowStyle();

            // 记忆原TopMost状态
            if (!_originalTopmostStates.ContainsKey(hwnd))
                _originalTopmostStates[hwnd] = IsWindowTopMost(hwnd);

            // 设置无边框、透明
            window.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            window.ExtendsContentIntoTitleBar = false;

            // 设置窗口置顶
            SetWindowTopMost(hwnd, true);

            // 设置全局穿透
            SetClickThrough(window, true);

            // 启用局部穿透
            EnablePartialClickThrough(window);
        }

        public static void Disable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 恢复样式和透明度
            if (_originalWindowStyles.TryGetValue(hwnd, out var style))
            {
                window.SetWindowStyle(style);
                _originalWindowStyles.Remove(hwnd);
            }

            window.ExtendsContentIntoTitleBar = true;

            // 恢复TopMost状态
            if (_originalTopmostStates.TryGetValue(hwnd, out var wasTopMost))
            {
                SetWindowTopMost(hwnd, wasTopMost);
                _originalTopmostStates.Remove(hwnd);
            }

            // 关闭点击穿透
            SetClickThrough(window, false);

            // 关闭局部穿透
            DisablePartialClickThrough(window);
        }

        /// <summary>
        /// 设置窗口是否置顶
        /// </summary>
        private static void SetWindowTopMost(IntPtr hwnd, bool topmost)
        {
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;
            const int SWP_NOACTIVATE = 0x0010;
            IntPtr hWndInsertAfter = topmost ? (IntPtr)(-1) : (IntPtr)(1); // HWND_TOPMOST / HWND_NOTOPMOST

            SetWindowPos(
                hwnd,
                hWndInsertAfter,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
            );
        }

        /// <summary>
        /// 判断窗口是否为置顶
        /// </summary>
        private static bool IsWindowTopMost(IntPtr hwnd)
        {
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            return (exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;
        }

        /// <summary>
        /// 切换点击穿透状态
        /// </summary>
        public static void SetClickThrough(Window window, bool enable)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enable)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
                _clickThroughStates[hwnd] = true;
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
                _clickThroughStates[hwnd] = false;
            }
        }

        /// <summary>
        /// 获取当前窗口是否为点击穿透状态
        /// </summary>
        public static bool GetClickThrough(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            return _clickThroughStates.TryGetValue(hwnd, out var state) && state;
        }

        /// <summary>
        /// 启用局部穿透
        /// </summary>
        public static void EnablePartialClickThrough(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (_oldWndProcs.ContainsKey(hwnd))
                return; // 已经子类化

            WndProcDelegate newWndProc = (hWnd, msg, wParam, lParam) =>
            {
                if (msg == WM_NCHITTEST)
                {
                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                    // 屏幕坐标转窗口坐标
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                        Win32Interop.GetWindowIdFromWindow(hwnd)
                    );
                    var windowPos = appWindow.Position;
                    int localX = x - windowPos.X;
                    int localY = y - windowPos.Y;

                    if (IsInInteractiveRegion(window, localX, localY))
                        return HTCLIENT;
                    else
                        return HTTRANSPARENT;
                }
                return CallWindowProc(_oldWndProcs[hwnd], hWnd, msg, wParam, lParam);
            };

            nint oldWndProc = SetWindowLongPtr(
                hwnd,
                GWL_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(newWndProc)
            );
            _oldWndProcs[hwnd] = oldWndProc;
            _wndProcDelegates[hwnd] = newWndProc; // 防止GC
        }

        /// <summary>
        /// 关闭局部穿透
        /// </summary>
        public static void DisablePartialClickThrough(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            if (_oldWndProcs.TryGetValue(hwnd, out var oldWndProc))
            {
                SetWindowLongPtr(hwnd, GWL_WNDPROC, oldWndProc);
                _oldWndProcs.Remove(hwnd);
                _wndProcDelegates.Remove(hwnd);
            }
        }

        /// <summary>
        /// 判断点是否在可交互区域（此处为窗口上半部分，可自定义）
        /// </summary>
        private static bool IsInInteractiveRegion(Window window, int x, int y)
        {
            // 例：窗口上半部分可交互
            var bounds = window.Bounds;
            return y < bounds.Height / 2;
        }

        #region Win32

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOPMOST = 0x00000008;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint CallWindowProc(
            nint lpPrevWndFunc,
            nint hWnd,
            uint msg,
            nint wParam,
            nint lParam
        );

        #endregion
    }
}
