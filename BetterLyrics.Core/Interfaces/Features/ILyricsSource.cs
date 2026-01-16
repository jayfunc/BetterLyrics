using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Features
{
    public interface ILyricsSource
    {
        public LyricsSearchProvider Provider { get; }

        Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration);
    }
}
