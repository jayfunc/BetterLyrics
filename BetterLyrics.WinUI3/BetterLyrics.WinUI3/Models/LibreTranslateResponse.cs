using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}
