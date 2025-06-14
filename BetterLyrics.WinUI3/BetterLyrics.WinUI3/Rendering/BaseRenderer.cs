using System;

namespace BetterLyrics.WinUI3.Rendering
{
    public class BaseRenderer
    {
        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
    }
}
