using NLanguageTag;

namespace BetterLyrics.Sdk.Interfaces.Plugins;

public interface ILyricsTransliterator
{
    Task<string?> GetTransliterationAsync(string text, LanguageTag? targetLangTag, CancellationToken token);
}