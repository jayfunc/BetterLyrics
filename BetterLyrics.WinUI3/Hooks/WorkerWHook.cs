using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using Vanara.PInvoke;
using WinRT.Interop;
using static Vanara.PInvoke.User32;

namespace BetterLyrics.WinUI3.Hooks
{
    /// <summary>
    /// Ref <see href="https://blog.cast1e.top/posts/windeskchange/wdc/"/>
    /// </summary>
    public class WorkerWHook
    {
        private static User32.SafeHWINEVENTHOOK? _hWinEventHook;
        private static User32.WinEventProc? _winEventDelegate;
        private static HWND _hWorkerW = HWND.NULL;
        private static NowPlayingWindow? _pinnedWindow;
        private static DispatcherQueueTimer? _debounceTimer;

        public static void PinToDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            HWND windowHandle = (HWND)WindowNative.GetWindowHandle(window);
            HWND hProgman = User32.FindWindow("Progman", null);

            IntPtr _ = IntPtr.Zero;

            // 发送 0x052C 消息，触发 WorkerW 层的生成
            User32.SendMessageTimeout(hProgman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, ref _);

            HWND hWorkerW = User32.FindWindowEx(hProgman, HWND.NULL, "WorkerW", null);

            // 设置父窗口
            if (hWorkerW != HWND.NULL)
            {
                // 保存全局状态以便后续更新
                _pinnedWindow = window;
                _hWorkerW = hWorkerW;

                // 首次计算并定位
                RepositionPinnedWindow();
                User32.SetParent(windowHandle, hWorkerW);

                // 启动对 WorkerW 的尺寸监听
                StartListeningToWorkerW();
            }
        }

        public static void UnpinFromDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // 停止监听并释放资源
            StopListeningToWorkerW();

            // 必需在此处先拿到以免在 SetParent 时新坐标被记忆
            var windowBounds = window.LyricsWindowStatus.WindowBounds;

            HWND windowHandle = (HWND)WindowNative.GetWindowHandle(window);
            User32.SetParent(windowHandle, HWND.NULL);

            window.MoveAndResize(windowBounds);

            _pinnedWindow = null;
            _hWorkerW = HWND.NULL;
        }

        private static void StartListeningToWorkerW()
        {
            if (_hWinEventHook != null && !_hWinEventHook.IsInvalid) return;
            if (_hWorkerW == HWND.NULL) return;

            uint threadId = User32.GetWindowThreadProcessId(_hWorkerW, out uint processId);

            _winEventDelegate = new User32.WinEventProc(WinEventCallback);

            _hWinEventHook = User32.SetWinEventHook(
                EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                HINSTANCE.NULL,
                _winEventDelegate,
                processId,
                threadId,
                User32.WINEVENT.WINEVENT_OUTOFCONTEXT);
        }

        private static void StopListeningToWorkerW()
        {
            if (_hWinEventHook != null && !_hWinEventHook.IsInvalid)
            {
                _hWinEventHook.Dispose();
                _hWinEventHook = null;
                _winEventDelegate = null;
            }

            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer = null;
            }
        }

        private static void WinEventCallback(HWINEVENTHOOK hWinEventHook, User32.EventConstant eventType, HWND hwnd, User32.ObjectIdentifier idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == _hWorkerW && idObject == (int)User32.ObjectIdentifier.OBJID_WINDOW)
            {
                if (_debounceTimer == null && _pinnedWindow != null)
                {
                    _debounceTimer = _pinnedWindow.DispatcherQueue.CreateTimer();
                    _debounceTimer.Interval = TimeSpan.FromMilliseconds(200);
                    _debounceTimer.Tick += (s, e) =>
                    {
                        _debounceTimer.Stop();
                        RepositionPinnedWindow();
                    };
                }

                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
        }

        private static void RepositionPinnedWindow()
        {
            if (_pinnedWindow == null || _hWorkerW == HWND.NULL) return;

            HWND windowHandle = (HWND)WindowNative.GetWindowHandle(_pinnedWindow);

            var windowBounds = _pinnedWindow.LyricsWindowStatus.WindowBounds.ToRectInt32();
            POINT pt = new() { X = windowBounds.X, Y = windowBounds.Y };

            User32.ScreenToClient(_hWorkerW, ref pt);

            User32.SetWindowPos(windowHandle, HWND.NULL,
                pt.X, pt.Y,
                windowBounds.Width, windowBounds.Height,
                User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOACTIVATE);
        }
    }
}