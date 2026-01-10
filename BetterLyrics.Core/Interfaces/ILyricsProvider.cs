using BetterLyrics.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface ILyricsProvider
    {
        string Id { get; }
        string Name { get; }
        string Author { get; }

        Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration);
    }
}
