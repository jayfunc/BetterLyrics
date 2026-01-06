using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsCacheService
{
    public interface ILyricsCacheService
    {
        Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsSearchProvider provider);
        Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result);
    }
}
