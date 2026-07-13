using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Extensions;

public static class SongInfoExtensions
{
    public static SongInfo Placeholder => new()
    {
        Title = "N/A",
        Album = "N/A",
        Artist = "N/A"
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

        public SongInfo WithSongId(string value)
        {
            songInfo.SongId = value;
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
            var title = songInfo.Title?.Trim() ?? "";
            var album = songInfo.Album?.Trim() ?? "";

            var artists = songInfo.Artist?.Trim() ?? "";

            var seconds = (long)Math.Round(songInfo.Duration);
            var durationPart = seconds.ToString(CultureInfo.InvariantCulture);

            var rawKey = $"{title}|{artists}|{album}|{durationPart}";

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToHexString(bytes);
        }
    }
}