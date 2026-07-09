using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class AppUIThreadProvider : IAppUIThreadProvider
{
    public void Initialize(object? obj)
    {
        // 如果你的框架在这个阶段不需要注入任何上下文，建议留空。
        // 抛出 NotImplementedException 可能会在容器初始化时导致应用直接崩溃。
    }

    public void Execute(Action action)
    {
        // 使用 Post 放入队列执行（非阻塞 / Fire-and-forget）。
        // 相比 Invoke，Post 更安全，能有效避免在某些同步上下文中可能引发的死锁。
        Dispatcher.UIThread.Post(action);

        // 注：如果你确实需要阻塞当前线程直到 UI 操作完成，可以保留原来的 Dispatcher.UIThread.Invoke(action);
    }

    public async Task RunAsync(Action action)
    {
        // Avalonia 的 InvokeAsync 直接返回 Task，原生支持 async/await
        // 它内部已经帮你处理好了 TaskCompletionSource 和异常抛出的逻辑
        await Dispatcher.UIThread.InvokeAsync(action);
    }
}