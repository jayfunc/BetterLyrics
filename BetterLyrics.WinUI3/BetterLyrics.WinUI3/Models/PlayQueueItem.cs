using ATL;

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
