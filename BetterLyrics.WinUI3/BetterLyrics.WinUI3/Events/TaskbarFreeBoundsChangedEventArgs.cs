using System;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Events
{
    public class TaskbarFreeBoundsChangedEventArgs : EventArgs
    {
        public Rect TaskbarFreeBounds { get; }
        public TaskbarFreeBoundsChangedEventArgs(Rect taskbarBounds)
        {
            TaskbarFreeBounds = taskbarBounds;
        }
    }
}
