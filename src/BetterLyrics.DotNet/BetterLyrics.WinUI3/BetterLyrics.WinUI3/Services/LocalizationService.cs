using BetterLyrics.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.ApplicationModel.Resources;
using System;

namespace BetterLyrics.WinUI3.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly ResourceManager _resourceManager = new();
    private readonly ResourceMap _resourceMap;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
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

            _logger.LogDebug("Localized string \"{ResourceId}\" was not found.", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read localized string \"{ResourceId}\".", id);
        }
        return string.Empty;
    }
}
