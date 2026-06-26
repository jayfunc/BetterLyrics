using BetterLyrics.Core.Helpers;
using Microsoft.UI.Windowing;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class AppWindowExtensions
    {
        extension(AppWindow appWindow)
        {
            public void SetIcons()
            {
                appWindow.SetIcon(PathHelper.LogoPath);
                appWindow.SetTaskbarIcon(PathHelper.LogoPath);
                appWindow.SetTitleBarIcon(PathHelper.LogoPath);
            }
        }
    }
}
