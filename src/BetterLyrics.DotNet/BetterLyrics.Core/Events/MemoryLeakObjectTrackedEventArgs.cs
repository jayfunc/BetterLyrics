using BetterLyrics.Core.Helpers;

namespace BetterLyrics.Core.Events;

/// <summary>
/// Describes an object that was registered with <see cref="MemoryLeakDetector"/>.
/// The object itself is only referenced weakly so observing this event does not
/// prevent it from being garbage collected.
/// </summary>
public sealed class MemoryLeakObjectTrackedEventArgs : EventArgs
{
    public MemoryLeakObjectTrackedEventArgs(string name, WeakReference reference)
    {
        Name = name;
        Reference = reference;
    }

    public string Name { get; }

    public WeakReference Reference { get; }

    public bool IsAlive => Reference.IsAlive;
}
