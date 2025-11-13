using BetterLyrics.WinUI3.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TranslateService
{
    public interface ITranslateService
    {
        Task<string> TranslateTextAsync(string text, string targetLangCode, CancellationToken token);

        int SearchTranslatedLyricsItself(List<LyricsData> lyricsDataArr, string targetLangCode);
    }
}
