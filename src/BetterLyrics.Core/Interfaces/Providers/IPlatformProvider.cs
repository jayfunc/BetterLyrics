namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IPlatformProvider
    {
        string AppVersion { get; }

        void SaveCredential(string resource, string key, string value);
        string? GetCredential(string resource, string key);
        void DeleteCredential(string resource, string key);
    }
}