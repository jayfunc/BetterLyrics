using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TransliterationService
{
    public class TransliterationService : ITransliterationService
    {
        private readonly ISettingsService _settingsService;
        private readonly IPluginService _pluginService;

        public TransliterationService(ISettingsService settingsService, IPluginService pluginService)
        {
            _settingsService = settingsService;
            _pluginService = pluginService;
        }

        public async Task<(string, TransliterationSearchProvider)> TransliterateTextAsync(string text, string targetLangCode, CancellationToken token)
        {
            string? result = null;

            var pluginInfo = _settingsService.AppSettings.PluginsInfo.FirstOrDefault(x => x.Plugin is ILyricsTransliterator);
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
}
