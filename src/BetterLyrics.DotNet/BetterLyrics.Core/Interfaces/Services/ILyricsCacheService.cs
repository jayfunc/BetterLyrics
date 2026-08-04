using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILyricsCacheService
{
    /// <summary>
    /// </summary>
    /// <param name="songInfo"></param>
    /// <param name="provider"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <returns></returns>
    Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsProvider provider, CancellationToken token);

    /// <summary>
    ///     Write or update cache to DB
    /// </summary>
    /// <param name="songInfo"></param>
    /// <param name="result"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <returns></returns>
    Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result, CancellationToken token);

    Task ClearCacheAsync();
}