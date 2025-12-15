using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.SettingsService;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

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
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception(text + " is empty or null.");
            }

            if (string.IsNullOrEmpty(_settingsService.AppSettings.TranslationSettings.CutletDockerServer))
            {
                throw new Exception("cutlet-docker server URL is not set in settings.");
            }

            var url = $"{_settingsService.AppSettings.TranslationSettings.CutletDockerServer}/convert";
            var response = await _httpClient.PostAsJsonAsync(url, new CutletDockerRequest { Text = text }, token);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(token);

            var result = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.CutletDockerResponse);
            return result?.RomajiText ?? string.Empty;
        }
    }
}
