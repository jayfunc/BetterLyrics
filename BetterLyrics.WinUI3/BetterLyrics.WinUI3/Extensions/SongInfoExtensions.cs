using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class SongInfoExtensions
    {
        extension(SongInfo songInfo)
        {
            public SongInfo WithTitle(string value)
            {
                songInfo.Title = value;
                return songInfo;
            }

            public SongInfo WithArtist(string value)
            {
                songInfo.Artist = value;
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
