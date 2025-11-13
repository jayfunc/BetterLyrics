namespace BetterLyrics.WinUI3.Models
{
    public class TrimmedTrack
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }
        public string FilePath { get; set; }
        public int Duration { get; set; }
        public byte[]? AlbumArt { get; set; }
    }
}
