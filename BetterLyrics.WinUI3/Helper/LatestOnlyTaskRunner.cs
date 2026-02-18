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

        public async Task RunAsync(Func<CancellationToken, Task> taskFactory)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            string taskName = taskFactory.Method.Name;
            string tokenHashCode = token.GetHashCode().ToString();

            try
            {
                //_logger.LogInformation("RunAsync: Starting task {Name} with token hash code {HashCode}.", taskName, tokenHashCode);
                Debug.WriteLine($"RunAsync: Starting task {taskName} with token hash code {tokenHashCode}.");

                await taskFactory(token);
                
                //_logger.LogInformation("RunAsync: Task {Name} with token hash code {HashCode} completed successfully.", taskFactory.Method.Name, tokenHashCode);
                Debug.WriteLine($"RunAsync: Task {taskName} with token hash code {tokenHashCode} completed successfully.");
            }
            catch (OperationCanceledException)
            {
                //_logger.LogInformation("RunAsync: Task {Name} with token hash code {HashCode} was cancelled.", taskFactory.Method.Name, tokenHashCode);
                Debug.WriteLine($"RunAsync: Task {taskName} with token hash code {tokenHashCode} was cancelled.");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "RunAsync: Task {Name} threw an exception.", taskFactory.Method.Name);
                Debug.WriteLine($"RunAsync: Task {taskFactory.Method.Name} threw an exception: {ex}");
            }
        }
    }
}
