using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Serialization;
using NLanguageTag;
using System.Text.Json;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class TranslationService(ISettingsService settingsService, IPluginService pluginService, IHttpClientFactory httpClientFactory) : ITranslationService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();
    private readonly IPluginService _pluginService = pluginService;
    private readonly ISettingsService _settingsService = settingsService;

    public async Task<string> TranslateTextAsync(string text, LanguageTag? targetLangTag, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception(text + " is empty or null.");
        }

        var originalLangTag = LanguageHelper.DetectLanguageTag(text);
        if (originalLangTag == null || originalLangTag == targetLangTag)
        {
            return text; // No translation needed
        }

        if (string.IsNullOrEmpty(_settingsService.AppSettings.TranslationSettings.LibreTranslateServer))
        {
            throw new Exception("LibreTranslate server URL is not set in settings.");
        }

        var url = $"{_settingsService.AppSettings.TranslationSettings.LibreTranslateServer}/translate";
        var requestBody = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("q", text),
            new KeyValuePair<string, string>("source", originalLangTag?.Language?.Macrolanguage?.ToString() ?? (originalLangTag?.Language?.ToString() ?? "auto")),
            new KeyValuePair<string, string>("target", targetLangTag?.Language?.Macrolanguage?.ToString() ?? (targetLangTag?.Language?.ToString() ?? ""))
        ]);
        var response = await _client.PostAsync(url, requestBody, token);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(token);

        var result = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LibreTranslateResponse);
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

    public async Task<string> TranslateTextAsync(string text, string? targetLangCode, CancellationToken token)
    {
        return await TranslateTextAsync(text, LanguageTag.TryParse(targetLangCode, out var languageTag) ? languageTag : null, token);
    }
}