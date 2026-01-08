namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class LyricsChar
    {
        public int StartMs { get; set; }
        public int EndMs { get; set; }
        public int DurationMs => EndMs - StartMs;

        public string Text { get; set; } = "";
        public int Index { get; set; }
    }
}
