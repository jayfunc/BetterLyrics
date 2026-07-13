using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Stats;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Implementations.Services;

public class PlayHistoryService : IPlayHistoryService
{
    private readonly IDbContextFactory<PlayHistoryDbContext> _contextFactory;

    public PlayHistoryService(IDbContextFactory<PlayHistoryDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddLogAsync(PlayHistoryItem item)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // 确保 UTC
        if (item.StartedAt.Kind != DateTimeKind.Utc) item.StartedAt = item.StartedAt.ToUniversalTime();

        context.PlayHistory.Add(item);
        await context.SaveChangesAsync();
    }

    public async Task<List<PlayHistoryItem>> GetRecentLogsAsync(int limit = 50)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayHistory
            .AsNoTracking() // 读操作，不需要追踪状态，提升性能
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<PlayHistoryItem>> GetLogsByDateRangeAsync(DateTime start, DateTime end)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayHistory
            .AsNoTracking()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToListAsync();
    }

    public async Task<List<SongPlayCount>> GetTopSongsAsync(DateTime start, DateTime end, int limit = 10)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // EF Core 会自动将这个 LINQ 翻译成高效的 GROUP BY SQL
        return await context.PlayHistory
            .AsNoTracking()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .GroupBy(x => new { x.Title, x.Artist }) // 组合分组
            .Select(g => new SongPlayCount
            {
                Title = g.Key.Title,
                Artist = g.Key.Artist,
                PlayCount = g.Count()
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ArtistPlayCount>> GetTopArtistsAsync(DateTime start, DateTime end, int limit = 10)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayHistory
            .AsNoTracking()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .GroupBy(x => x.Artist)
            .Select(g => new ArtistPlayCount
            {
                Artist = g.Key,
                PlayCount = g.Count(),
                // 注意：SQLite 存储 double 精度，这里求和后转秒
                TotalDurationSeconds = g.Sum(x => x.DurationPlayedMs) / 1000.0
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<TimeSpan> GetTotalListeningDurationAsync(DateTime start, DateTime end)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var totalMs = await context.PlayHistory
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .SumAsync(x => Math.Min(x.DurationPlayedMs, x.TotalDurationMs)); // 防止超过歌曲本身时长

        return TimeSpan.FromMilliseconds(totalMs);
    }

    public async Task<List<PlayerStats>> GetPlayerDistributionAsync(DateTime start, DateTime end)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayHistory
            .AsNoTracking()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .GroupBy(x => x.PlayerId)
            .Select(g => new PlayerStats
            {
                PlayerId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    public async Task DeleteLogAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // EF Core 删除需要先查询，或者使用 ExecuteDeleteAsync (EF Core 7+)
        // 写法 1 (传统):
        // var item = await context.PlayHistory.FindAsync(id);
        // if (item != null) { context.PlayHistory.Remove(item); await context.SaveChangesAsync(); }

        // 写法 2 (EF Core 7.0+ 高效写法，直接生成 DELETE SQL):
        await context.PlayHistory
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();
    }

    public async Task ClearHistoryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // 高效清空表
        await context.PlayHistory.ExecuteDeleteAsync();
    }

    public async Task GenerateTestDataAsync(int count = 100)
    {
        // 这里的逻辑稍微重构了一下，使用批量插入提升性能
        var random = new Random();
        var presetSongs = new List<(string Title, string Artist, string Album)>
        {
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
            ("Bohemian Rhapsody", "Queen", "A Night at the Opera"),
            ("Don't Stop Me Now", "Queen", "Jazz"),
            ("Numb", "Linkin Park", "Meteora"),
            ("In the End", "Linkin Park", "Hybrid Theory"),
            ("Yellow", "Coldplay", "Parachutes"),
            ("Viva La Vida", "Coldplay", "Viva La Vida"),
            ("Smells Like Teen Spirit", "Nirvana", "Nevermind"),
            ("Hotel California", "Eagles", "Hotel California"),
            ("Lemon", "米津玄師", "Lemon"),
            ("Kick Back", "米津玄師", "KICK BACK"),
            ("アイドル", "YOASOBI", "アイドル"),
            ("夜に駆ける", "YOASOBI", "THE BOOK"),
            ("First Love", "宇多田ヒカル", "First Love"),
            ("Dynamite", "BTS", "BE"),
            ("Butter", "BTS", "Butter"),
            ("How You Like That", "BLACKPINK", "The Album"),
            ("Ditto", "NewJeans", "OMG"),
            ("Get Lucky", "Daft Punk", "Random Access Memories"),
            ("The Nights", "Avicii", "The Days / Nights"),
            ("Summer", "Calvin Harris", "Motion")
        };

        var playerIds = new[]
        {
            //PlayerId.Spotify, PlayerId.Spotify, PlayerId.Spotify,
            //PlayerId.MusicBee, PlayerId.MusicBee,
            //PlayerId.QQMusic,
            //PlayerId.NetEaseCloudMusic,
            //PlayerId.AppleMusic,
            ""
        };

        var batchList = new List<PlayHistoryItem>();

        // 我们尝试生成 count 条有效数据
        // 为了防止死循环，加个硬上限
        var attempts = 0;
        while (batchList.Count < count && attempts < count * 5)
        {
            attempts++;
            var song = presetSongs[random.Next(presetSongs.Count)];
            var playerId = playerIds[random.Next(playerIds.Length)];

            var daysBack = random.Next(0, 365);
            var hoursBack = random.Next(0, 24);
            var minutesBack = random.Next(0, 60);
            var secondsBack = random.Next(0, 60);

            var startedAt = DateTime.UtcNow // 直接用 UTC
                .AddDays(-daysBack)
                .AddHours(-hoursBack)
                .AddMinutes(-minutesBack)
                .AddSeconds(-secondsBack);

            var totalDurationMs = random.Next(180, 300) * 1000.0;
            double playedRatio;
            var roll = random.NextDouble();

            if (roll > 0.3) playedRatio = 0.9 + random.NextDouble() * 0.1;
            else if (roll > 0.1) playedRatio = 0.3 + random.NextDouble() * 0.5;
            else playedRatio = 0.05 + random.NextDouble() * 0.25;

            var playedDurationMs = totalDurationMs * playedRatio;

            // 只有听了一半以上的才算作记录
            if (playedDurationMs >= totalDurationMs / 2)
                batchList.Add(new PlayHistoryItem
                {
                    Title = song.Title,
                    Artist = song.Artist,
                    Album = song.Album,
                    PlayerId = playerId,
                    StartedAt = startedAt,
                    TotalDurationMs = totalDurationMs,
                    DurationPlayedMs = playedDurationMs
                });
        }

        if (batchList.Count > 0)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.PlayHistory.AddRangeAsync(batchList);
            await context.SaveChangesAsync();
        }
    }
}