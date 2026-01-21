using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsCacheService
{
    public class LyricsCacheService : ILyricsCacheService
    {
        private readonly IDbContextFactory<LyricsCacheDbContext> _contextFactory;

        public LyricsCacheService(IDbContextFactory<LyricsCacheDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Read cache from DB
        /// </summary>
        public async Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsSearchProvider provider)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            string key = songInfo.GetCacheKey();

            var existingItem = await context.LyricsCache
                .FirstOrDefaultAsync(x => x.CacheKey == key && x.Provider == provider);

            return existingItem;
        }

        /// <summary>
        /// Write or update cache to DB
        /// </summary>
        public async Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            string key = songInfo.GetCacheKey();

            var existingItem = await context.LyricsCache
                .FirstOrDefaultAsync(x => x.CacheKey == key && x.Provider == result.Provider);

            if (existingItem == null)
            {
                var newItem = (LyricsCacheItem)result.Clone();
                newItem.CacheKey = key;

                await context.LyricsCache.AddAsync(newItem);
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

            await context.SaveChangesAsync();
        }

        public async Task ClearCacheAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.LyricsCache.ExecuteDeleteAsync();
            await context.Database.ExecuteSqlRawAsync("VACUUM;");
        }

    }
}
