namespace BetterLyrics.Core.Models.Stats;

public class HourlyStatBar
{
    public int Hour { get; set; }
    public double NormalizedHeight { get; set; } // 0 - 100，用于UI高度
    public int RawCount { get; set; } // 实际播放数
    public string Label { get; set; } // Tooltip: "09:00 - 15 plays"
}