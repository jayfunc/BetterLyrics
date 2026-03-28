using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinRT.Interop;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.ComCtl32; // 引入 Vanara 的 ComCtl32

namespace BetterLyrics.WinUI3.Hooks
{
    public class WorkerWHook
    {
        private static User32.SafeHWINEVENTHOOK? _hLocationWinEventHook;
        private static User32.WinEventProc? _winEventDelegate;
        private static HWND _hWorkerW = HWND.NULL;
        private static NowPlayingWindow? _pinnedWindow;
        private static DispatcherQueueTimer? _debounceTimer;

        private static SUBCLASSPROC? _subclassDelegate;
        private static readonly nuint _subclassId = 027;

        public static void PinToDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            HWND windowHandle = (HWND)WindowNative.GetWindowHandle(window);

            if (_subclassDelegate == null)
            {
                _subclassDelegate = new SUBCLASSPROC(WindowSubclassProc);
                ComCtl32.SetWindowSubclass(windowHandle, _subclassDelegate, _subclassId, IntPtr.Zero);
            }

            HWND hProgman = User32.FindWindow("Progman", null);
            IntPtr _ = IntPtr.Zero;

            // 触发 WorkerW 生成
            User32.SendMessageTimeout(hProgman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, ref _);

            HWND hWorkerW = User32.FindWindowEx(hProgman, HWND.NULL, "WorkerW", null);

            if (hWorkerW != HWND.NULL)
            {
                _pinnedWindow = window;
                _hWorkerW = hWorkerW;

                RepositionPinnedWindow();
                User32.SetParent(windowHandle, hWorkerW);

                StartListening();
            }
        }

        public static void UnpinFromDesktop(NowPlayingWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            StopListening();

            HWND windowHandle = (HWND)WindowNative.GetWindowHandle(window);

            if (_subclassDelegate != null)
            {
                ComCtl32.RemoveWindowSubclass(windowHandle, _subclassDelegate, _subclassId);
                _subclassDelegate = null;
            }

            var windowBounds = window.LyricsWindowStatus.WindowBounds;

            User32.SetParent(windowHandle, HWND.NULL);
            window.MoveAndResize(windowBounds);

            _pinnedWindow = null;
            _hWorkerW = HWND.NULL;
        }

        private static IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == (uint)User32.WindowMessage.WM_SETTINGCHANGE)
            {
                if (wParam.ToInt32() == (int)User32.SPI.SPI_SETDESKWALLPAPER)
                {
                    if (_pinnedWindow != null)
                    {
                        var windowToSave = _pinnedWindow;

                        User32.SetParent(hWnd, HWND.NULL);

                        _pinnedWindow.DispatcherQueue.TryEnqueue(async () =>
                        {
                            StopListening();
                            _pinnedWindow = null;
                            _hWorkerW = HWND.NULL;

                            await Task.Delay(800);

                            PinToDesktop(windowToSave);
                        });
                    }
                }
            }

            return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private static void StartListening()
        {
            if (_pinnedWindow == null || _hWorkerW == HWND.NULL) return;

            uint workerThreadId = User32.GetWindowThreadProcessId(_hWorkerW, out uint workerProcessId);
            _winEventDelegate = new User32.WinEventProc(WinEventCallback);

            if (_hLocationWinEventHook == null || _hLocationWinEventHook.IsInvalid)
            {
                _hLocationWinEventHook = User32.SetWinEventHook(
                    EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                    EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                    HINSTANCE.NULL,
                    _winEventDelegate,
                    workerProcessId,
                    workerThreadId,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT);
            }
        }

        private static void StopListening()
        {
            if (_hLocationWinEventHook != null && !_hLocationWinEventHook.IsInvalid)
            {
                _hLocationWinEventHook.Dispose();
                _hLocationWinEventHook = null;
            }

            _winEventDelegate = null;

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