// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILyricsSearchService
{
    Task<LyricsCacheItem?> SearchSmartlyAsync(SongInfo songInfo, LyricsSearchType? lyricsSearchType,
        CancellationToken token);

    IAsyncEnumerable<LyricsCacheItem> SearchAllAsync(
        SongInfo songInfo,
        bool checkCache,
        CancellationToken cancellationToken = default);

    List<LyricsProvider> GetActiveProviders();
}