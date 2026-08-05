using System;

namespace BetterLyrics.Core.Models.Stats;

public class HourlyActivityItem
{
    public string TimeLabel { get; set; } = string.Empty;
    public int Count { get; set; }
    public double HeightPercentage { get; set; }
    public string TooltipText { get; set; } = string.Empty;
}

public class PlayerSourceItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
