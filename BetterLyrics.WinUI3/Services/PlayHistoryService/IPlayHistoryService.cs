using BetterLyrics.WinUI3.Models.Entities;
using BetterLyrics.WinUI3.Models.Stats;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.PlayHistoryService
{
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
}
