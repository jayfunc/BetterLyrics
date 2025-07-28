using ATL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class PlayQueueItem
    {
        public Track Track { get; set; }

        public PlayQueueItem(Track track)
        {
            Track = track;
        }
    }
}
