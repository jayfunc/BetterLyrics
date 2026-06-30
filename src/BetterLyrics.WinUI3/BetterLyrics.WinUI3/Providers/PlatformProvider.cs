using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI.Helpers;

namespace BetterLyrics.WinUI3.Providers
{
    public class PlatformProvider : IPlatformProvider
    {
        public string AppVersion => Windows.ApplicationModel.Package.Current.Id.Version.ToFormattedString();

        public void SaveCredential(string resource, string key, string value)
        {
            PasswordVaultHelper.Save(resource, key, value);
        }

        public string? GetCredential(string resource, string key)
        {
            return PasswordVaultHelper.Get(resource, key);
        }

        public void DeleteCredential(string resource, string key)
        {
            PasswordVaultHelper.Delete(resource, key);
        }
    }
}