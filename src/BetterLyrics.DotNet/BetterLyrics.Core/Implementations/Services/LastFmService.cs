using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using LiteFM;
using LiteFM.Abstractions;
using LiteFM.Abstractions.ApiContracts;
using LiteFM.Api;

namespace BetterLyrics.Core.Implementations.Services;

public class LastFmService : ILastFmService
{
    private readonly LastFMClient _client;
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

        _client = new LastFMClient(new LastFMOptions { ApiKey = LastFM.ApiKey, ApiSecret = LastFM.SharedSecret });
        _sessionKey = _passwordVaultProvider.Get(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey);
        _ = UpdateAuthStatusAsync();
    }

    public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    public LastFMUser? User { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public async Task ConfirmAuthAsync(string param)
    {
        var resp = await _client.RequestAsync(LastFMApi.GetSessionApi, new GetSessionRequest { Token = param });
        if (resp.IsSuccess)
        {
            _sessionKey = resp.Response!.Session!.Key;
            _passwordVaultProvider.Save(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey, _sessionKey);
            await UpdateAuthStatusAsync();
        }
        else
        {
            _globalToastProvider.Show("LastFMAuthFailed", resp.Error?.Message, MessageSeverity.Error);
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
        var url = $"https://www.last.fm/api/auth?api_key={_client.Options.ApiKey}&cb=betterlyrics://link.last.fm";
        _ = _launcherProvider.LaunchUriAsync(new Uri(url));

        await _lastFmDialogProvider.ShowAuthDialogAsync();
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

            var resp = await _client.RequestAsync(LastFMApi.ScrobbleApi, new ScrobbleRequest
            {
                Track = mappedTitle,
                Artist = mappedArtist,
                Album = mappedAlbum,
                TimeStamp = GetUnixTimeStamp()
            }, _sessionKey);
            if (!resp.IsSuccess)
                _globalToastProvider.Show("LastFMScrobbleFailed", resp.Error?.Message, MessageSeverity.Error);
        }
    }

    public async Task RefreshAsync()
    {
        await UpdateAuthStatusAsync();
    }

    private async Task UpdateAuthStatusAsync()
    {
        IsAuthenticated = !string.IsNullOrEmpty(_sessionKey);
        IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
        if (IsAuthenticated)
        {
            var resp = await _client.RequestAsync(LastFMApi.GetUserInfoApi,
                new GetUserInfoRequest { User = null }, _sessionKey);
            User = resp.Response?.User;
            if(!resp.IsSuccess) _globalToastProvider.Show("Error", resp.Error?.Message, MessageSeverity.Error);
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