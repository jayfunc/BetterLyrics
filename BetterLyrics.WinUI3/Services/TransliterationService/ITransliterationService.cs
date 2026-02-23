using BetterLyrics.WinUI3.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TransliterationService
{
    public interface ITransliterationService
    {
        Task<(string, TransliterationSearchProvider)> TransliterateTextAsync(string text, string targetLangCode, CancellationToken token);
    }
}
