using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using Lyricify.Lyrics.Helpers.General;
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
    public class TranslateService : ITranslateService
    {
        private readonly ISettingsService _settingsService;

        private readonly HttpClient _httpClient;

        public TranslateService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string text, string targetLangCode, CancellationToken? token)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text and target language must be provided.");
            }

            string? originalLangCode = LanguageHelper.DetectLanguageCode(text);
            if (string.IsNullOrWhiteSpace(originalLangCode) || originalLangCode == targetLangCode)
            {
                return text; // No translation needed
            }
            else if (originalLangCode == "zh-Hant" && targetLangCode == "zh-Hans")
            {
                return ChineseConverter.ConvertToSimplifiedChinese(text);
            }
            else if (originalLangCode == "zh-Hans" && targetLangCode == "zh-Hant")
            {
                return ChineseConverter.ConvertToTraditionalChinese(text);
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
    }
}
