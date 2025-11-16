using BetterLyrics.WinUI3.Enums;
using NTextCat.Commons;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsSearchResult
    {
        public LyricsSearchProvider? Provider { get; set; }

        public string? Raw { get; set; }

        public string? Title { get; set; }
        public string[]? Artists { get; set; }
        public string? Album { get; set; }

        public bool IsFound => !string.IsNullOrEmpty(Raw);

        public LyricsSearchProvider? ProviderIfFound => IsFound ? Provider : null;

        public string? DisplayArtists => Artists?.Join("; ");

        public void CopyFromSongInfo(SongInfo songInfo)
        {
            Title = songInfo.Title;
            Artists = songInfo.Artists;
            Album = songInfo.Album;
        }
    }
}
