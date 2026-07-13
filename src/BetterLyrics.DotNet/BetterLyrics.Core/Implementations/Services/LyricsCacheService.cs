using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Implementations.Services;

public class LyricsCacheService : ILyricsCacheService
{
    private readonly IDbContextFactory<LyricsCacheDbContext> _contextFactory;

    public LyricsCacheService(IDbContextFactory<LyricsCacheDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    ///     Read cache from DB
    ///     <exception cref="OperationCanceledException"></exception>
    /// </summary>
    public async Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsSearchProvider provider,
        CancellationToken token)
    {
        using var context = await _contextFactory.CreateDbContextAsync(token);

        var key = songInfo.GetCacheKey();

        var existingItem = await context.LyricsCache
            .FirstOrDefaultAsync(x => x.CacheKey == key && x.Provider == provider, token);

        return existingItem;
    }

    public async Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result, CancellationToken token)
    {
        using var context = await _contextFactory.CreateDbContextAsync(token);

        var key = songInfo.GetCacheKey();

        var existingItem = await context.LyricsCache
            .FirstOrDefaultAsync(x => x.CacheKey == key && x.Provider == result.Provider, token);

        if (existingItem == null)
        {
            var newItem = (LyricsCacheItem)result.Clone();
            newItem.CacheKey = key;

            await context.LyricsCache.AddAsync(newItem, token);
        }
        else
        {
            existingItem.Title = result.Title;
            existingItem.Artist = result.Artist;
            existingItem.Album = result.Album;
            existingItem.Duration = result.Duration;

            existingItem.TransliterationProvider = result.TransliterationProvider;
            existingItem.TranslationProvider = result.TranslationProvider;

            existingItem.Raw = result.Raw;
            existingItem.Translation = result.Translation;

            existingItem.MatchPercentage = result.MatchPercentage;
            existingItem.Reference = result.Reference;
        }

        await context.SaveChangesAsync(token);
    }

    public async Task ClearCacheAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        await context.LyricsCache.ExecuteDeleteAsync();
        await context.Database.ExecuteSqlRawAsync("VACUUM;");
    }
}