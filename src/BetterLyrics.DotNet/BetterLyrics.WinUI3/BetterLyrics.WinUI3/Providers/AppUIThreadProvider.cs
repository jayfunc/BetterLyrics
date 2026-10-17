using BetterLyrics.Core.Interfaces.Providers;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class AppUIThreadProvider : IAppUIThreadProvider
{
    private static DispatcherQueue? _dispatcherQueue;

    public void Initialize(object? obj)
    {
        _dispatcherQueue = obj as DispatcherQueue ?? throw new ArgumentNullException(nameof(obj));
    }

    public void Execute(Action action)
    {
        if (_dispatcherQueue == null) throw new InvalidOperationException("DispatcherQueue is not initialized.");
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }
        _dispatcherQueue.TryEnqueue(() => action());
    }

    public Task RunAsync(Action action)
    {
        if (_dispatcherQueue == null) throw new InvalidOperationException("DispatcherQueue is not initialized.");
        var tcs = new TaskCompletionSource();
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}