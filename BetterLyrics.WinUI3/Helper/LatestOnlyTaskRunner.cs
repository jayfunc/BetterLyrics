using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class LatestOnlyTaskRunner
    {
        private CancellationTokenSource? _cts;

        public async Task RunAsync(Func<CancellationToken, Task> taskFactory)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await taskFactory(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }
    }
}
