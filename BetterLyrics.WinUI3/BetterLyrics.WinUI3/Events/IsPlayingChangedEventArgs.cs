// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Events
{
    public class IsPlayingChangedEventArgs(bool isPlaying) : EventArgs
    {
        public bool IsPlaying { get; set; } = isPlaying;
    }
}
