using BetterLyrics.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces.Features
{
    public interface ILyricsSource
    {
        Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration);
    }
}
