using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface ILyricsTranslationPlugin
    {
        Task<string?> GetTranslationAsync(string text, string targetLangCode);
    }
}
