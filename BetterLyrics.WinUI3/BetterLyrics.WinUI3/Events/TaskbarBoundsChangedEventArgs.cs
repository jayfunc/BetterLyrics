using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Events
{
    public class TaskbarBoundsChangedEventArgs : EventArgs
    {
        public Rect TaskbarBounds { get; }
        public TaskbarBoundsChangedEventArgs(Rect taskbarBounds)
        {
            TaskbarBounds = taskbarBounds;
        }
    }
}
