using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public class CutletDockerResponse
    {
        [JsonPropertyName("romaji")]
        public string RomajiText { get; set; }
    }
}
