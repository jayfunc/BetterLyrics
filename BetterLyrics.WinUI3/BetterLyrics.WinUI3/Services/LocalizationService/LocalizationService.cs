using Microsoft.Windows.ApplicationModel.Resources;

namespace BetterLyrics.WinUI3.Services.LocalizationService
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ResourceLoader _resourceLoader = new();

        public string GetLocalizedString(string id)
        {
            return _resourceLoader.GetString(id);
        }
    }
}
