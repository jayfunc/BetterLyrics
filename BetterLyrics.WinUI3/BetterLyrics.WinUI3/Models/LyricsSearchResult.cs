using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsSearchResult
    {
        public LyricsSearchProvider? Provider { get; set; }

        public string? Raw { get; set; }

        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }

        public bool IsFound => !string.IsNullOrEmpty(Raw);

        public LyricsSearchProvider? ProviderIfFound => IsFound ? Provider : null; 

        public void CopyFromSongInfo(SongInfo songInfo)
        {
            Title = songInfo.Title;
            Artist = songInfo.Artist;
            Album = songInfo.Album;
        }
    }
}
