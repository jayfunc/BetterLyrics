using Microsoft.UI.Xaml;
using System;

namespace BetterLyrics.WinUI3.Hooks
{
    public class TaskbarHook
    {
        private readonly DispatcherTimer _timer;

        public Action<EventArgs> OnTaskbarBoundsChanged;

        public TaskbarHook()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
        }

    }
}
