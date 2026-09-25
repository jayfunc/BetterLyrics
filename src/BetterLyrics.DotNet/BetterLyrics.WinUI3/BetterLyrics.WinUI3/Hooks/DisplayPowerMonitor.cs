using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.WinUI3.Hooks;

public static class DisplayPowerMonitor
{
    private static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
    private static User32.SafeHPOWERSETTINGNOTIFY _powerNotifyHandle;
    private static Win32Window _messageWindow;

    private static event EventHandler<bool> _displayStatusChanged;
    public static event EventHandler<bool> DisplayStatusChanged
    {
        add
        {
            Initialize();
            _displayStatusChanged += value;
        }
        remove
        {
            _displayStatusChanged -= value;
        }
    }
    
    public static bool IsDisplayOn { get; private set; } = true;

    private static void Initialize()
    {
        if (_messageWindow != null) return;
        _messageWindow = new Win32Window();
        _messageWindow.PowerSettingChanged += OnPowerSettingChanged;
        _powerNotifyHandle = User32.RegisterPowerSettingNotification(new HANDLE((IntPtr)_messageWindow.Handle), in GUID_CONSOLE_DISPLAY_STATE, User32.DEVICE_NOTIFY.DEVICE_NOTIFY_WINDOW_HANDLE);
    }

    private static void OnPowerSettingChanged(bool isDisplayOn)
    {
        IsDisplayOn = isDisplayOn;
        try
        {
            var logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(nameof(DisplayPowerMonitor));
            logger?.LogInformation("Display status changed: {Status}", isDisplayOn ? "On" : "Off");
        }
        catch { }
        _displayStatusChanged?.Invoke(null, isDisplayOn);
    }

    private class Win32Window
    {
        public HWND Handle;
        private User32.WindowProc _wndProc;

        public event Action<bool> PowerSettingChanged;

        public Win32Window()
        {
            _wndProc = CustomWndProc;
            var wndClass = new User32.WNDCLASS
            {
                lpszClassName = "BetterLyricsPowerMonitorClass",
                lpfnWndProc = _wndProc
            };
            User32.RegisterClass(wndClass);
            Handle = User32.CreateWindowEx(0, wndClass.lpszClassName, "BetterLyricsPowerMonitor", 0, 0, 0, 0, 0, (HWND)(IntPtr)(-3) /*HWND_MESSAGE*/, HMENU.NULL, HINSTANCE.NULL, IntPtr.Zero);
        }

        private IntPtr CustomWndProc(HWND hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)User32.WindowMessage.WM_POWERBROADCAST && wParam.ToInt32() == (int)User32.PowerBroadcastType.PBT_POWERSETTINGCHANGE)
            {
                var setting = Marshal.PtrToStructure<User32.POWERBROADCAST_SETTING>(lParam);
                if (setting.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
                {
                    var state = Marshal.ReadByte(lParam, Marshal.OffsetOf<User32.POWERBROADCAST_SETTING>("Data").ToInt32());
                    PowerSettingChanged?.Invoke(state != 0);
                }
            }
            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
