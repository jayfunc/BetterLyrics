// 2025/6/23 by Zhe Fang

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsChar
    {
        public int? EndMs { get; set; }
        public int StartIndex { get; set; }
        public int StartMs { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
