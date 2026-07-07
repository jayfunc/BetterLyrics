namespace BetterLyrics.Core.Models;

public class PlayQueueItem
{
    public PlayQueueItem(ExtendedTrack track)
    {
        Track = track;
    }

    public ExtendedTrack Track { get; set; }
}