using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class LastFmService : ILastFmService
{
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmDialogProvider _lastFmDialogProvider;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;
    private readonly ILauncherProvider _launcherProvider;
    private readonly ILogger<LastFmService> _logger;

    private LastfmClient? _client;
    private string? _apiKey;
    private string? _sharedSecret;
    private string? _sessionKey;

    public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    public User? User { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_sharedSecret);

    public string? ApiKey => _apiKey;

    public string? SharedSecret => _sharedSecret;

    public LastFmService(ISettingsService settingsService,
        ISongSearchMapService songSearchMapService, IPasswordVaultProvider passwordVaultProvider,
        IGlobalToastProvider globalToastProvider,
        ILauncherProvider launcherProvider, ILastFmDialogProvider lastFmDialogProvider,
        ILogger<LastFmService> logger)
    {
        _settingsService = settingsService;
        _songSearchMapService = songSearchMapService;
        _passwordVaultProvider = passwordVaultProvider;
        _globalToastProvider = globalToastProvider;
        _launcherProvider = launcherProvider;
        _lastFmDialogProvider = lastFmDialogProvider;
        _logger = logger;

        _apiKey = _passwordVaultProvider.Get(Core.Constants.App.AppName, LastFmApi.ApiKeyCredentialKey);
        _sharedSecret = _passwordVaultProvider.Get(Core.Constants.App.AppName, LastFmApi.SharedSecretCredentialKey);
        _sessionKey = _passwordVaultProvider.Get(Core.Constants.App.AppName, LastFmApi.SessionKeyCredentialKey);

        RebuildClient();

        _logger.LogDebug("Last.fm service initialized. Configured: {IsConfigured}. Session key present: {HasSessionKey}.",
            IsConfigured, !string.IsNullOrEmpty(_sessionKey));
        _ = UpdateAuthStatusAsync();
    }

    public async Task ConfigureAsync(string? apiKey, string? sharedSecret)
    {
        var newApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        var newSharedSecret = string.IsNullOrWhiteSpace(sharedSecret) ? null : sharedSecret.Trim();

        if (newApiKey != _apiKey || newSharedSecret != _sharedSecret)
        {
            _apiKey = newApiKey;
            _sharedSecret = newSharedSecret;

            SaveCredential(LastFmApi.ApiKeyCredentialKey, _apiKey);
            SaveCredential(LastFmApi.SharedSecretCredentialKey, _sharedSecret);

            // A session key is bound to the credentials that created it, so it has to be discarded.
            ClearSession();

            RebuildClient();

            _logger.LogInformation("Last.fm API credentials were updated. Configured: {IsConfigured}.", IsConfigured);
        }

        await UpdateAuthStatusAsync();
    }

    public async Task ConfirmAuthAsync()
    {
        if (_client == null) return;

        try
        {
            await _client.AuthenticateViaWebAsync();
            if (_client.Session != null && _client.Session.Authenticated)
            {
                _sessionKey = _client.Session.SessionKey;
                SaveCredential(LastFmApi.SessionKeyCredentialKey, _sessionKey);
                _logger.LogInformation("Last.fm authentication succeeded.");
                await UpdateAuthStatusAsync();
                return;
            }
            throw new Exception("Authentication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Last.fm authentication failed.");
            _globalToastProvider.Show("LastFMAuthFailed", ex.Message, MessageSeverity.Error);
        }
    }

    public async Task ConfirmUnAuthAsync()
    {
        ClearSession();
        _logger.LogInformation("Last.fm authentication cleared by the user.");
        await UpdateAuthStatusAsync();
    }

    public async Task AuthAsync()
    {
        if (_client == null)
        {
            _logger.LogWarning("Last.fm authorization requested without user provided API credentials.");
            _globalToastProvider.Show("LastFMCredentialsRequired", null, MessageSeverity.Warning);
            return;
        }

        var url = await _client.GetWebAuthenticationUrlAsync();

        _ = _launcherProvider.LaunchUriAsync(new Uri(url));

        await _lastFmDialogProvider.ShowAuthDialogAsync(ConfirmAuthAsync);
    }

    public async Task UnAuthAsync()
    {
        await _launcherProvider.LaunchUriAsync(new Uri(LastFmApi.UnAuthUrl));
        await _lastFmDialogProvider.ShowUnAuthDialogAsync(ConfirmUnAuthAsync);
    }

    public async Task TrackAsync(SongInfo songInfo)
    {
        var client = _client;
        if (IsAuthenticated && client != null)
        {
            var (mappedTitle, mappedArtist, mappedAlbum) = await _songSearchMapService.GetMappingAsync(songInfo);

            try
            {
                var scrobble = new Scrobble
                {
                    Track = mappedTitle,
                    Artist = mappedArtist,
                    Album = mappedAlbum,
                    Date = DateTime.UtcNow
                };
                var resp = await client.Track.ScrobbleAsync(scrobble);
                if (resp != null && resp.Accepted == 0)
                {
                    _logger.LogWarning("Last.fm scrobble was not accepted. Ignored: {Ignored}.", resp.Ignored);
                    _globalToastProvider.Show("Error", resp.Ignored > 0 ? "Scrobble ignored" : "Scrobble failed", MessageSeverity.Error);
                }
                else
                {
                    _logger.LogInformation("Scrobbled to Last.fm: {Title} - {Artist}.", mappedTitle, mappedArtist);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrobble to Last.fm: {Title} - {Artist}.", mappedTitle,
                    mappedArtist);
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }
        else
        {
            _logger.LogDebug("Skipped Last.fm scrobble because the service is not authenticated.");
        }
    }

    public async Task UpdateNowPlayingAsync(SongInfo songInfo)
    {
        var client = _client;
        if (IsAuthenticated && client != null)
        {
            var (mappedTitle, mappedArtist, mappedAlbum) = await _songSearchMapService.GetMappingAsync(songInfo);

            try
            {
                var resp = await client.Track.UpdateNowPlayingAsync(mappedTitle, mappedArtist, 0, mappedAlbum);
                if (!resp)
                {
                    _logger.LogWarning("Failed to update Last.fm now playing: {Title} - {Artist}.", mappedTitle,
                        mappedArtist);
                    _globalToastProvider.Show("Error", "Update now playing failed", MessageSeverity.Error);
                }
                else
                {
                    _logger.LogDebug("Updated Last.fm now playing: {Title} - {Artist}.", mappedTitle, mappedArtist);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Last.fm now playing: {Title} - {Artist}.", mappedTitle,
                    mappedArtist);
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }
    }

    public async Task RefreshAsync()
    {
        await UpdateAuthStatusAsync();
    }

    private void RebuildClient()
    {
        _client = IsConfigured ? new LastfmClient(_apiKey!, _sharedSecret!) : null;
    }

    private void SaveCredential(string key, string? value)
    {
        _passwordVaultProvider.Delete(Core.Constants.App.AppName, key);
        if (!string.IsNullOrEmpty(value))
        {
            _passwordVaultProvider.Save(Core.Constants.App.AppName, key, value);
        }
    }

    private void ClearSession()
    {
        _sessionKey = null;
        _passwordVaultProvider.Delete(Core.Constants.App.AppName, LastFmApi.SessionKeyCredentialKey);
    }

    private async Task UpdateAuthStatusAsync()
    {
        var client = _client;

        IsAuthenticated = client != null && !string.IsNullOrEmpty(_sessionKey);
        if (IsAuthenticated)
        {
            client!.Session.SessionKey = _sessionKey;
        }
        IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
        if (IsAuthenticated)
        {
            try
            {
                User = await client!.User.GetInfoAsync(null);
                _logger.LogInformation("Last.fm user information loaded: {UserName}.", User?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load Last.fm user information; treating the session as unavailable.");
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
                User = null;
            }
        }
        else
        {
            User = null;
            _logger.LogDebug("Last.fm is not authenticated.");
        }

        UserChanged?.Invoke(this, new LastFMUserChangedEventArgs(User));
    }

    public async Task<string?> GetAlbumArtUrlAsync(SongInfo songInfo)
    {
        var client = _client;
        if (client == null) return null;

        try
        {
            var artist = songInfo.Artist ?? "";
            var album = songInfo.Album ?? "";
            var track = songInfo.Title ?? "";

            if (!string.IsNullOrWhiteSpace(album))
            {
                var albumInfo = await client.Album.GetInfoAsync(artist, album);
                if (albumInfo?.Images != null && albumInfo.Images.Count > 0)
                {
                    var imgUrl = albumInfo.Images[albumInfo.Images.Count - 1].Url;
                    if (!string.IsNullOrEmpty(imgUrl) && imgUrl.Contains("http"))
                    {
                        return imgUrl;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(track))
            {
                var trackInfo = await client.Track.GetInfoAsync(track, artist);
                if (trackInfo?.Album?.Images != null && trackInfo.Album.Images.Count > 0)
                {
                    var imgUrl = trackInfo.Album.Images[trackInfo.Album.Images.Count - 1].Url;
                    if (!string.IsNullOrEmpty(imgUrl) && imgUrl.Contains("http"))
                    {
                        return imgUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch album art from Last.fm for {Title} - {Artist}.", songInfo.Title,
                songInfo.Artist);
        }

        return null;
    }
}
