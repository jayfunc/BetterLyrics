using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class SongInfoExtensions
    {
        public static SongInfo Placeholder => new SongInfo
        {
            Title = "N/A",
            Album = "N/A",
            Artists = ["N/A"],
        };

        extension(SongInfo songInfo)
        {
            public SongInfo WithTitle(string value)
            {
                songInfo.Title = value;
                return songInfo;
            }

            public SongInfo WithArtist(string[] value)
            {
                songInfo.Artists = value;
                return songInfo;
            }

            public SongInfo WithAlbum(string value)
            {
                songInfo.Album = value;
                return songInfo;
            }
        }
    }
}
