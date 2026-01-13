using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Http
{
    public class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}
