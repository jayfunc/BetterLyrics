using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class AppUIThread
    {
        private static DispatcherQueue? _dispatcherQueue;

        public static void Initialize(DispatcherQueue dispatcher)
        {
            _dispatcherQueue = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public static void Execute(Action action)
        {
            _dispatcherQueue?.TryEnqueue(() => action());
        }

        public static Task RunAsync(Action action)
        {
            if (_dispatcherQueue == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource();
            _dispatcherQueue.TryEnqueue(() =>
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
