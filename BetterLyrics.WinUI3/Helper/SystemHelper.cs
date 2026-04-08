using System;

namespace BetterLyrics.WinUI3.Helper
{
    public static class SystemHelper
    {
        public static readonly bool IsWindows11OrGreater = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
    }
}