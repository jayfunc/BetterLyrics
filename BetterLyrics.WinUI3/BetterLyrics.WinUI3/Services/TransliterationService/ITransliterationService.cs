using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TransliterationService
{
    public interface ITransliterationService
    {
        Task<string> TransliterateText(string text, string targetLangCode, CancellationToken token);
    }
}
