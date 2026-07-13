using System.Diagnostics;

namespace BetterLyrics.Core.Helpers;

public class LatestOnlyTaskRunner
{
    //private static readonly ILogger<LatestOnlyTaskRunner> _logger = Ioc.Default.GetRequiredService<ILogger<LatestOnlyTaskRunner>>();
    private CancellationTokenSource? _cts;

    public async Task RunAsync(Func<CancellationToken, Task> taskFactory, int maxRetries = 1,
        int delayMilliseconds = 1000)
    {
        _cts?.Cancel();
        _cts?.Dispose();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var taskName = taskFactory.Method.Name;
        var tokenHashCode = token.GetHashCode().ToString();

        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                Debug.WriteLine(
                    $"RunAsync: Starting task {taskName} (Attempt {attempt}/{maxRetries}) with token {tokenHashCode}.");

                await taskFactory(token);

                Debug.WriteLine($"RunAsync: Task {taskName} completed successfully on attempt {attempt}.");
                return;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    $"RunAsync: Task {taskName} with token hash code {tokenHashCode} was cancelled. Stopping retries.");
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

    public async Task<T?> RunAsync<T>(Func<CancellationToken, Task<T>> taskFactory, int maxRetries = 1,
        int delayMilliseconds = 1000)
    {
        _cts?.Cancel();
        _cts?.Dispose();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var taskName = taskFactory.Method.Name;
        var tokenHashCode = token.GetHashCode().ToString();

        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                Debug.WriteLine(
                    $"RunAsync<{typeof(T).Name}>: Starting task {taskName} (Attempt {attempt}/{maxRetries}) with token {tokenHashCode}.");

                var result = await taskFactory(token);

                Debug.WriteLine(
                    $"RunAsync<{typeof(T).Name}>: Task {taskName} completed successfully on attempt {attempt}.");
                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    $"RunAsync<{typeof(T).Name}>: Task {taskName} with token hash code {tokenHashCode} was cancelled. Stopping retries.");
                return default;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"RunAsync<{typeof(T).Name}>: Task {taskName} threw an exception on attempt {attempt}: {ex.Message}");

                if (attempt == maxRetries)
                {
                    Debug.WriteLine(
                        $"RunAsync<{typeof(T).Name}>: Task {taskName} failed after {maxRetries} attempts. Giving up.");
                    return default;
                }

                try
                {
                    Debug.WriteLine($"RunAsync<{typeof(T).Name}>: Waiting {delayMilliseconds}ms before next retry...");
                    await Task.Delay(delayMilliseconds, token);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"RunAsync<{typeof(T).Name}>: Task {taskName} was cancelled during retry delay.");
                    return default;
                }
            }

        return default;
    }
}