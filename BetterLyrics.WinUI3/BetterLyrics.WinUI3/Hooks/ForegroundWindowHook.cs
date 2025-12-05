using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Hooks
{
    public class ForegroundWindowHook
    {
        private readonly User32.WinEventProc _winEventDelegate;
        private readonly List<User32.HWINEVENTHOOK> _hooks = new();
        private HWND _currentForeground = HWND.NULL;
        private readonly IntPtr _selfHwnd;

        public delegate void WindowChangedHandler(HWND hwnd);
        private readonly WindowChangedHandler _onWindowChanged;

        private readonly DispatcherTimer _timer;

        public ForegroundWindowHook(IntPtr selfHwnd, WindowChangedHandler onWindowChanged)
        {
            _selfHwnd = selfHwnd;
            _onWindowChanged = onWindowChanged;
            _winEventDelegate = new User32.WinEventProc(WinEventProc);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            // Hook: foreground changes and minimize end
            _hooks.Add(
                User32.SetWinEventHook(
                    User32.EventConstants.EVENT_SYSTEM_FOREGROUND,
                    User32.EventConstants.EVENT_SYSTEM_MINIMIZEEND,
                    HINSTANCE.NULL,
                    _winEventDelegate,
                    0,
                    0,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT
                )
            );

            // Hook: window move/resize (location change)
            _hooks.Add(
                User32.SetWinEventHook(
                    User32.EventConstants.EVENT_OBJECT_LOCATIONCHANGE,
                    User32.EventConstants.EVENT_OBJECT_LOCATIONCHANGE,
                    HINSTANCE.NULL,
                    _winEventDelegate,
                    0,
                    0,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT
                )
            );

            _timer.Start();
        }

        public void Stop()
        {
            foreach (var hook in _hooks)
                User32.UnhookWinEvent(hook);

            _hooks.Clear();

            _timer.Stop();
        }

        private void Timer_Tick(object? sender, object e)
        {
            if (_currentForeground != HWND.NULL)
            {
                _onWindowChanged?.Invoke(_currentForeground);
            }
        }

        private void WinEventProc(
            User32.HWINEVENTHOOK hWinEventHook,
            uint eventType,
            HWND hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        )
        {
            if (hwnd == IntPtr.Zero)
                return;

            if (eventType == User32.EventConstants.EVENT_SYSTEM_FOREGROUND)
            {
                _currentForeground = hwnd;
                _onWindowChanged?.Invoke(hwnd);
            }
            else if ((eventType == User32.EventConstants.EVENT_OBJECT_LOCATIONCHANGE || eventType == User32.EventConstants.EVENT_SYSTEM_MINIMIZEEND) && hwnd == _currentForeground)
            {
                _onWindowChanged?.Invoke(hwnd);
            }
        }
    }
}
