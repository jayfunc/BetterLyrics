using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Serialization;
using BetterLyrics.WinUI3.ViewModels;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BetterLyrics.Core.ViewModels;

namespace BetterLyrics.WinUI3.Services
{
    public partial class TranslationService : BaseViewModel, ITranslationService
    {
        private readonly ISettingsService _settingsService;
        private readonly IPluginService _pluginService;
        private readonly HttpClient _httpClient;

        public TranslationService(ISettingsService settingsService, IPluginService pluginService)
        {
            _settingsService = settingsService;
            _pluginService = pluginService;
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateTextAsync(string text, string targetLangCode, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception(text + " is empty or null.");
            }

            string? originalLangCode = LanguageHelper.DetectLanguageCode(text);
            if (string.IsNullOrWhiteSpace(originalLangCode) || originalLangCode == targetLangCode)
            {
                return text; // No translation needed
            }

            if (string.IsNullOrEmpty(_settingsService.AppSettings.TranslationSettings.LibreTranslateServer))
            {
                throw new Exception("LibreTranslate server URL is not set in settings.");
            }

            var url = $"{_settingsService.AppSettings.TranslationSettings.LibreTranslateServer}/translate";
            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(
            [
                new("q", text),
                new("source", originalLangCode),
                new("target", targetLangCode),
            ]), token);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(token);

            var result = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LibreTranslateResponse);
            return result?.TranslatedText ?? string.Empty;

            //var translatorPlugin = _pluginService.GetPlugin<ILyricsTranslator>();
            //if (translatorPlugin != null)
            //{
            //    var translatedText = await translatorPlugin.GetTranslationAsync(text, targetLangCode);
            //    if (!string.IsNullOrWhiteSpace(translatedText))
            //    {
            //        return translatedText;
            //    }
            //    else
            //    {
            //        throw new Exception("Translation failed or returned empty result.");
            //    }
            //}
            //else
            //{
            //    throw new Exception("No translation plugin available.");
            //}
        }
    }
}
