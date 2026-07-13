using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Stats;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IPlayHistoryService
{
    Task AddLogAsync(PlayHistoryItem item);
    Task<List<PlayHistoryItem>> GetRecentLogsAsync(int limit = 50);
    Task<List<PlayHistoryItem>> GetLogsByDateRangeAsync(DateTime start, DateTime end);

    Task<List<SongPlayCount>> GetTopSongsAsync(DateTime start, DateTime end, int limit = 10);
    Task<List<ArtistPlayCount>> GetTopArtistsAsync(DateTime start, DateTime end, int limit = 10);
    Task<TimeSpan> GetTotalListeningDurationAsync(DateTime start, DateTime end);
    Task<List<PlayerStats>> GetPlayerDistributionAsync(DateTime start, DateTime end);

    Task DeleteLogAsync(int id);
    Task ClearHistoryAsync();
    Task GenerateTestDataAsync(int count = 100);
}