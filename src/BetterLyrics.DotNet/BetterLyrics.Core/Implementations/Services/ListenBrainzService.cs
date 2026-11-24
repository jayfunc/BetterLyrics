using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Http;
using BetterLyrics.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class ListenBrainzService : IListenBrainzService
{
    private const string ListenTypePlayingNow = "playing_now";
    private const string ListenTypeSingle = "single";
    private const string SubmissionClientName = "BetterLyrics";

    private readonly HttpClient _client;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ISongSearchMapService _songSearchMapService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILogger<ListenBrainzService> _logger;

    private string? _token;
    private TimeSpan _serverClockOffset = TimeSpan.Zero;

    public ListenBrainzService(IHttpClientFactory httpClientFactory, IPasswordVaultProvider passwordVaultProvider,
        ISongSearchMapService songSearchMapService, IGlobalToastProvider globalToastProvider,
        ILogger<ListenBrainzService> logger)
    {
        _client = httpClientFactory.CreateClient();

        // ListenBrainz requires a meaningful User-Agent on every API call, otherwise requests can be
        // blocked/dropped as bot traffic. Format: "Application name/<version> ( contact-url )".
        var userAgent = $"{SubmissionClientName}/{MetadataHelper.AppVersion} ( {Link.BetterLyricsGitHub} )";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

        _passwordVaultProvider = passwordVaultProvider;
        _songSearchMapService = songSearchMapService;
        _globalToastProvider = globalToastProvider;
        _logger = logger;

        _token = _passwordVaultProvider.Get(Core.Constants.App.AppName, ListenBrainz.TokenCredentialKey);
        _logger.LogDebug("ListenBrainz service initialized. Token present: {HasToken}.", IsAuthenticated);

        if (IsAuthenticated)
        {
            _ = RefreshAsync();
        }
    }

    public event EventHandler<ListenBrainzAuthenticationChangedEventArgs>? AuthenticationChanged;

    public string? Username { get; private set; }

    public string? UserToken => _token;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public async Task<bool> SaveTokenAsync(string? token)
    {
        var newToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        if (newToken is null)
        {
            await ClearTokenAsync();
            return true;
        }

        var (status, userName, error) = await ValidateTokenAsync(newToken).ConfigureAwait(false);

        switch (status)
        {
            case ListenBrainzTokenValidationStatus.Valid:
                _token = newToken;
                _passwordVaultProvider.Delete(Core.Constants.App.AppName, ListenBrainz.TokenCredentialKey);
                _passwordVaultProvider.Save(Core.Constants.App.AppName, ListenBrainz.TokenCredentialKey, newToken);
                Username = userName;
                _logger.LogInformation("ListenBrainz token saved for user {UserName}.", userName);
                RaiseAuthenticationChanged();
                return true;

            case ListenBrainzTokenValidationStatus.Invalid:
                _logger.LogWarning("ListenBrainz token was rejected: {Error}.", error);
                _globalToastProvider.Show("ListenBrainzTokenInvalid", error, MessageSeverity.Error);
                return false;

            default:
                _logger.LogWarning("ListenBrainz token could not be validated: {Error}.", error);
                _globalToastProvider.Show("Error", error, MessageSeverity.Error);
                return false;
        }
    }

    public Task ClearTokenAsync()
    {
        _logger.LogInformation("ListenBrainz token cleared.");
        ClearToken();
        return Task.CompletedTask;
    }

    public async Task RefreshAsync()
    {
        var token = _token;
        if (string.IsNullOrEmpty(token)) return;

        var (status, userName, error) = await ValidateTokenAsync(token).ConfigureAwait(false);

        switch (status)
        {
            case ListenBrainzTokenValidationStatus.Valid:
                if (Username != userName)
                {
                    Username = userName;
                    _logger.LogInformation("ListenBrainz user information loaded: {UserName}.", userName);
                    RaiseAuthenticationChanged();
                }
                break;

            case ListenBrainzTokenValidationStatus.Invalid:
                _logger.LogWarning("The stored ListenBrainz token is no longer valid: {Error}.", error);
                ClearToken();
                break;

            default:
                _logger.LogWarning("Failed to validate the stored ListenBrainz token: {Error}.", error);
                break;
        }
    }

    public async Task UpdateNowPlayingAsync(SongInfo songInfo)
    {
        await SubmitListenAsync(songInfo, ListenTypePlayingNow, null).ConfigureAwait(false);
    }

    public async Task TrackAsync(SongInfo songInfo)
    {
        await SubmitListenAsync(songInfo, ListenTypeSingle, GetListenedAtUnixSeconds(songInfo))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// ListenBrainz expects <c>listened_at</c> to be the moment playback started, so the song start time is
    /// used instead of the submission time. The local clock can be off (it was ~30s ahead on the test
    /// machine), so the offset learned from the server's responses is applied and timestamps are never
    /// submitted in the future.
    /// </summary>
    private long GetListenedAtUnixSeconds(SongInfo songInfo)
    {
        var serverNow = DateTimeOffset.UtcNow + _serverClockOffset;

        try
        {
            var startedAt = new DateTimeOffset(DateTime.FromBinary(songInfo.StartedAt)) + _serverClockOffset;
            if (startedAt > serverNow || serverNow - startedAt > TimeSpan.FromDays(1))
            {
                startedAt = serverNow.AddSeconds(-1);
            }

            return startedAt.ToUnixTimeSeconds();
        }
        catch (ArgumentException)
        {
            return serverNow.AddSeconds(-1).ToUnixTimeSeconds();
        }
    }

    private void CaptureServerTime(HttpResponseMessage response)
    {
        var serverDate = response.Headers.Date;
        if (serverDate is null) return;

        _serverClockOffset = serverDate.Value - DateTimeOffset.UtcNow;
    }

    private async Task SubmitListenAsync(SongInfo songInfo, string listenType, long? listenedAt)
    {
        var token = _token;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("Skipped ListenBrainz {ListenType} because no token is configured.", listenType);
            return;
        }

        var isPlayingNow = listenType == ListenTypePlayingNow;

        try
        {
            var (mappedTitle, mappedArtist, mappedAlbum) = await _songSearchMapService
                .GetMappingAsync(songInfo).ConfigureAwait(false);

            if (!IsUsableTrackMetadata(mappedTitle, mappedArtist))
            {
                _logger.LogDebug("Skipped ListenBrainz {ListenType} because the track metadata is incomplete.",
                    listenType);
                return;
            }

            var requestBody = new ListenBrainzSubmitListenRequest
            {
                ListenType = listenType,
                Payload =
                [
                    new ListenBrainzListen
                    {
                        ListenedAt = listenedAt,
                        TrackMetadata = new ListenBrainzTrackMetadata
                        {
                            TrackName = mappedTitle.Trim(),
                            ArtistName = mappedArtist.Trim(),
                            ReleaseName = string.IsNullOrWhiteSpace(mappedAlbum) ? null : mappedAlbum.Trim(),
                            AdditionalInfo = new ListenBrainzAdditionalInfo
                            {
                                SubmissionClient = SubmissionClientName,
                                SubmissionClientVersion = MetadataHelper.AppVersion,
                                MediaPlayer = string.IsNullOrWhiteSpace(songInfo.PlayerId) ? null : songInfo.PlayerId,
                                DurationMs = songInfo.DurationMs > 0
                                    ? (int)Math.Min(int.MaxValue, Math.Round(songInfo.DurationMs))
                                    : null
                            }
                        }
                    }
                ]
            };

            var json = JsonSerializer.Serialize(requestBody,
                SourceGenerationContext.Default.ListenBrainzSubmitListenRequest);

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{ListenBrainz.ApiBaseUrl}/submit-listens")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);

            using var response = await _client.SendAsync(request).ConfigureAwait(false);
            CaptureServerTime(response);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var submitResponse = TryReadSubmitResponse(responseBody);

                // A 2xx status is not enough: blocked requests/proxies can answer with an empty or
                // non-JSON body, which would otherwise be mistaken for a stored listen.
                if (submitResponse is null ||
                    !string.Equals(submitResponse.Status, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    var detail = submitResponse?.Error ?? submitResponse?.Message
                                 ?? (string.IsNullOrWhiteSpace(responseBody)
                                     ? "empty response body"
                                     : Truncate(responseBody, 300));
                    _logger.LogWarning(
                        "ListenBrainz {ListenType} submission was not acknowledged (HTTP {StatusCode}): {Detail}",
                        listenType, (int)response.StatusCode, detail);
                    if (!isPlayingNow)
                    {
                        _globalToastProvider.Show("Error", "ListenBrainz did not accept the listen.",
                            MessageSeverity.Error);
                    }
                    return;
                }

                _logger.LogDebug("ListenBrainz {ListenType} response: {Body}.", listenType,
                    Truncate(responseBody, 200));

                if (isPlayingNow)
                {
                    _logger.LogDebug("Updated ListenBrainz now playing: {Title} - {Artist}.", mappedTitle,
                        mappedArtist);
                }
                else
                {
                    _logger.LogInformation("Submitted ListenBrainz listen: {Title} - {Artist}.", mappedTitle,
                        mappedArtist);
                }
                return;
            }

            var apiError = TryReadError(responseBody);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                var authError = apiError ?? "The ListenBrainz token was rejected.";
                _logger.LogWarning("ListenBrainz rejected the stored token: {Error}.", authError);
                _globalToastProvider.Show("ListenBrainzTokenInvalid", authError, MessageSeverity.Error);
                ClearToken();
                return;
            }

            if ((int)response.StatusCode == 429)
            {
                var resetIn = response.Headers.TryGetValues("X-RateLimit-Reset-In", out var resetValues)
                    ? resetValues.FirstOrDefault()
                    : null;
                var rateLimitError = apiError ?? "rate limit reached";
                _logger.LogWarning(
                    "ListenBrainz {ListenType} request was rate limited ({Error}); reset in {ResetIn}s.",
                    listenType, rateLimitError, resetIn ?? "unknown");
                if (!isPlayingNow)
                {
                    _globalToastProvider.Show("Error", "ListenBrainz rate limit reached. Try again later.",
                        MessageSeverity.Error);
                }
                return;
            }

            var error = apiError ?? $"HTTP {(int)response.StatusCode}";
            _logger.LogWarning("ListenBrainz {ListenType} request failed: {Error}.", listenType, error);
            if (!isPlayingNow)
            {
                _globalToastProvider.Show("Error", error, MessageSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit a ListenBrainz {ListenType} request for {Title} - {Artist}.",
                listenType, songInfo.Title, songInfo.Artist);
            if (!isPlayingNow)
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }
    }

    private async Task<(ListenBrainzTokenValidationStatus Status, string? UserName, string? Error)> ValidateTokenAsync(
        string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ListenBrainz.ApiBaseUrl}/validate-token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);

            using var response = await _client.SendAsync(request).ConfigureAwait(false);
            CaptureServerTime(response);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return (ListenBrainzTokenValidationStatus.Invalid, null,
                    TryReadError(body) ?? "The ListenBrainz token was rejected.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (ListenBrainzTokenValidationStatus.Unreachable, null,
                    $"Validating the ListenBrainz token failed with HTTP {(int)response.StatusCode}.");
            }

            var result = JsonSerializer.Deserialize(body,
                SourceGenerationContext.Default.ListenBrainzValidateTokenResponse);

            if (result is null)
            {
                return (ListenBrainzTokenValidationStatus.Unreachable, null,
                    "The ListenBrainz validate-token response was empty.");
            }

            if (result.Valid || !string.IsNullOrWhiteSpace(result.UserName))
            {
                return (ListenBrainzTokenValidationStatus.Valid, result.UserName, null);
            }

            return (ListenBrainzTokenValidationStatus.Invalid, null,
                result.Message ?? TryReadError(body) ?? "The ListenBrainz token is not valid.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate the ListenBrainz token.");
            return (ListenBrainzTokenValidationStatus.Unreachable, null, ex.Message);
        }
    }

    private void ClearToken()
    {
        _token = null;
        Username = null;
        _passwordVaultProvider.Delete(Core.Constants.App.AppName, ListenBrainz.TokenCredentialKey);
        RaiseAuthenticationChanged();
    }

    private void RaiseAuthenticationChanged()
    {
        AuthenticationChanged?.Invoke(this,
            new ListenBrainzAuthenticationChangedEventArgs(IsAuthenticated, Username));
    }

    private static bool IsUsableTrackMetadata(string? title, string? artist)
    {
        return !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist)
                                                  && title != "N/A" && artist != "N/A";
    }

    private static string? TryReadError(string? body)
    {
        var result = TryReadSubmitResponse(body);
        return result?.Error ?? result?.Message;
    }

    private static ListenBrainzSubmitListenResponse? TryReadSubmitResponse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            return JsonSerializer.Deserialize(body,
                SourceGenerationContext.Default.ListenBrainzSubmitListenResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
