using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Services
{
    public class LyricsCacheService : ILyricsCacheService
    {
        public Task ClearCacheAsync()
        {
            throw new NotImplementedException();
        }

        public Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsSearchProvider provider, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
