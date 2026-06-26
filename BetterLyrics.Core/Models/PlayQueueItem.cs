namespace BetterLyrics.Core.Models
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
