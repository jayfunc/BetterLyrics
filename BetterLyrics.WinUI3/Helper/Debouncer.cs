using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class Debouncer
    {
        private long _currentVersion = 0;

        private CancellationTokenSource? _executeCts;

        public async Task RunAsync(Action action, int? delayMilliseconds = null)
        {
            long expectedVersion = Interlocked.Increment(ref _currentVersion);
            await Task.Delay(delayMilliseconds ?? (int)Constants.Time.DebounceTimeout.TotalMilliseconds);

            if (Interlocked.Read(ref _currentVersion) == expectedVersion)
            {
                action();
            }
        }

        public async Task RunAsync(Func<Task> action, int? delayMilliseconds = null)
        {
            long expectedVersion = Interlocked.Increment(ref _currentVersion);
            await Task.Delay(delayMilliseconds ?? (int)Constants.Time.DebounceTimeout.TotalMilliseconds);

            if (Interlocked.Read(ref _currentVersion) == expectedVersion)
            {
                await action();
            }
        }

        public async Task RunAsync(Func<CancellationToken, Task> action, int? delayMilliseconds = null)
        {
            long expectedVersion = Interlocked.Increment(ref _currentVersion);

            await Task.Delay(delayMilliseconds ?? (int)Constants.Time.DebounceTimeout.TotalMilliseconds);

            if (Interlocked.Read(ref _currentVersion) == expectedVersion)
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
                    System.Diagnostics.Debug.WriteLine($"Debouncer Execute Error: {ex.Message}");
                }
            }
        }
    }
}