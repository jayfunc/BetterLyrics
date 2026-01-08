using BetterLyrics.WinUI3.Enums;
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
        /// Write cache to DB
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
                // No need to handle this case
                return;
            }

            await context.SaveChangesAsync();
        }

    }
}
