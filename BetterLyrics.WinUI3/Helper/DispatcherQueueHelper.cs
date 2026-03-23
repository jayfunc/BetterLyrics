using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Helper
{
    public static class DispatcherQueueHelper
    {
        public static DispatcherQueue? Instance { get; set; }

        public static void Init(Window window)
        {
            Instance = window.DispatcherQueue;
        }
    }
}
