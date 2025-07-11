using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class LatestOnlyTaskRunner
    {
        private CancellationTokenSource? _cts;

        public async Task RunAsync(Func<CancellationToken, Task> func)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                await func(token);
            }
            catch (OperationCanceledException) { }
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }
    }
}
