// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Events
{
    public class TimelineChangedEventArgs(TimeSpan position, TimeSpan end) : EventArgs()
    {
        public TimeSpan Position { get; set; } = position;
        public TimeSpan End { get; set; } = end;
    }
}
