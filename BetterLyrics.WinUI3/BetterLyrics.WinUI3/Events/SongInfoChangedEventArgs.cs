// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Events
{
    public class SongInfoChangedEventArgs(SongInfo? songInfo) : EventArgs
    {
        public SongInfo? SongInfo { get; set; } = songInfo;
    }
}
