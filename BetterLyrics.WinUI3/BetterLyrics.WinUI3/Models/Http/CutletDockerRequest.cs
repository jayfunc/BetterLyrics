using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Http
{
    public class CutletDockerRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
