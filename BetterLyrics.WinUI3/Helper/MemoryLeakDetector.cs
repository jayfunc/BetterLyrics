using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class MemoryLeakDetector
    {
        private static readonly List<(WeakReference Reference, string Name)> _watchedObjects = new();

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

            Debug.WriteLine($"[MemoryLeakDetector] 开始监视: {name}");
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
                        Debug.WriteLine($"[警告 - 可能泄漏] 对象仍存活: {item.Name}");
                    }
                    else
                    {
                        deadObjects.Add(item);
                    }
                }

                foreach (var dead in deadObjects)
                {
                    _watchedObjects.Remove(dead);
                    Debug.WriteLine($"[成功回收] {dead.Name}");
                }
            }
        }

        public static void ScheduleCheck(int delayMs = 3000)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                await CheckLeaksAsync();
            });
        }
    }
}
