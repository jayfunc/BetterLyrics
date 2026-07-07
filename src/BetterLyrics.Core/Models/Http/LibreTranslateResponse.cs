using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Http;

public class LibreTranslateResponse
{
    [JsonPropertyName("translatedText")] public string TranslatedText { get; set; }
}