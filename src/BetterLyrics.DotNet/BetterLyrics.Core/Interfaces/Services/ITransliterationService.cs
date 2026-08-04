using BetterLyrics.Core.Enums;
using NLanguageTag;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ITransliterationService
{
    Task<(string, LyricsProvider)> TransliterateTextAsync(string text, LanguageTag? targetLangTag,
        CancellationToken token);
}