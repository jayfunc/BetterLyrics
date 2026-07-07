using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class PasswordVaultProvider : IPasswordVaultProvider
{
    public void Save(string resource, string key, string value)
    {
        throw new System.NotImplementedException();
    }

    public string? Get(string resource, string key)
    {
        // TODO
        return null;
    }

    public void Delete(string resource, string key)
    {
        throw new System.NotImplementedException();
    }
}