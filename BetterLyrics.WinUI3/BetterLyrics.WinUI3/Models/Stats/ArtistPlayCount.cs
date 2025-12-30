using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Stats
{
    public class ArtistPlayCount
    {
        public string Artist { get; set; }
        public int PlayCount { get; set; }
        public double TotalDurationSeconds { get; set; }
    }
}
