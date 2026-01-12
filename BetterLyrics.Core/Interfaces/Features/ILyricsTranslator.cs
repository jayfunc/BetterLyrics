using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces.Features
{
    public interface ILyricsTranslator
    {
        Task<string?> GetTranslationAsync(string text, string targetLangCode);
    }
}
