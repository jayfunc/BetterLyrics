using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Http;

public sealed class ListenBrainzValidateTokenResponse
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }
}
