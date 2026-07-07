namespace BetterLyrics.Sdk.Interfaces.Plugins;

public interface ILyricsTranslator
{
    Task<string?> GetTranslationAsync(string text, string targetLangCode);
}