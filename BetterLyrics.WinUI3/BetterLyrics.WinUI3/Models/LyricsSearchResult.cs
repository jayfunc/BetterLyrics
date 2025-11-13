using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsSearchResult
    {
        public bool IsFound => !string.IsNullOrEmpty(Raw);
        public LyricsSearchProvider? Provider { get; set; }

        public string? Raw { get; set; }

        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
    }
}
