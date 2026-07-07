using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Helpers;

public static class SystemHelper
{
    public static bool IsWindows11OrGreater => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static SoftwareFramework CurrentFramework
    {
        get
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var hasAvalonia = assemblies.Any(a => a.GetName().Name?.StartsWith("Avalonia") == true);
            if (hasAvalonia) return SoftwareFramework.Avalonia;

            var hasWinUI3 = assemblies.Any(a => a.GetName().Name == "Microsoft.WinUI");
            if (hasWinUI3) return SoftwareFramework.WinUI3;

            return SoftwareFramework.Unknown;
        }
    }
}