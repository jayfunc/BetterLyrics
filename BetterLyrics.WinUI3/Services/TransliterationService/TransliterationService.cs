using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Http;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TransliterationService
{
    public class TransliterationService : ITransliterationService
    {
        private readonly ISettingsService _settingsService;
        private readonly HttpClient _httpClient;

        public TransliterationService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
        }

        public async Task<string> TransliterateText(string text, string targetLangCode, CancellationToken token)
        {
            string? result = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception(text + " is empty or null.");
            }

            var plugin = _settingsService.AppSettings.PluginsInfo.Select(x => x.Plugin).OfType<ILyricsTransliterator>().FirstOrDefault();
            if (plugin != null)
            {
                result = await plugin.GetTransliterationAsync(text, PhoneticHelper.RomanCode);
            }

            return result ?? "";
        }
    }
}
