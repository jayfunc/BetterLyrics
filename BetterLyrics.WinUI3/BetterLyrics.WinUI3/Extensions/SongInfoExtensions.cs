using BetterLyrics.WinUI3.Models;
using System;

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

            public PlayHistoryItem? ToPlayHistoryItem(double actualPlayedMs)
            {
                if (songInfo == null) return null;

                return new PlayHistoryItem
                {
                    Title = songInfo.Title,
                    Artist = songInfo.DisplayArtists,
                    Album = songInfo.Album,
                    PlayerID = songInfo.PlayerId ?? "N/A",
                    TotalDurationMs = songInfo.DurationMs,
                    DurationPlayedMs = actualPlayedMs,
                    StartedAt = DateTime.Now.AddMilliseconds(-actualPlayedMs)
                };
            }
        }
    }
}
