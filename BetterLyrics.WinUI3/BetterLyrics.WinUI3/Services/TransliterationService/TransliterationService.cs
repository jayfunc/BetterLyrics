using BetterLyrics.Core.Interfaces;
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
        private readonly IPluginService _pluginService;
        private readonly HttpClient _httpClient;

        public TransliterationService(ISettingsService settingsService, IPluginService pluginService)
        {
            _settingsService = settingsService;
            _pluginService = pluginService;
            _httpClient = new HttpClient();
        }

        //public async Task<string> TransliterateText(string text, string targetLangCode, CancellationToken token)
        //{
        //    if (string.IsNullOrWhiteSpace(text))
        //    {
        //        throw new Exception(text + " is empty or null.");
        //    }

        //    if (string.IsNullOrEmpty(_settingsService.AppSettings.TranslationSettings.CutletDockerServer))
        //    {
        //        throw new Exception("cutlet-docker server URL is not set in settings.");
        //    }

        //    var request = new CutletDockerRequest { Text = text };
        //    var reqJson = System.Text.Json.JsonSerializer.Serialize(request, SourceGenerationContext.Default.CutletDockerRequest);

        //    var url = $"{_settingsService.AppSettings.TranslationSettings.CutletDockerServer}/convert";
        //    var response = await _httpClient.PostAsync(url, new StringContent(reqJson, Encoding.UTF8, "application/json"));

        //    response.EnsureSuccessStatusCode();
        //    var resJson = await response.Content.ReadAsStringAsync(token);

        //    var result = System.Text.Json.JsonSerializer.Deserialize(resJson, SourceGenerationContext.Default.CutletDockerResponse);
        //    return result?.RomajiText ?? string.Empty;
        //}

        public async Task<string> TransliterateText(string text, string targetLangCode, CancellationToken token)
        {
            string? result = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception(text + " is empty or null.");
            }

            var plugin = (ILyricsTransliterationPlugin?)_pluginService.Plugins.FirstOrDefault(x => x is ILyricsTransliterationPlugin);
            if (plugin != null)
            {
                result = await plugin.GetTransliterationAsync(text, PhoneticHelper.RomanCode);
            }

            return result ?? "";
        }
    }
}
