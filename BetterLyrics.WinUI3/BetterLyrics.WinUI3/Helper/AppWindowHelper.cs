using Microsoft.UI.Windowing;

namespace BetterLyrics.WinUI3.Helper
{
    public static class AppWindowHelper
    {
        public static void SetIcons(this AppWindow appWindow)
        {
            appWindow.SetIcon(PathHelper.LogoPath);
            appWindow.SetTaskbarIcon(PathHelper.LogoPath);
            appWindow.SetTitleBarIcon(PathHelper.LogoPath);
        }
    }
}
