using BetterLyrics.WinUI3.Models.Lyrics;
using System;

namespace BetterLyrics.WinUI3.Events
{
    public class LyricsChangedEventArgs(LyricsData? lyricsData) : EventArgs
    {
        public LyricsData? LyricsData { get; } = lyricsData;
    }
}
