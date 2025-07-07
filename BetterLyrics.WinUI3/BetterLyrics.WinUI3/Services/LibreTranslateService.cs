using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public class LibreTranslateService : ILibreTranslateService
    {
        private readonly ISettingsService _settingsService;

        private readonly HttpClient _httpClient;

        public LibreTranslateService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string text, CancellationToken? token)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text and target language must be provided.");
            }

            string targetLangCode = AppInfo.GetAllTranslationLanguagesInfo()[_settingsService.SelectedTargetLanguageIndex].Code;

            string originalLangCode = await DetectLanguageCode(text);
            token?.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(originalLangCode) || originalLangCode == targetLangCode)
            {
                return text; // No translation needed
            }

            var url = $"{_settingsService.LibreTranslateServer}/translate";
            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(
            [
                new("q", text),
                new("source", originalLangCode),
                new("target", targetLangCode),
            ]));
            token?.ThrowIfCancellationRequested();

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            token?.ThrowIfCancellationRequested();

            var result = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.TranslateResponse);
            return result?.TranslatedText ?? string.Empty;
        }

        private async Task<string> DetectLanguageCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text must be provided.");
            }
            var url = $"{_settingsService.LibreTranslateServer}/detect";
            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(
            [
                new("q", text),
            ]));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var resultList = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ListDetectLanguageResult);
            return resultList?.OrderByDescending(x => x.Confidence).FirstOrDefault()?.Language ?? string.Empty;
        }
    }
}
