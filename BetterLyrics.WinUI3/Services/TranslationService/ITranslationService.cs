using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TranslationService
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLangCode, CancellationToken token);
    }
}
