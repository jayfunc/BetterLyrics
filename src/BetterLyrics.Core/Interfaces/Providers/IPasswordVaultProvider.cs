namespace BetterLyrics.Core.Interfaces.Providers;

public interface IPasswordVaultProvider
{
    void Save(string resource, string key, string value);
    string? Get(string resource, string key);
    void Delete(string resource, string key);
}