using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Avalonia.Services
{
    public class LocalizationService : ILocalizationService
    {
        public string GetLocalizedString(string id)
        {
            return Strings.Resources.ResourceManager.GetString(id) ?? id;
        }
    }
}
