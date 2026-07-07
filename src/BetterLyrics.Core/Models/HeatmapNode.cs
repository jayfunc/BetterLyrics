namespace BetterLyrics.Core.Models;

public class HeatmapNode
{
    public DateTime Date { get; set; }
    public int PlayCount { get; set; }
    public int Level { get; set; }
    public bool IsEmpty { get; set; }

    public double Opacity => IsEmpty ? 0.0 : Level == 0 ? 0.05 : Level * 0.25;
}