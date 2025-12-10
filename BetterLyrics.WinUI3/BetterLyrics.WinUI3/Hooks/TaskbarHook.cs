using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml;
using System;
using Vanara.Windows.Shell.TaskBar;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Hooks
{
    public class TaskbarHook
    {
        private readonly DispatcherTimer _timer;
        private Rect _bounds = new();

        public Action<TaskbarBoundsChangedEventArgs>? OnTaskbarBoundsChanged;

        public TaskbarHook()
        {
            _timer = new();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
            var newBounds = Taskbar.Bounds.ToRect();
            if (newBounds != _bounds)
            {
                _bounds = newBounds;
                OnTaskbarBoundsChanged?.Invoke(new TaskbarBoundsChangedEventArgs(_bounds));
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

    }
}
