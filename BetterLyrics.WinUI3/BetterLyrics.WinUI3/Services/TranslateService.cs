using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.ViewModels;
using Lyricify.Lyrics.Helpers.General;
using Microsoft.UI.Dispatching;
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
    public class TranslateService : BaseViewModel, ITranslateService
    {
        private readonly HttpClient _httpClient;

        public TranslateService(ISettingsService settingsService) : base(settingsService)
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateTextAsync(string text, string targetLangCode, CancellationToken? token)
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
            else if (originalLangCode == "zh-Hant" && targetLangCode == "zh-Hans")
            {
                return ChineseConverter.ConvertToSimplifiedChinese(text);
            }
            else if (originalLangCode == "zh-Hans" && targetLangCode == "zh-Hant")
            {
                return ChineseConverter.ConvertToTraditionalChinese(text);
            }

            if (string.IsNullOrEmpty(_settingsService.LibreTranslateServer))
            {
                throw new Exception("LibreTranslate server URL is not set in settings.");
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

        public int SearchTranslatedLyricsItself(List<LyricsData> lyricsDataArr)
        {
            string targetLangCode = LanguageHelper.GetUserTargetLanguageCode().Substring(0, 2);
            if (lyricsDataArr.Count > 1)
            {
                for (int i = 1; i < lyricsDataArr.Count; i++)
                {
                    if (lyricsDataArr[i].LanguageCode?.Substring(0, 2) == targetLangCode)
                    {
                        return i; // Translation lyrics data found
                    }
                }
            }
            return -1; // No translation lyrics data found
        }
    }
}
