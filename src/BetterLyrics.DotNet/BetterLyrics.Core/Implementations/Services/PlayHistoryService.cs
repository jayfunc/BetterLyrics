using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Stats;
using LiteDB;

namespace BetterLyrics.Core.Implementations.Services;

public class PlayHistoryService : IPlayHistoryService
{
    private readonly IDatabaseService _databaseService;

    public PlayHistoryService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        
        var col = _databaseService.PlayHistoryDb.GetCollection<PlayHistoryItem>("playHistory");
        col.EnsureIndex(x => x.Title);
        col.EnsureIndex(x => x.Artist);
        col.EnsureIndex(x => x.StartedAt);
        col.EnsureIndex(x => x.PlayerId);
    }

    private ILiteCollection<PlayHistoryItem> GetCollection()
    {
        return _databaseService.PlayHistoryDb.GetCollection<PlayHistoryItem>("playHistory");
    }

    public Task AddLogAsync(PlayHistoryItem item)
    {
        if (item.StartedAt.Kind != DateTimeKind.Utc) item.StartedAt = item.StartedAt.ToUniversalTime();

        var col = GetCollection();
        col.Insert(item);
        
        return Task.CompletedTask;
    }

    public Task<List<PlayHistoryItem>> GetRecentLogsAsync(int limit = 50)
    {
        var col = GetCollection();
        var result = col.Query()
            .OrderByDescending(x => x.StartedAt)
            .Limit(limit)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<List<PlayHistoryItem>> GetLogsByDateRangeAsync(DateTime start, DateTime end)
    {
        var col = GetCollection();
        var result = col.Query()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<List<SongPlayCount>> GetTopSongsAsync(DateTime start, DateTime end, int limit = 10)
    {
        var col = GetCollection();
        var logs = col.Query()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToList();

        var result = logs
            .GroupBy(x => new { x.Title, x.Artist })
            .Select(g => new SongPlayCount
            {
                Title = g.Key.Title,
                Artist = g.Key.Artist,
                PlayCount = g.Count()
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<List<ArtistPlayCount>> GetTopArtistsAsync(DateTime start, DateTime end, int limit = 10)
    {
        var col = GetCollection();
        var logs = col.Query()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToList();

        var result = logs
            .GroupBy(x => x.Artist)
            .Select(g => new ArtistPlayCount
            {
                Artist = g.Key,
                PlayCount = g.Count(),
                TotalDurationSeconds = g.Sum(x => x.DurationPlayedMs) / 1000.0
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<TimeSpan> GetTotalListeningDurationAsync(DateTime start, DateTime end)
    {
        var col = GetCollection();
        var totalMs = col.Query()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToList()
            .Sum(x => Math.Min(x.DurationPlayedMs, x.TotalDurationMs));

        return Task.FromResult(TimeSpan.FromMilliseconds(totalMs));
    }

    public Task<List<PlayerStats>> GetPlayerDistributionAsync(DateTime start, DateTime end)
    {
        var col = GetCollection();
        var logs = col.Query()
            .Where(x => x.StartedAt >= start && x.StartedAt <= end)
            .ToList();

        var result = logs
            .GroupBy(x => x.PlayerId)
            .Select(g => new PlayerStats
            {
                PlayerId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Task.FromResult(result);
    }

    public Task DeleteLogAsync(int id)
    {
        var col = GetCollection();
        col.Delete(id);
        return Task.CompletedTask;
    }

    public Task ClearHistoryAsync()
    {
        var col = GetCollection();
        col.DeleteAll();
        return Task.CompletedTask;
    }

    public Task GenerateTestDataAsync(int count = 100)
    {
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
            ("Stay", "The Kid LAROI & Justin Bieber", "F*ck Love 3: Over You")
        };

        var playerIds = new[] 
        { 
            "Sakawish.SaltPlayerforWindows_q65q631pyh094!SaltPlayerforWindows",
            "AppleInc.AppleMusicWin_nzyj5cx40ttqa!App",
            "37412.BetterLyrics_mxmjbjshrz3mm!App",
            "Chrome"
        };
        var batchList = new List<PlayHistoryItem>();
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

            var startedAt = DateTime.UtcNow
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
            var col = GetCollection();
            col.InsertBulk(batchList);
        }
        
        return Task.CompletedTask;
    }
}