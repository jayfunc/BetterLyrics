namespace BetterLyrics.Core.Interfaces
{
    public interface ILocalizer
    {
        string this[string key] { get; }
        string GetString(string key);
        string CurrentLanguage { get; }
    }
}
