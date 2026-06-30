using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using LiteFM;
using LiteFM.Abstractions;
using LiteFM.Abstractions.ApiContracts;
using LiteFM.Api;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace BetterLyrics.WinUI3.Services
{
    public partial class LastFMService : ILastFMService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly ISongSearchMapService _songSearchMapService;

        private readonly LastFMClient _client;
        private string? _sessionKey;

        public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
        public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

        public LastFMUser? User { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public LastFMService(ISettingsService settingsService, ILocalizationService localizationService,
            ISongSearchMapService songSearchMapService)
        {
            _localizationService = localizationService;
            _settingsService = settingsService;
            _songSearchMapService = songSearchMapService;

            _client = new LastFMClient(new LastFMOptions() { ApiKey = LastFM.ApiKey, ApiSecret = LastFM.SharedSecret });
            _sessionKey = PasswordVaultHelper.Get(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey);
            _ = UpdateAuthStatusAsync();
        }

        public async Task ConfirmAuthAsync(string param)
        {
            var resp = await _client.RequestAsync(LastFMApi.GetSessionApi, new GetSessionRequest() { Token = param });
            if (resp.IsSuccess)
            {
                _sessionKey = resp.Response!.Session!.Key;
                PasswordVaultHelper.Save(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey, _sessionKey);
                await UpdateAuthStatusAsync();
            }
            else
            {
                GlobalToastManager.Show("LastFMAuthFailed", resp.Error?.Message, MessageSeverity.Error);
            }
        }

        public async Task ConfirmUnAuthAsync()
        {
            _sessionKey = null;
            PasswordVaultHelper.Delete(Core.Constants.App.AppName, LastFM.SessionKeyCredentialKey);
            await UpdateAuthStatusAsync();
        }

        public async Task AuthAsync()
        {
            string url =
                $"https://www.last.fm/api/auth?api_key={_client.Options.ApiKey}&cb=betterlyrics://link.last.fm";
            _ = Launcher.LaunchUriAsync(new Uri(url));
        }

        public async Task UnAuthAsync()
        {
            var dialogXamlRoot = WindowHook.GetWindow<SettingsWindow>()?.Content.XamlRoot;
            if (dialogXamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _localizationService.GetLocalizedString("LastFMRequestUnAuthTitle") ?? "",
                Content = _localizationService.GetLocalizedString("LastFMRequestUnAuthDesc") ?? "",
                PrimaryButtonText = _localizationService.GetLocalizedString("LastFMRequestUnAuthConfirm") ?? "",
                CloseButtonText = _localizationService.GetLocalizedString("Cancel") ?? "",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = dialogXamlRoot,
            };
            dialog.PrimaryButtonClick += async (s, args) => { await ConfirmUnAuthAsync(); };

            await Launcher.LaunchUriAsync(new Uri(LastFM.UnAuthUrl));
            await dialog.ShowAsync();
        }

        private async Task UpdateAuthStatusAsync()
        {
            IsAuthenticated = !(string.IsNullOrEmpty(_sessionKey));
            IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
            if (IsAuthenticated)
            {
                var resp = await _client.RequestAsync(LastFMApi.GetUserInfoApi,
                    new GetUserInfoRequest() { User = null }, _sessionKey);
                User = resp.Response?.User;
                //if(!resp.IsSuccess) GlobalToastManager.Show("LastFMGetUserFailed", resp.Error?.Message, InfoBarSeverity.Error);
            }
            else
            {
                User = null;
            }

            UserChanged?.Invoke(this, new LastFMUserChangedEventArgs(User));
        }

        public async Task TrackAsync(SongInfo songInfo)
        {
            if (IsAuthenticated)
            {
                (string mappedTitle, string mappedArtist, string mappedAlbum) =
                    await _songSearchMapService.GetMappingAsync(songInfo);

                var resp = await _client.RequestAsync(LastFMApi.ScrobbleApi, new()
                {
                    Track = mappedTitle,
                    Artist = mappedArtist,
                    Album = mappedAlbum,
                    TimeStamp = GetUnixTimeStamp()
                }, _sessionKey);
                if (!resp.IsSuccess)
                {
                    GlobalToastManager.Show("LastFMScrobbleFailed", resp.Error?.Message, MessageSeverity.Error);
                }
            }
        }

        public async Task RefreshAsync()
        {
            await UpdateAuthStatusAsync();
        }

        public uint GetUnixTimeStamp()
        {
            return (uint)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        }
    }
}