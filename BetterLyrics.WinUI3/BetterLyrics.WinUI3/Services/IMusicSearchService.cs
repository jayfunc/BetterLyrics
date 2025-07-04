// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Services
{
    public interface IMusicSearchService
    {
        Task <byte[]> SearchAlbumArtAsync(string title, string artist, string album);

        Task<(string?, LyricsFormat?)> SearchLyricsAsync(
            string title,
            string artist,
            string album = "",
            double durationMs = 0.0
        );
    }
}
