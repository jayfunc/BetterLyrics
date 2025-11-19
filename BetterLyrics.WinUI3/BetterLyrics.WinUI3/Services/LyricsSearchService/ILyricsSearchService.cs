// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsSearchService
{
    public interface ILyricsSearchService
    {
        Task<LyricsSearchResult?> SearchSmartlyAsync(SongInfo songInfo, bool checkCache, LyricsSearchType? lyricsSearchType, CancellationToken token);

        Task<List<LyricsSearchResult>> SearchAllAsync(SongInfo songInfo, bool checkCache, CancellationToken token);
    }
}
