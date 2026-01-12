using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces.Features
{
    public interface ILyricsTransliterator
    {
        Task<string?> GetTransliterationAsync(string text, string targetLangCode);
    }
}
