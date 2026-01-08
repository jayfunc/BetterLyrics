// 2025/6/23 by Zhe Fang

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class LyricsSyllable
    {
        public string Text { get; set; } = string.Empty;
        public int Length => Text.Length;

        public int StartIndex { get; set; }
        public int EndIndex => StartIndex + Length - 1;

        public int StartMs { get; set; }
        public int? EndMs { get; set; }

        public int? DurationMs => EndMs - StartMs;
        public bool IsLongDuration => DurationMs >= 700;
    }
}
