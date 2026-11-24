using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Http;

public sealed class ListenBrainzSubmitListenRequest
{
    [JsonPropertyName("listen_type")]
    public string ListenType { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public List<ListenBrainzListen> Payload { get; set; } = [];
}

public sealed class ListenBrainzListen
{
    /// <summary>
    /// Unix timestamp (seconds) of the moment the listen started. Must be omitted for "playing_now".
    /// </summary>
    [JsonPropertyName("listened_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ListenedAt { get; set; }

    [JsonPropertyName("track_metadata")]
    public ListenBrainzTrackMetadata TrackMetadata { get; set; } = new();
}

public sealed class ListenBrainzTrackMetadata
{
    [JsonPropertyName("artist_name")]
    public string ArtistName { get; set; } = string.Empty;

    [JsonPropertyName("track_name")]
    public string TrackName { get; set; } = string.Empty;

    [JsonPropertyName("release_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReleaseName { get; set; }

    [JsonPropertyName("additional_info")]
    public ListenBrainzAdditionalInfo AdditionalInfo { get; set; } = new();
}

public sealed class ListenBrainzAdditionalInfo
{
    [JsonPropertyName("submission_client")]
    public string SubmissionClient { get; set; } = "BetterLyrics";

    [JsonPropertyName("submission_client_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SubmissionClientVersion { get; set; }

    [JsonPropertyName("media_player")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MediaPlayer { get; set; }

    [JsonPropertyName("duration_ms")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DurationMs { get; set; }
}
