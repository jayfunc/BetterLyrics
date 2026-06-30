namespace BetterLyrics.Core.Interfaces.Plugins
{
    public interface ILyricsTransliterator
    {
        Task<string?> GetTransliterationAsync(string text, string targetLangCode, CancellationToken token);
    }
}
