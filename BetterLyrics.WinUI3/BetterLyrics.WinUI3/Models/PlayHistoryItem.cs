using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    [Table("PlayHistory")]
    public class PlayHistoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed] public string Title { get; set; }
        [Indexed] public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtHash { get; set; } // 不同播放器来源的图可能不同

        [Indexed] public DateTime StartedAt { get; set; }
        public double DurationPlayedMs { get; set; }
        public double TotalDurationMs { get; set; }

        [Indexed]
        public string PlayerID { get; set; } // "Spotify", "QQMusic", etc.
    }
}
