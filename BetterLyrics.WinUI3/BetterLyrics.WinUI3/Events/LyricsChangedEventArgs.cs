using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class LyricsChangedEventArgs(LyricsData? lyricsData) : EventArgs
    {
        public LyricsData? LyricsData { get; } = lyricsData;
    }
}
