using BetterLyrics.Core.Events;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BetterLyrics.Core.Helpers;

public class MemoryLeakDetector
{
    private static readonly List<(WeakReference Reference, string Name)> _watchedObjects = [];

    private static readonly ILogger<MemoryLeakDetector> _logger = Ioc.Default.GetRequiredService<ILogger<MemoryLeakDetector>>();

    /// <summary>
    /// Raised when an object starts being watched. UI tests use this to keep only weak
    /// references to the objects that belong to the window being closed.
    /// </summary>
    public static event EventHandler<MemoryLeakObjectTrackedEventArgs>? ObjectTracked;

    public static void Track(object target)
    {
        if (target == null) return;

        var name = target.GetType().Name;
        var hashCode = target.GetHashCode();
        name = $"{name}({hashCode})";

        var reference = new WeakReference(target);

        lock (_watchedObjects)
        {
            foreach (var (watchedReference, _) in _watchedObjects)
            {
                if (ReferenceEquals(watchedReference.Target, target)) return;
            }

            _watchedObjects.Add((reference, name));
        }

        Debug.WriteLine($"[MemoryLeakDetector] GC is preparing: {name}");
        _logger.LogInformation("[MemoryLeakDetector] GC is preparing: {Name}", name);

        ObjectTracked?.Invoke(null, new MemoryLeakObjectTrackedEventArgs(name, reference));
    }

    private static async Task CheckAsync()
    {
        CollectAndWait();

        await Task.Delay(100);

        lock (_watchedObjects)
        {
            Debug.WriteLine($"[MemoryLeakDetector] === Checking watched objects ===");

            var aliveObjects = new List<(WeakReference Reference, string Name)>();
            var deadObjects = new List<(WeakReference Reference, string Name)>();

            foreach (var item in _watchedObjects)
            {
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
            }

            foreach (var dead in deadObjects)
            {
                _watchedObjects.Remove(dead);

                Debug.WriteLine($"[MemoryLeakDetector] GC completed: {dead.Name}");
                _logger.LogInformation("[MemoryLeakDetector] GC completed: {Name}", dead.Name);
            }
        }
    }

    private static void CollectAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static void ScheduleCheck(int delayMs = 3000)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delayMs);
            await CheckAsync();
        });
    }
}