using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class DesktopModeHelper
    {
        private static readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        private static readonly Dictionary<IntPtr, bool> _clickThroughStates = [];
        private static readonly Dictionary<IntPtr, bool> _originalTopmostStates = [];
        private static readonly Dictionary<IntPtr, (double X, double Y, double Width, double Height)> _originalWindowBounds = [];
        private static readonly Dictionary<IntPtr, WindowStyle> _originalWindowStyles = [];

        private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

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

            // 从存储区获取目标宽高和位置
            int targetWidth = _settingsService.DesktopWindowWidth;
            int targetHeight = _settingsService.DesktopWindowHeight;
            int targetX = _settingsService.DesktopWindowLeft;
            int targetY = _settingsService.DesktopWindowTop;

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

        public static void Lock(Window window)
        {
            window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);
            
            // 设置无边框、透明
            window.ToggleWindowStyle(true, WindowStyle.Popup | WindowStyle.Visible);
            window.ExtendsContentIntoTitleBar = false;

            SetClickThrough(window, true);
        }

        public static void SetClickThrough(Window window, bool enable)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            int exStyle = User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);
            if (enable)
            {
                User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, exStyle | (int)User32.WindowStylesEx.WS_EX_TRANSPARENT | (int)User32.WindowStylesEx.WS_EX_LAYERED);
                _clickThroughStates[hwnd] = true;
            }
            else
            {
                User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, exStyle & ~(int)User32.WindowStylesEx.WS_EX_TRANSPARENT);
                _clickThroughStates[hwnd] = false;
            }
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

            // To recover the system backdrop, we need to reopen the window
            WindowHelper.RestartApp(AppInfo.UnlockWindowTag);
        }
    }
}
