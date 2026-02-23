using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public class MemoryLeakDetector
    {
        private static readonly List<(WeakReference Reference, string Name)> _watchedObjects = [];
        private static readonly ILogger<MemoryLeakDetector> _logger = Ioc.Default.GetRequiredService<ILogger<MemoryLeakDetector>>();

        public static void Track(object target)
        {
            if (target == null) return;

            string name = target.GetType().Name;
            if (target is Microsoft.UI.Xaml.FrameworkElement fe && !string.IsNullOrEmpty(fe.Name))
            {
                name += $" ({fe.Name})";
            }

            lock (_watchedObjects)
            {
                _watchedObjects.Add((new WeakReference(target), name));
            }

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
                {
                    if (item.Reference.IsAlive)
                    {
                        aliveObjects.Add(item);
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
}
