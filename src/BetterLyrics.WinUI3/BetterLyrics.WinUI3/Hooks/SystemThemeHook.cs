using System;
using Microsoft.UI.Xaml;
using Microsoft.Win32;

namespace BetterLyrics.WinUI3.Hooks;

public static class SystemThemeHook
{
    private static readonly DispatcherTimer? _timer;
    private static ApplicationTheme _lastTheme;

    static SystemThemeHook()
    {
        _lastTheme = GetCurrentMode();

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var current = GetCurrentMode();
            if (current != _lastTheme)
            {
                _lastTheme = current;
                ThemeChanged?.Invoke(current);
            }
        };
        _timer.Start();
    }

    public static event Action<ApplicationTheme>? ThemeChanged;

    public static ApplicationTheme GetCurrentMode()
    {
        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = (int)key.GetValue("SystemUsesLightTheme", 1);
                return value == 1 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
        }
        catch
        {
        }

        return ApplicationTheme.Dark;
    }
}