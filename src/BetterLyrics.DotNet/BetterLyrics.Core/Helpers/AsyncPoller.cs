using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Helpers;

public class AsyncPoller : IDisposable
{
    private static ILogger<AsyncPoller>? Logger
    {
        get
        {
            try
            {
                return Ioc.Default.GetService<ILogger<AsyncPoller>>();
            }
            catch
            {
                return null;
            }
        }
    }

    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;

    public AsyncPoller(int intervalMilliseconds = 1000)
    {
        _interval = TimeSpan.FromMilliseconds(intervalMilliseconds);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    public void Start(Func<CancellationToken, Task> action)
    {
        Stop();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _pollingTask = RunLoopAsync(action, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunLoopAsync(Func<CancellationToken, Task> action, CancellationToken token)
    {
        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(token))
                try
                {
                    await action(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "A polled background operation failed.");
                }
        }
        catch (OperationCanceledException)
        {
        }
    }
}