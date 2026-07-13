namespace BetterLyrics.Core.Interfaces.Services;

public interface ITranslationService
{
    Task<string> TranslateTextAsync(string text, string targetLangCode, CancellationToken token);
}