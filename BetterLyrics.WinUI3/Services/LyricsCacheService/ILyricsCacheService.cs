using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsCacheService
{
    public interface ILyricsCacheService
    {
        Task<LyricsCacheItem?> GetLyricsAsync(SongInfo songInfo, LyricsSearchProvider provider);
        Task SaveLyricsAsync(SongInfo songInfo, LyricsCacheItem result);
        Task ClearCacheAsync();
    }
}
