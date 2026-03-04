using Microsoft.UI.Dispatching;

namespace BetterLyrics.WinUI3.Helper
{
    public class DispatcherQueueHelper
    {
        public static DispatcherQueue? GetUIDispatcherQueue() => App.SystemTrayWindow?.DispatcherQueue;
    }
}
