namespace BetterLyrics.Core.Interfaces.Features
{
    public interface ILyricsTranslator
    {
        Task<string?> GetTranslationAsync(string text, string targetLangCode);
    }
}
