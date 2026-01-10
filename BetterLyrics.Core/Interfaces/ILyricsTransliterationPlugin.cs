using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface ILyricsTransliterationPlugin : IPlugin
    {
        Task<string?> GetTransliterationAsync(string text, string targetLangCode);
    }
}
