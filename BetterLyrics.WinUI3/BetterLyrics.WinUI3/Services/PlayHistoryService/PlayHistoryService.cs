using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Stats;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.PlayHistoryService
{
    public class PlayHistoryService : IPlayHistoryService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;

        public PlayHistoryService()
        {
            _dbPath = PathHelper.PlayHistoryPath;
        }

        public async Task InitializeAsync()
        {
            if (_db != null) return;

            _db = new SQLiteAsyncConnection(_dbPath);
            await _db.CreateTableAsync<PlayHistoryItem>();
        }

        /// <summary>
        /// 添加一条播放记录
        /// </summary>
        public async Task AddLogAsync(PlayHistoryItem item)
        {
            await InitializeAsync();
            // 再次确保这里是 UTC 时间，方便跨时区统计
            if (item.StartedAt.Kind != DateTimeKind.Utc)
            {
                item.StartedAt = item.StartedAt.ToUniversalTime();
            }
            await _db.InsertAsync(item);
        }

        /// <summary>
        /// 获取最近的播放记录 (用于“最近播放”列表)
        /// </summary>
        public async Task<List<PlayHistoryItem>> GetRecentLogsAsync(int limit = 50)
        {
            await InitializeAsync();
            return await _db.Table<PlayHistoryItem>()
                            .OrderByDescending(x => x.StartedAt)
                            .Take(limit)
                            .ToListAsync();
        }

        /// <summary>
        /// 获取特定时间段的所有原始记录 (用于生成复杂的图表，如 Hourly Heatmap)
        /// </summary>
        public async Task<List<PlayHistoryItem>> GetLogsByDateRangeAsync(DateTime start, DateTime end)
        {
            await InitializeAsync();
            return await _db.Table<PlayHistoryItem>()
                            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
                            .ToListAsync();
        }

        /// <summary>
        /// 统计时间段内 Top N 歌曲
        /// </summary>
        public async Task<List<SongPlayCount>> GetTopSongsAsync(DateTime start, DateTime end, int limit = 10)
        {
            await InitializeAsync();

            // SQLite 语法: Group By Title 和 Artist
            string query = @"
                SELECT Title, Artist, COUNT(*) as PlayCount 
                FROM PlayHistory 
                WHERE StartedAt >= ? AND StartedAt <= ? 
                GROUP BY Title, Artist 
                ORDER BY PlayCount DESC 
                LIMIT ?";

            return await _db.QueryAsync<SongPlayCount>(query, start, end, limit);
        }

        /// <summary>
        /// 统计时间段内 Top N 歌手
        /// </summary>
        public async Task<List<ArtistPlayCount>> GetTopArtistsAsync(DateTime start, DateTime end, int limit = 10)
        {
            await InitializeAsync();

            // 同时统计播放次数和总播放时长(秒)
            string query = @"
                SELECT Artist, COUNT(*) as PlayCount, SUM(DurationPlayedMs)/1000.0 as TotalDurationSeconds
                FROM PlayHistory 
                WHERE StartedAt >= ? AND StartedAt <= ? 
                GROUP BY Artist 
                ORDER BY PlayCount DESC 
                LIMIT ?";

            return await _db.QueryAsync<ArtistPlayCount>(query, start, end, limit);
        }

        /// <summary>
        /// 获取总听歌时长
        /// </summary>
        public async Task<TimeSpan> GetTotalListeningDurationAsync(DateTime start, DateTime end)
        {
            await InitializeAsync();

            var result = await _db.ExecuteScalarAsync<double>(
                "SELECT SUM(DurationPlayedMs) FROM PlayHistory WHERE StartedAt >= ? AND StartedAt <= ?",
                start, end);

            return TimeSpan.FromMilliseconds(result);
        }

        /// <summary>
        /// 获取播放器来源分布
        /// </summary>
        public async Task<List<PlayerStats>> GetPlayerDistributionAsync(DateTime start, DateTime end)
        {
            await InitializeAsync();

            string query = @"
                SELECT PlayerId, COUNT(*) as Count
                FROM PlayHistory
                WHERE StartedAt >= ? AND StartedAt <= ?
                GROUP BY PlayerId
                ORDER BY Count DESC";

            return await _db.QueryAsync<PlayerStats>(query, start, end);
        }

        public async Task DeleteLogAsync(int id)
        {
            await InitializeAsync();
            await _db.DeleteAsync<PlayHistoryItem>(id);
        }

        public async Task ClearHistoryAsync()
        {
            await InitializeAsync();
            await _db.DeleteAllAsync<PlayHistoryItem>();
        }

        public async Task GenerateTestDataAsync(int count = 100)
        {
            var random = new Random();

            var presetSongs = new List<(string Title, string Artist, string Album)>
            {
                // --- 欧美流行 ---
                ("Anti-Hero", "Taylor Swift", "Midnights"),
                ("Cruel Summer", "Taylor Swift", "Lover"),
                ("Blank Space", "Taylor Swift", "1989"),
                ("As It Was", "Harry Styles", "Harry's House"),
                ("Late Night Talking", "Harry Styles", "Harry's House"),
                ("Die For You", "The Weeknd", "Starboy"),
                ("Blinding Lights", "The Weeknd", "After Hours"),
                ("Starboy", "The Weeknd", "Starboy"),
                ("Shape of You", "Ed Sheeran", "Divide"),
                ("Bad Guy", "Billie Eilish", "When We All Fall Asleep, Where Do We Go?"),
                ("Flowers", "Miley Cyrus", "Endless Summer Vacation"),
                ("Stay", "The Kid LAROI & Justin Bieber", "F*ck Love 3: Over You"),
        
                // --- 华语流行 ---
                ("七里香", "周杰伦", "七里香"),
                ("晴天", "周杰伦", "叶惠美"),
                ("一路向北", "周杰伦", "11月的肖邦"),
                ("告白气球", "周杰伦", "周杰伦的床边故事"),
                ("十年", "陈奕迅", "黑·白·灰"),
                ("富士山下", "陈奕迅", "What's Going On...?"),
                ("孤勇者", "陈奕迅", "孤勇者"),
                ("修炼爱情", "林俊杰", "因你而在"),
                ("江南", "林俊杰", "第二天堂"),
                ("光年之外", "G.E.M. 邓紫棋", "摩天动物园"),
                ("泡沫", "G.E.M. 邓紫棋", "Xposed"),
                ("因为爱情", "王菲 & 陈奕迅", "Stranger Under My Skin"),
                ("红豆", "王菲", "唱游"),
        
                // --- 摇滚/经典 ---
                ("Bohemian Rhapsody", "Queen", "A Night at the Opera"),
                ("Don't Stop Me Now", "Queen", "Jazz"),
                ("Numb", "Linkin Park", "Meteora"),
                ("In the End", "Linkin Park", "Hybrid Theory"),
                ("Yellow", "Coldplay", "Parachutes"),
                ("Viva La Vida", "Coldplay", "Viva La Vida"),
                ("Smells Like Teen Spirit", "Nirvana", "Nevermind"),
                ("Hotel California", "Eagles", "Hotel California"),

                // --- 日韩/二次元 ---
                ("Lemon", "米津玄師", "Lemon"),
                ("Kick Back", "米津玄師", "KICK BACK"),
                ("アイドル", "YOASOBI", "アイドル"),
                ("夜に駆ける", "YOASOBI", "THE BOOK"),
                ("First Love", "宇多田ヒカル", "First Love"),
                ("Dynamite", "BTS", "BE"),
                ("Butter", "BTS", "Butter"),
                ("How You Like That", "BLACKPINK", "The Album"),
                ("Ditto", "NewJeans", "OMG"),
        
                // --- 电子/纯音乐 ---
                ("Get Lucky", "Daft Punk", "Random Access Memories"),
                ("The Nights", "Avicii", "The Days / Nights"),
                ("Summer", "Calvin Harris", "Motion"),
            };

            var playerIds = new[] {
                "Spotify", "Spotify", "Spotify",
                "MusicBee", "MusicBee",
                "QQMusic",
                "NeteaseCloudMusic",
                "AppleMusic"
            };

            int addedCount = 0;

            while (addedCount < count)
            {
                var song = presetSongs[random.Next(presetSongs.Count)];

                var playerId = playerIds[random.Next(playerIds.Length)];

                // 生成时间：过去 365 天内均匀分布
                var daysBack = random.Next(0, 365);
                var hoursBack = random.Next(0, 24);
                var minutesBack = random.Next(0, 60);
                var secondsBack = random.Next(0, 60);

                var startedAt = DateTime.Now
                    .AddDays(-daysBack)
                    .AddHours(-hoursBack)
                    .AddMinutes(-minutesBack)
                    .AddSeconds(-secondsBack);

                // 歌曲总时长 (3分钟 - 5分钟)
                var totalDurationMs = random.Next(180, 300) * 1000.0;

                // 模拟听歌习惯：
                // 70% 的概率是听完的 (0.9 - 1.0)
                // 20% 的概率是切歌 (0.3 - 0.8)
                // 10% 的概率是刚听就切了 (0.05 - 0.3)
                double playedRatio;
                double roll = random.NextDouble();
                if (roll > 0.3) playedRatio = 0.9 + (random.NextDouble() * 0.1); // 听完
                else if (roll > 0.1) playedRatio = 0.3 + (random.NextDouble() * 0.5); // 听一半
                else playedRatio = 0.05 + (random.NextDouble() * 0.25); // 秒切

                var playedDurationMs = totalDurationMs * playedRatio;

                if (playedDurationMs >= (totalDurationMs / 2))
                {
                    var item = new PlayHistoryItem
                    {
                        Title = song.Title,
                        Artist = song.Artist,
                        Album = song.Album,
                        PlayerId = playerId,
                        StartedAt = startedAt,
                        TotalDurationMs = totalDurationMs,
                        DurationPlayedMs = playedDurationMs
                    };

                    await AddLogAsync(item);
                    addedCount++; // 只有成功写入才计数
                }
            }
        }

    }
}
