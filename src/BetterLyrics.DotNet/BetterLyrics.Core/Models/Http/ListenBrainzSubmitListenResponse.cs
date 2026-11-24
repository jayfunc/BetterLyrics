using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Http;

public sealed class ListenBrainzSubmitListenResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
