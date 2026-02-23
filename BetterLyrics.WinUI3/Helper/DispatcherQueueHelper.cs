using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Helper
{
    public class DispatcherQueueHelper
    {
        public static DispatcherQueue? GetUIDispatcherQueue() => App.SystemTrayWindow?.DispatcherQueue;
    }
}
