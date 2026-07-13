using System.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Helpers;

public class MemoryLeakDetector
{
    private static readonly List<(WeakReference Reference, string Name)> _watchedObjects = [];

    private static readonly ILogger<MemoryLeakDetector> _logger =
        Ioc.Default.GetRequiredService<ILogger<MemoryLeakDetector>>();

    public static void Track(object target)
    {
        if (target == null) return;

        var name = target.GetType().Name;
        var hashCode = target.GetHashCode();
        name = $"{name}({hashCode})";

        lock (_watchedObjects)
        {
            _watchedObjects.Add((new WeakReference(target), name));
        }

        Debug.WriteLine($"[MemoryLeakDetector] GC is preparing: {name}");
        _logger.LogInformation("[MemoryLeakDetector] GC is preparing: {Name}", name);
    }

    public static async Task CheckLeaksAsync()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Task.Delay(100);

        lock (_watchedObjects)
        {
            var aliveObjects = new List<(WeakReference Reference, string Name)>();
            var deadObjects = new List<(WeakReference Reference, string Name)>();

            foreach (var item in _watchedObjects)
                if (item.Reference.IsAlive)
                {
                    aliveObjects.Add(item);

                    Debug.WriteLine($"[MemoryLeakDetector] GC failed, object is still alive: {item.Name}");
                    _logger.LogWarning("[MemoryLeakDetector] GC failed, object is still alive: {Name}", item.Name);
                }
                else
                {
                    deadObjects.Add(item);
                }

            foreach (var dead in deadObjects)
            {
                _watchedObjects.Remove(dead);

                Debug.WriteLine($"[MemoryLeakDetector] GC completed: {dead.Name}");
                _logger.LogInformation("[MemoryLeakDetector] GC completed: {Name}", dead.Name);
            }
        }
    }

    public static void ScheduleCheck(int delayMs = 3000)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delayMs);
            await CheckLeaksAsync();
        });
    }
}