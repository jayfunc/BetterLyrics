// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using System;

namespace BetterLyrics.WinUI3.Events
{
    public class SongInfoChangedEventArgs(SongInfo? songInfo) : EventArgs
    {
        public SongInfo? SongInfo { get; set; } = songInfo;
    }
}
