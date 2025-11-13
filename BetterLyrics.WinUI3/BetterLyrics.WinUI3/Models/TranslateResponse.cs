using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public class TranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}
