using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ITransliterationService
{
    Task<(string, TransliterationSearchProvider)> TransliterateTextAsync(string text, string targetLangCode,
        CancellationToken token);
}