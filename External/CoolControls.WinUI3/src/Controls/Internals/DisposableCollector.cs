using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoolControls.WinUI3.Controls.Internals
{
    internal class DisposableCollector : IDisposable
    {
        private bool disposedValue;

        private List<IDisposable> objects = new List<IDisposable>();

        public void Add(IDisposable item)
        {
            objects.Add(item);
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                var objects = Interlocked.Exchange(ref this.objects, null!);

                for (int i = objects.Count - 1; i >= 0; i--)
                {
                    objects[i].Dispose();
                }

                disposedValue = true;
            }
        }

    }

    internal static class DisposableCollectorExtensions
    {
        public static T TraceDisposable<T>(this T obj, DisposableCollector disposableCollector) where T : IDisposable
        {
            disposableCollector.Add(obj);
            return obj;
        }
    }
}
