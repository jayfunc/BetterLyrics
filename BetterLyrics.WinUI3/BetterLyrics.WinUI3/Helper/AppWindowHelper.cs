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
            appWindow.SetIcon(@"Assets/Logo.ico");
            appWindow.SetTaskbarIcon(@"Assets/Logo.ico");
            appWindow.SetTitleBarIcon(@"Assets/Logo.ico");
        }
    }
}
