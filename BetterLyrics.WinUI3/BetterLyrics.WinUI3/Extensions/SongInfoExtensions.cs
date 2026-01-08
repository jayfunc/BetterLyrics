using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Entities;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class SongInfoExtensions
    {
        public static SongInfo Placeholder => new()
        {
            Title = "N/A",
            Album = "N/A",
            Artist = "N/A",
        };

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

            public PlayHistoryItem? ToPlayHistoryItem(double actualPlayedMs)
            {
                if (songInfo == null) return null;

                return new PlayHistoryItem
                {
                    Title = songInfo.Title,
                    Artist = songInfo.Artist,
                    Album = songInfo.Album,
                    PlayerId = songInfo.PlayerId ?? "N/A",
                    TotalDurationMs = songInfo.DurationMs,
                    DurationPlayedMs = actualPlayedMs,
                    StartedAt = DateTime.FromBinary(songInfo.StartedAt)
                };
            }

            public string GetCacheKey()
            {
                string title = songInfo.Title?.Trim() ?? "";
                string album = songInfo.Album?.Trim() ?? "";

                string artists = songInfo.Artist?.Trim() ?? "";

                long seconds = (long)Math.Round(songInfo.Duration);
                string durationPart = seconds.ToString(CultureInfo.InvariantCulture);

                string rawKey = $"{title}|{artists}|{album}|{durationPart}";

                using var sha256 = SHA256.Create();
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
                return Convert.ToHexString(bytes);
            }
        }
    }
}
