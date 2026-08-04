using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Sdk.Interfaces.Plugins;
using NLanguageTag;

namespace BetterLyrics.Core.Implementations.Services;

public class TransliterationService : ITransliterationService
{
    private readonly IPluginService _pluginService;
    private readonly ISettingsService _settingsService;

    public TransliterationService(ISettingsService settingsService, IPluginService pluginService)
    {
        _settingsService = settingsService;
        _pluginService = pluginService;
    }

    public async Task<(string, LyricsProvider)> TransliterateTextAsync(string text,
        LanguageTag? targetLangTag, CancellationToken token)
    {
        var pluginsInfo =
            _settingsService.AppSettings.PluginsInfo.Where(x => x.Plugin is ILyricsTransliterator);
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
                        return (result, (LyricsProvider)_pluginService.GetPluginHashedId(pluginInfo?.Id ?? ""));
                    }
                }
            }
        }

        return (string.Empty, (LyricsProvider)_pluginService.GetPluginHashedId(""));
    }
}