using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using static BetterLyrics.WinUI3.Helper.Win32Helper;

namespace BetterLyrics.WinUI3.Helper
{
    public class ForegroundWindowWatcherHelper
    {
        private readonly WinEventDelegate _winEventDelegate;
        private readonly List<IntPtr> _hooks = new();
        private IntPtr _currentForeground = IntPtr.Zero;
        private readonly IntPtr _selfHwnd;
        private readonly DispatcherTimer _pollingTimer;
        private DateTime _lastEventTime = DateTime.MinValue;
        private const int ThrottleIntervalMs = 1000;

        public delegate void WindowChangedHandler(IntPtr hwnd);
        private readonly WindowChangedHandler _onWindowChanged;

        public ForegroundWindowWatcherHelper(IntPtr selfHwnd, WindowChangedHandler onWindowChanged)
        {
            _selfHwnd = selfHwnd;
            _onWindowChanged = onWindowChanged;
            _winEventDelegate = new WinEventDelegate(WinEventProc);

            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _pollingTimer.Tick += (_, _) =>
            {
                if (_currentForeground != IntPtr.Zero && _currentForeground != _selfHwnd)
                    _onWindowChanged?.Invoke(_currentForeground);
            };
        }

        public void Start()
        {
            // Hook: foreground changes and minimize end
            _hooks.Add(
                SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_MINIMIZEEND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT
                )
            );

            // Hook: window move/resize (location change)
            _hooks.Add(
                SetWinEventHook(
                    EVENT_OBJECT_LOCATIONCHANGE,
                    EVENT_OBJECT_LOCATIONCHANGE,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT
                )
            );

            _pollingTimer.Start();
        }

        public void Stop()
        {
            foreach (var hook in _hooks)
                UnhookWinEvent(hook);

            _hooks.Clear();
            _pollingTimer.Stop();
        }

        private void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        )
        {
            if (hwnd == IntPtr.Zero || hwnd == _selfHwnd)
                return;

            var now = DateTime.Now;
            if ((now - _lastEventTime).TotalMilliseconds < ThrottleIntervalMs)
                return;

            _lastEventTime = now;

            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                _currentForeground = hwnd;
                _onWindowChanged?.Invoke(hwnd);
            }
            else if ((eventType == EVENT_OBJECT_LOCATIONCHANGE || eventType == EVENT_SYSTEM_MINIMIZEEND) && hwnd == _currentForeground)
            {
                _onWindowChanged?.Invoke(hwnd);
            }
        }
    }
}
