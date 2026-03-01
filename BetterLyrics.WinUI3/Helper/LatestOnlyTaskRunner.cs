using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class LatestOnlyTaskRunner
    {
        //private static readonly ILogger<LatestOnlyTaskRunner> _logger = Ioc.Default.GetRequiredService<ILogger<LatestOnlyTaskRunner>>();
        private CancellationTokenSource? _cts;

        public async Task RunAsync(Func<CancellationToken, Task> taskFactory, int maxRetries = 1, int delayMilliseconds = 1000)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            string taskName = taskFactory.Method.Name;
            string tokenHashCode = token.GetHashCode().ToString();

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"RunAsync: Starting task {taskName} (Attempt {attempt}/{maxRetries}) with token {tokenHashCode}.");

                    await taskFactory(token);

                    Debug.WriteLine($"RunAsync: Task {taskName} completed successfully on attempt {attempt}.");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"RunAsync: Task {taskName} with token hash code {tokenHashCode} was cancelled. Stopping retries.");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RunAsync: Task {taskName} threw an exception on attempt {attempt}: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Debug.WriteLine($"RunAsync: Task {taskName} failed after {maxRetries} attempts. Giving up.");
                        return;
                    }

                    try
                    {
                        Debug.WriteLine($"RunAsync: Waiting {delayMilliseconds}ms before next retry...");
                        await Task.Delay(delayMilliseconds, token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"RunAsync: Task {taskName} was cancelled during retry delay.");
                        return;
                    }
                }
            }
        }
    }
}
