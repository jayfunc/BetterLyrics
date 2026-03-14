using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Hooks
{
    public class WorkerWHook
    {
        /// <summary>
        /// 将指定的窗口固定到桌面壁纸层
        /// </summary>
        /// <param name="window"></param>
        public static void PinToDesktop(Window window)
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
                User32.SetParent(windowHandle, hWorkerW);
            }

        }

        public static void UnpinFromDesktop(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            IntPtr windowHandle = WindowNative.GetWindowHandle(window);

            User32.SetParent(windowHandle, HWND.NULL);
        }

    }
}
