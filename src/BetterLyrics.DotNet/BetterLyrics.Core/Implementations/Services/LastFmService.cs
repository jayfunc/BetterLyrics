using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;

namespace BetterLyrics.Core.Implementations.Services;

public class LastFmService : ILastFmService
{
    private readonly LastfmClient _client;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmDialogProvider _lastFmDialogProvider;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;
    private readonly ILauncherProvider _launcherProvider;
    private string? _sessionKey;

    public LastFmService(ISettingsService settingsService,
        ISongSearchMapService songSearchMapService, IPasswordVaultProvider passwordVaultProvider,
        IGlobalToastProvider globalToastProvider,
        ILauncherProvider launcherProvider, ILastFmDialogProvider lastFmDialogProvider)
    {
        _settingsService = settingsService;
        _songSearchMapService = songSearchMapService;
        _passwordVaultProvider = passwordVaultProvider;
        _globalToastProvider = globalToastProvider;
        _launcherProvider = launcherProvider;
        _lastFmDialogProvider = lastFmDialogProvider;

        _client = new LastfmClient(LastFM.ApiKey, LastFM.SharedSecret);
        _sessionKey = _passwordVaultProvider.Get(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey);
        _ = UpdateAuthStatusAsync();
    }

    public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    public User? User { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public async Task ConfirmAuthAsync()
    {
        try
        {
            await _client.AuthenticateViaWebAsync();
            if (_client.Session != null && _client.Session.Authenticated)
            {
                _sessionKey = _client.Session.SessionKey;
                _passwordVaultProvider.Save(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey, _sessionKey);
                await UpdateAuthStatusAsync();
                return;
            }
            throw new Exception("Authentication failed");
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("LastFMAuthFailed", ex.Message, MessageSeverity.Error);
        }
    }

    public async Task ConfirmUnAuthAsync()
    {
        _sessionKey = null;
        _passwordVaultProvider.Delete(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey);
        await UpdateAuthStatusAsync();
    }

    public async Task AuthAsync()
    {
        var url = await _client.GetWebAuthenticationUrlAsync();
        
        _ = _launcherProvider.LaunchUriAsync(new Uri(url));

        await _lastFmDialogProvider.ShowAuthDialogAsync(ConfirmAuthAsync);
    }

    public async Task UnAuthAsync()
    {
        await _launcherProvider.LaunchUriAsync(new Uri(LastFM.UnAuthUrl));
        await _lastFmDialogProvider.ShowUnAuthDialogAsync(ConfirmUnAuthAsync);
    }

    public async Task TrackAsync(SongInfo songInfo)
    {
        if (IsAuthenticated)
        {
            var (mappedTitle, mappedArtist, mappedAlbum) =
                await _songSearchMapService.GetMappingAsync(songInfo);

            try
            {
                var scrobble = new Scrobble
                {
                    Track = mappedTitle,
                    Artist = mappedArtist,
                    Album = mappedAlbum,
                    Date = DateTime.UtcNow
                };
                var resp = await _client.Track.ScrobbleAsync(scrobble);
                if (resp != null && resp.Accepted == 0)
                {
                    _globalToastProvider.Show("LastFMScrobbleFailed", resp.Ignored > 0 ? "Scrobble ignored" : "Scrobble failed", MessageSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("LastFMScrobbleFailed", ex.Message, MessageSeverity.Error);
            }
        }
    }

    public async Task RefreshAsync()
    {
        await UpdateAuthStatusAsync();
    }

    private async Task UpdateAuthStatusAsync()
    {
        IsAuthenticated = !string.IsNullOrEmpty(_sessionKey);
        if (IsAuthenticated)
        {
             _client.Session.SessionKey = _sessionKey;
        }
        IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
        if (IsAuthenticated)
        {
            try
            {
                User = await _client.User.GetInfoAsync(null);
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
                User = null;
            }
        }
        else
        {
            User = null;
        }

        UserChanged?.Invoke(this, new LastFMUserChangedEventArgs(User));
    }

    public uint GetUnixTimeStamp()
    {
        return (uint)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
    }
}