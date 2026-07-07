using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Vanara.PInvoke;
using WinRT.Interop;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.User32;

namespace BetterLyrics.WinUI3.Hooks;

public class WorkerWHook
{
    private static readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private static SafeHWINEVENTHOOK? _hLocationWinEventHook;
    private static WinEventProc? _winEventDelegate;
    private static HWND _hWorkerW = HWND.NULL;

    // 追踪所有被固定的窗口
    private static readonly Dictionary<HWND, NowPlayingWindow> _pinnedWindows = new();
    private static readonly object _lock = new();

    private static Timer? _debounceTimer;

    private static SUBCLASSPROC? _subclassDelegate;
    private static readonly nuint _subclassId = 027;

    // 防止壁纸切换时多次触发重载
    private static bool _isRepinning;

    public static void PinToDesktop(NowPlayingWindow window)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));

        var windowHandle = (HWND)WindowNative.GetWindowHandle(window);

        lock (_lock)
        {
            // 如果已经固定，则忽略
            if (_pinnedWindows.ContainsKey(windowHandle)) return;

            // 如果这是第一个被固定的窗口，触发 WorkerW 生成并启动监听
            if (_pinnedWindows.Count == 0)
            {
                var hProgman = FindWindow("Progman");
                var _ = IntPtr.Zero;

                // 触发 WorkerW 生成（只需要发一次）
                SendMessageTimeout(hProgman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, ref _);

                _hWorkerW = FindWindowEx(hProgman, HWND.NULL, "WorkerW");

                if (_hWorkerW != HWND.NULL) StartListening();
            }

            // 绑定当前窗口到 WorkerW
            if (_hWorkerW != HWND.NULL)
            {
                _pinnedWindows[windowHandle] = window;

                if (_subclassDelegate == null) _subclassDelegate = WindowSubclassProc;

                // 为当前窗口挂载子类化
                SetWindowSubclass(windowHandle, _subclassDelegate, _subclassId, IntPtr.Zero);

                RepositionWindow(windowHandle, window);
                SetParent(windowHandle, _hWorkerW);
            }
        }
    }

    public static void UnpinFromDesktop(NowPlayingWindow window)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));

        var windowHandle = (HWND)WindowNative.GetWindowHandle(window);

        lock (_lock)
        {
            if (!_pinnedWindows.ContainsKey(windowHandle)) return;

            if (_subclassDelegate != null) RemoveWindowSubclass(windowHandle, _subclassDelegate, _subclassId);

            var windowBounds = window.LyricsWindowStatus.WindowBounds;

            SetParent(windowHandle, HWND.NULL);
            _windowManagerProvider.MoveAndResize(window, windowBounds);

            _pinnedWindows.Remove(windowHandle);

            // 如果所有窗口都解绑了，停止监听并清理 WorkerW 句柄
            if (_pinnedWindows.Count == 0)
            {
                StopListening();
                _hWorkerW = HWND.NULL;
                _subclassDelegate = null;
            }
        }
    }

    private static IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass,
        IntPtr dwRefData)
    {
        if (uMsg == (uint)WindowMessage.WM_SETTINGCHANGE)
            if (wParam.ToInt32() == (int)SPI.SPI_SETDESKWALLPAPER)
                // 防止多个窗口同时接收到壁纸更改消息导致多次重新固定
                if (!_isRepinning)
                {
                    _isRepinning = true;

                    NowPlayingWindow? triggerWindow = null;
                    lock (_lock)
                    {
                        _pinnedWindows.TryGetValue(hWnd, out triggerWindow);
                    }

                    if (triggerWindow != null)
                        triggerWindow.DispatcherQueue.TryEnqueue(async () =>
                        {
                            List<NowPlayingWindow> windowsToSave;
                            lock (_lock)
                            {
                                windowsToSave = _pinnedWindows.Values.ToList();
                            }

                            // 卸载所有窗口
                            foreach (var w in windowsToSave) UnpinFromDesktop(w);

                            await Task.Delay(800);

                            // 重新固定所有窗口
                            foreach (var w in windowsToSave) PinToDesktop(w);

                            _isRepinning = false;
                        });
                }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private static void StartListening()
    {
        if (_hWorkerW == HWND.NULL) return;

        var workerThreadId = GetWindowThreadProcessId(_hWorkerW, out var workerProcessId);
        _winEventDelegate = WinEventCallback;

        if (_hLocationWinEventHook == null || _hLocationWinEventHook.IsInvalid)
            _hLocationWinEventHook = SetWinEventHook(
                EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                EventConstant.EVENT_OBJECT_LOCATIONCHANGE,
                HINSTANCE.NULL,
                _winEventDelegate,
                workerProcessId,
                workerThreadId,
                WINEVENT.WINEVENT_OUTOFCONTEXT);

        _debounceTimer = new Timer(200);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (s, e) =>
        {
            List<NowPlayingWindow> windowsToUpdate;
            lock (_lock)
            {
                windowsToUpdate = _pinnedWindows.Values.ToList();
            }

            foreach (var window in windowsToUpdate)
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    var handle = (HWND)WindowNative.GetWindowHandle(window);
                    RepositionWindow(handle, window);
                });
        };
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
            _debounceTimer.Dispose();
            _debounceTimer = null;
        }
    }

    private static void WinEventCallback(HWINEVENTHOOK hWinEventHook, EventConstant eventType, HWND hwnd,
        ObjectIdentifier idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == _hWorkerW && idObject == (int)ObjectIdentifier.OBJID_WINDOW)
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }
    }

    private static void RepositionWindow(HWND windowHandle, NowPlayingWindow window)
    {
        if (_hWorkerW == HWND.NULL) return;

        GetWindowRect(windowHandle, out var windowRect);
        POINT pt = new() { X = windowRect.X, Y = windowRect.Y };

        ScreenToClient(_hWorkerW, ref pt);

        SetWindowPos(windowHandle, HWND.NULL,
            pt.X, pt.Y,
            windowRect.Width, windowRect.Height,
            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
    }
}