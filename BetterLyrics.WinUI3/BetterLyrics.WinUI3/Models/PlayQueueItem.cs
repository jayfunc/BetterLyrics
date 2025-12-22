namespace BetterLyrics.WinUI3.Models
{
    public class PlayQueueItem
    {
        public ExtendedTrack Track { get; set; }

        public PlayQueueItem(ExtendedTrack track)
        {
            Track = track;
        }
    }
}
