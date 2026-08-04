using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using LiteDB;

namespace BetterLyrics.Core.Implementations.Services;

public class LyricsCacheService : ILyricsCacheService
{
    private readonly IDatabaseService _databaseService;

    public LyricsCacheService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        
        var col = _databaseService.LyricsCacheDb.GetCollection<LyricsCacheItem>("lyricsCache");
        col.EnsureIndex(x => x.CacheKey);
        col.EnsureIndex(x => x.Provider);
    }

    private ILiteCollection<LyricsCacheItem> GetCollection()
    {
        return _databaseService.LyricsCacheDb.GetCollection<LyricsCacheItem>("lyricsCache");
    }

    public async Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsProvider provider,
        CancellationToken token)
    {
        var col = GetCollection();
        var key = songInfo.GetCacheKey();

        var existingItem = col.FindOne(x => x.CacheKey == key && x.Provider == provider);

        return await Task.FromResult(existingItem);
    }

    public Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result, CancellationToken token)
    {
        var col = GetCollection();
        var key = songInfo.GetCacheKey();

        var existingItem = col.FindOne(x => x.CacheKey == key && x.Provider == result.Provider);

        if (existingItem == null)
        {
            var newItem = (LyricsCacheItem)result.Clone();
            newItem.CacheKey = key;
            col.Insert(newItem);
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

            col.Update(existingItem);
        }

        return Task.CompletedTask;
    }

    public Task ClearCacheAsync()
    {
        var col = GetCollection();
        col.DeleteAll();
        _databaseService.LyricsCacheDb.Rebuild();
        return Task.CompletedTask;
    }
}