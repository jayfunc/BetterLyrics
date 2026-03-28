using System;
using System.Timers;

namespace BetterLyrics.WinUI3.Helper
{
    public partial class SimpleTimer : IDisposable
    {
        public delegate void TimeElapsedHandler();
        private readonly TimeElapsedHandler _onTimeElapsed;

        private readonly Timer _timer;

        public SimpleTimer(TimeElapsedHandler onTimeElapsed)
        {
            _onTimeElapsed = onTimeElapsed;

            _timer = new();
            _timer.Interval = 1000;
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _onTimeElapsed?.Invoke();
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }
    }
}