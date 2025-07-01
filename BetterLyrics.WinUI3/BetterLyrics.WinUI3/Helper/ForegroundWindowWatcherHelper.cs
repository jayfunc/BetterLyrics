using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using Windows.System;

namespace BetterLyrics.WinUI3.Helper
{
    public class ForegroundWindowWatcherHelper
    {
        private readonly User32.WinEventProc _winEventDelegate;
        private readonly List<User32.HWINEVENTHOOK> _hooks = new();
        private HWND _currentForeground = HWND.NULL;
        private readonly IntPtr _selfHwnd;
        private readonly DispatcherTimer _pollingTimer;
        private DateTime _lastEventTime = DateTime.MinValue;
        private const int ThrottleIntervalMs = 1000;

        public delegate void WindowChangedHandler(HWND hwnd);
        private readonly WindowChangedHandler _onWindowChanged;

        public ForegroundWindowWatcherHelper(IntPtr selfHwnd, WindowChangedHandler onWindowChanged)
        {
            _selfHwnd = selfHwnd;
            _onWindowChanged = onWindowChanged;
            _winEventDelegate = new User32.WinEventProc(WinEventProc);

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

            _pollingTimer.Start();
        }

        public void Stop()
        {
            foreach (var hook in _hooks)
                User32.UnhookWinEvent(hook);

            _hooks.Clear();
            _pollingTimer.Stop();
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
            if (hwnd == IntPtr.Zero || hwnd == _selfHwnd)
                return;

            var now = DateTime.Now;
            if ((now - _lastEventTime).TotalMilliseconds < ThrottleIntervalMs)
                return;

            _lastEventTime = now;

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
