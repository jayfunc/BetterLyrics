using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Sdk.Interfaces.Plugins;
using Microsoft.Extensions.Logging;
using NLanguageTag;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class TransliterationService : ITransliterationService
{
    private readonly IPluginService _pluginService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TransliterationService> _logger;

    public TransliterationService(ISettingsService settingsService, IPluginService pluginService,
        ILogger<TransliterationService> logger)
    {
        _settingsService = settingsService;
        _pluginService = pluginService;
        _logger = logger;
    }

    public async Task<(string, LyricsProvider)> TransliterateTextAsync(string text, LanguageTag? targetLangTag, CancellationToken token)
    {
        var pluginsInfo = _settingsService.AppSettings.PluginsInfo.Where(x => x.Plugin is ILyricsTransliterator);
        if (pluginsInfo != null)
        {
            foreach (var pluginInfo in pluginsInfo)
            {
                var plugin = (ILyricsTransliterator?)pluginInfo.Plugin;
                if (plugin != null)
                {
                    var result = await plugin.GetTransliterationAsync(text, targetLangTag, token);
                    token.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(result))
                    {
                        _logger.LogDebug("Transliteration applied by plugin {PluginId}.", pluginInfo.Id);
                        return (result, (LyricsProvider)_pluginService.GetPluginHashedId(pluginInfo?.Id ?? ""));
                    }
                }
            }
        }

        _logger.LogDebug("No transliteration plugin produced a result for the requested language.");
        return (string.Empty, (LyricsProvider)_pluginService.GetPluginHashedId(""));
    }
}