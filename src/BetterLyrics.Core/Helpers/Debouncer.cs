using System.Diagnostics;
using BetterLyrics.Core.Constants;

namespace BetterLyrics.Core.Helpers;

public class Debouncer : IDisposable
{
    private long _currentVersion;
    private CancellationTokenSource? _delayCts;
    private CancellationTokenSource? _executeCts;

    public void Dispose()
    {
        Cancel();

        _delayCts?.Dispose();
        _executeCts?.Dispose();

        _delayCts = null;
        _executeCts = null;

        GC.SuppressFinalize(this);
    }

    public void Cancel()
    {
        Interlocked.Increment(ref _currentVersion);

        _delayCts?.Cancel();
        _executeCts?.Cancel();
    }

    private Task SafeDelayAsync(int milliseconds, CancellationToken token)
    {
        if (token.IsCancellationRequested) return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var timer = new Timer(_ => tcs.TrySetResult(), null, milliseconds, Timeout.Infinite);
        var ctr = token.Register(() => tcs.TrySetResult());

        tcs.Task.ContinueWith(_ =>
        {
            timer.Dispose();
            ctr.Dispose();
        }, TaskContinuationOptions.ExecuteSynchronously);

        return tcs.Task;
    }

    public async Task RunAsync(Action action, int? delayMilliseconds = null)
    {
        var expectedVersion = Interlocked.Increment(ref _currentVersion);

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = new CancellationTokenSource();

        await SafeDelayAsync(delayMilliseconds ?? (int)Time.DebounceTimeout.TotalMilliseconds, _delayCts.Token);

        if (Interlocked.Read(ref _currentVersion) == expectedVersion && !_delayCts.IsCancellationRequested) action();
    }

    public async Task RunAsync(Func<Task> action, int? delayMilliseconds = null)
    {
        var expectedVersion = Interlocked.Increment(ref _currentVersion);

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = new CancellationTokenSource();

        await SafeDelayAsync(delayMilliseconds ?? (int)Time.DebounceTimeout.TotalMilliseconds, _delayCts.Token);

        if (Interlocked.Read(ref _currentVersion) == expectedVersion && !_delayCts.IsCancellationRequested)
            await action();
    }

    public async Task RunAsync(Func<CancellationToken, Task> action, int? delayMilliseconds = null)
    {
        var expectedVersion = Interlocked.Increment(ref _currentVersion);

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = new CancellationTokenSource();

        await SafeDelayAsync(delayMilliseconds ?? (int)Time.DebounceTimeout.TotalMilliseconds, _delayCts.Token);

        if (Interlocked.Read(ref _currentVersion) == expectedVersion && !_delayCts.IsCancellationRequested)
        {
            _executeCts?.Cancel();
            _executeCts?.Dispose();

            _executeCts = new CancellationTokenSource();
            var token = _executeCts.Token;

            try
            {
                await action(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Debouncer Execute Error: {ex.Message}");
            }
        }
    }
}