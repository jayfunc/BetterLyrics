using SQLite;

namespace BetterLyrics.WinUI3.Models
{
    public class LocalFileRecord
    {
        public string? Path { get; set; }
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public byte[]? AlbumArt { get; set; }
        public string? LyricsRaw { get; set; }
        public int? Duration { get; set; }
    }
}
