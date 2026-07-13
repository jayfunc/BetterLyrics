using BetterLyrics.Core.Helpers;
using Microsoft.UI.Windowing;

namespace BetterLyrics.WinUI3.Extensions;

public static class AppWindowExtensions
{
    extension(AppWindow appWindow)
    {
        public void SetIcons()
        {
            string icon = "Logo.ico";
            appWindow.SetIcon(icon);
            appWindow.SetTaskbarIcon(icon);
            appWindow.SetTitleBarIcon(icon);
        }
    }
}