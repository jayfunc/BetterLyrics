using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class DesktopModeHelper
    {
        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyles = [];
        private static readonly Dictionary<IntPtr, bool> _clickThroughStates = [];
        private static readonly Dictionary<IntPtr, bool> _originalTopmostStates = [];
        private static readonly Dictionary<
            IntPtr,
            (double X, double Y, double Width, double Height)
        > _originalWindowBounds = [];

        // 子类化相关
        private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

        public static void Enable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 记录原始窗口位置和大小
            var windowManager = WindowManager.Get(window);
            if (!_originalWindowBounds.ContainsKey(hwnd))
            {
                _originalWindowBounds[hwnd] = (
                    windowManager.AppWindow.Position.X,
                    windowManager.AppWindow.Position.Y,
                    windowManager.Width,
                    windowManager.Height
                );
            }

            // 获取主屏幕工作区
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                windowManager.AppWindow.Id,
                Microsoft.UI.Windowing.DisplayAreaFallback.Primary
            );
            var workArea = displayArea.WorkArea;

            // 计算目标宽高和位置
            int targetWidth = workArea.Width / 3;
            int targetHeight = workArea.Height / 4;
            int targetX = workArea.X + (workArea.Width - targetWidth) / 2; // 居中
            int targetY = workArea.Y + workArea.Height - targetHeight - 64;

            // 设置窗口大小和位置
            windowManager.AppWindow.MoveAndResize(
                new Windows.Graphics.RectInt32(targetX, targetY, targetWidth, targetHeight)
            );

            // 记忆原样式
            if (!_originalWindowStyles.ContainsKey(hwnd))
                _originalWindowStyles[hwnd] = window.GetWindowStyle();

            // 记忆原TopMost状态
            if (!_originalTopmostStates.ContainsKey(hwnd))
                _originalTopmostStates[hwnd] = window.GetIsAlwaysOnTop();

            // 设置窗口置顶
            window.SetIsAlwaysOnTop(true);

            window.SetIsShownInSwitchers(false);
        }

        public static void Disable(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 恢复TopMost状态
            if (_originalTopmostStates.TryGetValue(hwnd, out var wasTopMost))
            {
                window.SetIsAlwaysOnTop(wasTopMost);
                _originalTopmostStates.Remove(hwnd);
            }

            // 恢复窗口位置和大小
            var windowManager = WindowManager.Get(window);
            if (_originalWindowBounds.TryGetValue(hwnd, out var bounds))
            {
                windowManager.AppWindow.MoveAndResize(
                    new Windows.Graphics.RectInt32(
                        (int)bounds.X,
                        (int)bounds.Y,
                        (int)bounds.Width,
                        (int)bounds.Height
                    )
                );
                _originalWindowBounds.Remove(hwnd);
            }

            // 恢复样式
            if (_originalWindowStyles.TryGetValue(hwnd, out var style))
            {
                window.SetWindowStyle(style);
                _originalWindowStyles.Remove(hwnd);
            }

            window.SetIsShownInSwitchers(true);
        }

        public static void Lock(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 设置无边框、透明
            window.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            window.ExtendsContentIntoTitleBar = false;

            SetClickThrough(window, true);
        }

        public static void Unlock(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            // 恢复样式（但不移出记忆的样式，只有在 Disable 时才移出）
            if (_originalWindowStyles.TryGetValue(hwnd, out var style))
            {
                window.SetWindowStyle(style);
            }
            window.ExtendsContentIntoTitleBar = true;

            SetClickThrough(window, false);
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

        #region Win32

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion
    }
}
