// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Services
{
    public interface ILyricsSearchService
    {
        Task<(string?, LyricsSearchProvider?)> SearchAsync(string title, string artist, string album, double durationMs, CancellationToken token);
    }
}
