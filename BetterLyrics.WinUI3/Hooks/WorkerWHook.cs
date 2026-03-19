using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using WinRT.Interop;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

namespace BetterLyrics.WinUI3.Hooks
{
    /// <summary>
    /// Ref <see href="https://blog.cast1e.top/posts/windeskchange/wdc/"/>
    /// </summary>
    public class WorkerWHook
    {
        /// <summary>
        /// 将指定的窗口固定到桌面壁纸层
        /// </summary>
        /// <param name="window"></param>
        public static void PinToDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            IntPtr windowHandle = WindowNative.GetWindowHandle(window);

            HWND hProgman = User32.FindWindow("Progman", null);

            var _ = IntPtr.Zero;
            // 发送 0x052C 消息，触发 WorkerW 层的生成
            User32.SendMessageTimeout(hProgman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, ref _);

            HWND hWorkerW = User32.FindWindowEx(hProgman, HWND.NULL, "WorkerW", null);

            // 设置父窗口
            if (hWorkerW != HWND.NULL)
            {
                var windowBounds = window.LyricsWindowStatus.WindowBounds.ToRectInt32();

                POINT pt = new() { X = windowBounds.X, Y = windowBounds.Y };
                User32.ScreenToClient(hWorkerW, ref pt);

                User32.SetParent(windowHandle, hWorkerW);

                User32.SetWindowPos(windowHandle, IntPtr.Zero, 
                    pt.X, pt.Y, 
                    windowBounds.Width, windowBounds.Height, 
                    User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOACTIVATE);
            }

        }

        public static void UnpinFromDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // 必需在此处先拿到以免在 SetParent 时新坐标被记忆
            var windowBounds = window.LyricsWindowStatus.WindowBounds;

            IntPtr windowHandle = WindowNative.GetWindowHandle(window);
            User32.SetParent(windowHandle, HWND.NULL);

            window.MoveAndResize(windowBounds);
        }

    }
}
