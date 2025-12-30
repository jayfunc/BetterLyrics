using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Stats
{
    public class SongPlayCount
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string AlbumArtHash { get; set; }
        public int PlayCount { get; set; }
    }
}
