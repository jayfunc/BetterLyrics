using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Hooks;

public partial class MonitorHook : IDisposable
{
    private HWND _handle;
    private readonly User32.WindowProc _wndProc;
    private readonly string _className = "BetterLyricsMonitorWatcherClass";
    private bool _isDisposed;
    private User32.SafeHPOWERSETTINGNOTIFY _powerNotifyHandle;
    private static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");

    public event Action? DisplayChanged;
    public event Action<bool>? DisplayPowerStatusChanged;

    public MonitorHook()
    {
        _wndProc = CustomWndProc;
        var wndClass = new User32.WNDCLASS
        {
            lpszClassName = _className,
            lpfnWndProc = _wndProc
        };
        User32.RegisterClass(wndClass);
        
        // MUST be a top-level window (HWND.NULL) to receive WM_DISPLAYCHANGE broadcast, not a message-only window
        _handle = User32.CreateWindowEx(
            User32.WindowStylesEx.WS_EX_LEFT, 
            _className, 
            "BetterLyricsMonitorWatcher", 
            User32.WindowStyles.WS_OVERLAPPED, 
            0, 0, 0, 0, 
            HWND.NULL, 
            HMENU.NULL, 
            HINSTANCE.NULL, 
            IntPtr.Zero);

        _powerNotifyHandle = User32.RegisterPowerSettingNotification(
            new HANDLE((IntPtr)_handle), 
            in GUID_CONSOLE_DISPLAY_STATE, 
            User32.DEVICE_NOTIFY.DEVICE_NOTIFY_WINDOW_HANDLE);
    }

    private IntPtr CustomWndProc(HWND hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == (uint)User32.WindowMessage.WM_DISPLAYCHANGE)
        {
            DisplayChanged?.Invoke();
        }
        else if (msg == (uint)User32.WindowMessage.WM_POWERBROADCAST && wParam.ToInt32() == (int)User32.PowerBroadcastType.PBT_POWERSETTINGCHANGE)
        {
            var setting = Marshal.PtrToStructure<User32.POWERBROADCAST_SETTING>(lParam);
            if (setting.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
            {
                var state = Marshal.ReadByte(lParam, Marshal.OffsetOf<User32.POWERBROADCAST_SETTING>("Data").ToInt32());
                DisplayPowerStatusChanged?.Invoke(state != 0);
            }
        }
        return User32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (_powerNotifyHandle != null && !_powerNotifyHandle.IsInvalid)
            {
                _powerNotifyHandle.Dispose();
            }

            if (_handle != HWND.NULL)
            {
                User32.DestroyWindow(_handle);
                _handle = HWND.NULL;
            }

            User32.UnregisterClass(_className, HINSTANCE.NULL);

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MonitorHook()
    {
        Dispose(false);
    }
}
