namespace BetterLyrics.Sdk.Interfaces.Plugins;

public interface ILocalizer
{
    string this[string key] { get; }
    string CurrentLanguage { get; }
    string GetString(string key);
}