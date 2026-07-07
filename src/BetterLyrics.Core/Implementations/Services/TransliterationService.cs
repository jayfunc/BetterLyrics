using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Sdk.Interfaces.Plugins;

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

    public async Task<(string, TransliterationSearchProvider)> TransliterateTextAsync(string text,
        string targetLangCode, CancellationToken token)
    {
        string? result = null;

        var pluginInfo =
            _settingsService.AppSettings.PluginsInfo.FirstOrDefault(x => x.Plugin is ILyricsTransliterator);
        if (pluginInfo != null)
        {
            var plugin = (ILyricsTransliterator?)pluginInfo.Plugin;
            if (plugin != null)
            {
                result = await plugin.GetTransliterationAsync(text, targetLangCode, token);
                token.ThrowIfCancellationRequested();
            }
        }

        return (result ?? "", (TransliterationSearchProvider)_pluginService.GetPluginHashedId(pluginInfo?.Id ?? ""));
    }
}