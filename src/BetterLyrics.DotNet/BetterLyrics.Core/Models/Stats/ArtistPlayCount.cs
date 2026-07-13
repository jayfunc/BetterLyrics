namespace BetterLyrics.Core.Models.Stats;

public class ArtistPlayCount
{
    public string Artist { get; set; }
    public int PlayCount { get; set; }
    public double TotalDurationSeconds { get; set; }
}