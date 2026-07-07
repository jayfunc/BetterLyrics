namespace BetterLyrics.Core.Models.Lyrics;

public class BaseLyrics
{
    public int StartMs { get; set; }
    public int? EndMs { get; set; } = null;
    public int DurationMs => Math.Max((EndMs ?? 0) - StartMs, 0);

    public string Text { get; set; } = "";
    public int Length => Text.Length;

    public int StartIndex { get; set; }
    public int EndIndex => StartIndex + Length - 1;
}