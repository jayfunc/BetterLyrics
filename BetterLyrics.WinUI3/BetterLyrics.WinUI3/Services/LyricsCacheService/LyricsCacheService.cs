using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
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

            var newItem = (LyricsCacheItem)result.Clone();
            newItem.CacheKey = key;

            if (existingItem != null)
            {                
                context.LyricsCache.Update(newItem);
            }
            else
            {
                await context.LyricsCache.AddAsync(newItem);
            }

            await context.SaveChangesAsync();
        }

    }
}
