using BetterLyrics.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface ILyricsSearchPlugin : IPlugin
    {
        Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration);
    }
}
