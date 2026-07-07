using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class AppUIThreadProvider : IAppUIThreadProvider
{
    public void Initialize(object? obj)
    {
        throw new NotImplementedException();
    }

    public void Execute(Action action)
    {
        Dispatcher.UIThread.Invoke(() => action());
    }

    public Task RunAsync(Action action)
    {
        var tcs = new TaskCompletionSource();
        Dispatcher.UIThread.Invoke(() =>
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