using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
