using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Helpers
{
    public static class AppUIThread
    {
        public static void Execute(Action action)
        {
            Dispatcher.UIThread.Invoke(() => action());
        }

        public static Task RunAsync(Action action)
        {
            var tcs = new TaskCompletionSource();
            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
