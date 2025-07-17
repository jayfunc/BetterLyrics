using Nito.AsyncEx;
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
        private readonly AsyncLock _mutex = new();
        private CancellationTokenSource _cts;

        public async Task RunAsync(Func<CancellationToken, Task> action)
        {
            CancellationTokenSource oldCts;

            // 使用 AsyncLock 保证线程安全
            using (await _mutex.LockAsync())
            {
                // 取消旧的
                oldCts = _cts;
                _cts = new CancellationTokenSource();
            }

            oldCts?.Cancel();
            oldCts?.Dispose();

            CancellationToken token = _cts.Token;

            try
            {
                await action(token);
            }
            catch (OperationCanceledException)
            {
                // 可以选择忽略取消异常
            }
        }
    }
}
