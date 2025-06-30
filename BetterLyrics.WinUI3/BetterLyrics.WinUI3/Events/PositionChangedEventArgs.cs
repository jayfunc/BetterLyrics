// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Events
{
    public class PositionChangedEventArgs(TimeSpan position) : EventArgs()
    {
        public TimeSpan Position { get; set; } = position;
    }
}
