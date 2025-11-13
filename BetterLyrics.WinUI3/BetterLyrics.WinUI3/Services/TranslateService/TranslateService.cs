using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.TranslateService
{
    public partial class TranslateService : BaseViewModel, ITranslateService
    {
        private readonly ISettingsService _settingsService;
        private readonly HttpClient _httpClient;

        public TranslateService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
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

            var result = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.TranslateResponse);
            return result?.TranslatedText ?? string.Empty;
        }

        public int SearchTranslatedLyricsItself(List<LyricsData> lyricsDataArr, string targetLangCode)
        {
            int ret = -1;
            float maxTranslatinRate = 0.0f;

            if (lyricsDataArr.Count > 1)
            {
                for (int i = 1; i < lyricsDataArr.Count; i++)
                {
                    if (lyricsDataArr[i].LanguageCode == targetLangCode)
                    {
                        float translationRate = lyricsDataArr[i].LyricsLines.Count / lyricsDataArr[0].LyricsLines.Count;
                        if (translationRate > maxTranslatinRate)
                        {
                            maxTranslatinRate = translationRate;
                            ret = i;
                        }
                    }
                }
            }
            return ret;
        }
    }
}
