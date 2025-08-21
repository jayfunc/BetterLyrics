// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsSearchService
{
    public interface ILyricsSearchService
    {
        Task<LyricsSearchResult> SearchSmartlyAsync(string mediaSessionId, string title, string artist, string album, double durationMs, CancellationToken token);

        Task<List<LyricsSearchResult>> SearchAllAsync(string title, string artist, string album, double durationMs, CancellationToken token);
    }
}
