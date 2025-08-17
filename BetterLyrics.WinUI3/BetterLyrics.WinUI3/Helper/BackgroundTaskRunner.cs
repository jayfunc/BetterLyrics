using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class BackgroundTaskRunner
    {
        private CancellationTokenSource? _cts;

        public void Run(Func<CancellationToken, Task> taskFactory)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
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
            }, token);
        }
    }
}
