using BetterLyrics.Core.Interfaces.Services;
using Microsoft.Windows.ApplicationModel.Resources;

namespace BetterLyrics.WinUI3.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager = new();
    private readonly ResourceMap _resourceMap;

    public LocalizationService()
    {
        _resourceMap = _resourceManager.MainResourceMap.GetSubtree("Resources");
    }

    public string GetLocalizedString(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        
        try 
        {
            var candidate = _resourceMap.TryGetValue(id);
            if (candidate != null)
            {
                return candidate.ValueAsString;
            }
        }
        catch 
        {
            // Fallback in case TryGetValue throws or something unexpected happens
        }
        return string.Empty;
    }
}
