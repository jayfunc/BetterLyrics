namespace BetterLyrics.Core.Models.Stats;

public class PlayerStats
{
    public string PlayerId { get; set; }
    public int Count { get; set; }

    public double DisplayWidth => TotalCount > 0 ? Count / (double)TotalCount * 150 : 0;

    public int TotalCount { get; set; }
}