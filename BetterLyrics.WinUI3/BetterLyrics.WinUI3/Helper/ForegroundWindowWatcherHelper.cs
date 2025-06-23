// 2025/6/23 by Zhe Fang

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="ForegroundWindowWatcherHelper" />
    /// </summary>
    public class ForegroundWindowWatcherHelper
    {
        #region Constants

        /// <summary>
        /// Defines the EVENT_OBJECT_LOCATIONCHANGE
        /// </summary>
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

        /// <summary>
        /// Defines the EVENT_SYSTEM_FOREGROUND
        /// </summary>
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        /// <summary>
        /// Defines the EVENT_SYSTEM_MINIMIZEEND
        /// </summary>
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

        /// <summary>
        /// Defines the ThrottleIntervalMs
        /// </summary>
        private const int ThrottleIntervalMs = 100;

        /// <summary>
        /// Defines the WINEVENT_OUTOFCONTEXT
        /// </summary>
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        #endregion

        #region Fields

        /// <summary>
        /// Defines the _hooks
        /// </summary>
        private readonly List<IntPtr> _hooks = new();

        /// <summary>
        /// Defines the _onWindowChanged
        /// </summary>
        private readonly WindowChangedHandler _onWindowChanged;

        /// <summary>
        /// Defines the _pollingTimer
        /// </summary>
        private readonly DispatcherTimer _pollingTimer;

        /// <summary>
        /// Defines the _selfHwnd
        /// </summary>
        private readonly IntPtr _selfHwnd;

        /// <summary>
        /// Defines the _winEventDelegate
        /// </summary>
        private readonly WinEventDelegate _winEventDelegate;

        /// <summary>
        /// Defines the _currentForeground
        /// </summary>
        private IntPtr _currentForeground = IntPtr.Zero;

        /// <summary>
        /// Defines the _lastEventTime
        /// </summary>
        private DateTime _lastEventTime = DateTime.MinValue;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ForegroundWindowWatcherHelper"/> class.
        /// </summary>
        /// <param name="selfHwnd">The selfHwnd<see cref="IntPtr"/></param>
        /// <param name="onWindowChanged">The onWindowChanged<see cref="WindowChangedHandler"/></param>
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

        #endregion

        #region Delegates

        /// <summary>
        /// The WindowChangedHandler
        /// </summary>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
        public delegate void WindowChangedHandler(IntPtr hwnd);

        /// <summary>
        /// The WinEventDelegate
        /// </summary>
        /// <param name="hWinEventHook">The hWinEventHook<see cref="IntPtr"/></param>
        /// <param name="eventType">The eventType<see cref="uint"/></param>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
        /// <param name="idObject">The idObject<see cref="int"/></param>
        /// <param name="idChild">The idChild<see cref="int"/></param>
        /// <param name="dwEventThread">The dwEventThread<see cref="uint"/></param>
        /// <param name="dwmsEventTime">The dwmsEventTime<see cref="uint"/></param>
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        );

        #endregion

        #region Methods

        /// <summary>
        /// The Start
        /// </summary>
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

        /// <summary>
        /// The Stop
        /// </summary>
        public void Stop()
        {
            foreach (var hook in _hooks)
                UnhookWinEvent(hook);

            _hooks.Clear();
            _pollingTimer.Stop();
        }

        /// <summary>
        /// The SetWinEventHook
        /// </summary>
        /// <param name="eventMin">The eventMin<see cref="uint"/></param>
        /// <param name="eventMax">The eventMax<see cref="uint"/></param>
        /// <param name="hmodWinEventProc">The hmodWinEventProc<see cref="IntPtr"/></param>
        /// <param name="lpfnWinEventProc">The lpfnWinEventProc<see cref="WinEventDelegate"/></param>
        /// <param name="idProcess">The idProcess<see cref="uint"/></param>
        /// <param name="idThread">The idThread<see cref="uint"/></param>
        /// <param name="dwFlags">The dwFlags<see cref="uint"/></param>
        /// <returns>The <see cref="IntPtr"/></returns>
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags
        );

        /// <summary>
        /// The UnhookWinEvent
        /// </summary>
        /// <param name="hWinEventHook">The hWinEventHook<see cref="IntPtr"/></param>
        /// <returns>The <see cref="bool"/></returns>
        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        /// <summary>
        /// The WinEventProc
        /// </summary>
        /// <param name="hWinEventHook">The hWinEventHook<see cref="IntPtr"/></param>
        /// <param name="eventType">The eventType<see cref="uint"/></param>
        /// <param name="hwnd">The hwnd<see cref="IntPtr"/></param>
        /// <param name="idObject">The idObject<see cref="int"/></param>
        /// <param name="idChild">The idChild<see cref="int"/></param>
        /// <param name="dwEventThread">The dwEventThread<see cref="uint"/></param>
        /// <param name="dwmsEventTime">The dwmsEventTime<see cref="uint"/></param>
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
            else if (
                (eventType == EVENT_OBJECT_LOCATIONCHANGE || eventType == EVENT_SYSTEM_MINIMIZEEND)
                && hwnd == _currentForeground
            )
            {
                _onWindowChanged?.Invoke(hwnd);
            }
        }

        #endregion
    }
}
