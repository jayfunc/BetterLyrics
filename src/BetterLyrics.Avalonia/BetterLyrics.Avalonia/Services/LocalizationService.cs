using BetterLyrics.Avalonia.Strings;
using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Avalonia.Services;

public class LocalizationService : ILocalizationService
{
    public string GetLocalizedString(string id)
    {
        return Resources.ResourceManager.GetString(id) ?? id;
    }
}