using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using LiteFM;
using LiteFM.Abstractions;
using LiteFM.Abstractions.ApiContracts;
using LiteFM.Api;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.System;
namespace BetterLyrics.WinUI3.Services.LastFMService
{
    public partial class LastFMService : ILastFMService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private readonly LastFMClient _client;
        private string? _sessionKey;

        public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
        public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

        public LastFMUser? User { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public LastFMService(ISettingsService settingsService, ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _settingsService = settingsService;

            _client = new LastFMClient(new LastFMOptions() { ApiKey = Constants.LastFM.ApiKey, ApiSecret = Constants.LastFM.SharedSecret });
            _sessionKey = PasswordVaultHelper.Get(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey);
            _ = UpdateAuthStatusAsync();
        }

        public async Task ConfirmAuth(string param)
        {
            var resp = await _client.RequestAsync(LastFMApi.GetSessionApi, new GetSessionRequest() { Token = param });
            if (resp.IsSuccess)
            {
                _sessionKey = resp.Response!.Session!.Key;
                PasswordVaultHelper.Save(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey, _sessionKey);
                await UpdateAuthStatusAsync();
            }
            else
            {
                GlobalToastManager.Show("LastFMAuthFailed", resp.Error?.Message, InfoBarSeverity.Error);
            }
        }

        public async Task ConfirmUnAuthAsync()
        {
            _sessionKey = null;
            PasswordVaultHelper.Delete(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey);
            await UpdateAuthStatusAsync();
        }

        public async Task AuthAsync()
        {
            string url = $"https://www.last.fm/api/auth?api_key={_client.Options.ApiKey}&cb=betterlyrics://link.last.fm";
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
            dialog.PrimaryButtonClick += async (s, args) =>
            {
                await ConfirmUnAuthAsync();
            };

            await Launcher.LaunchUriAsync(new Uri(Constants.LastFM.UnAuthUrl));
            await dialog.ShowAsync();
        }

        private async Task UpdateAuthStatusAsync()
        {
            IsAuthenticated = !(string.IsNullOrEmpty(_sessionKey));
            IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
            if (IsAuthenticated)
            {
                var resp = await _client.RequestAsync(LastFMApi.GetUserInfoApi, new GetUserInfoRequest() { User = null }, _sessionKey);
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
                var resp = await _client.RequestAsync(LastFMApi.ScrobbleApi, new()
                {
                    Track = songInfo.Title,
                    Artist = songInfo.Artist,
                    Album = songInfo.Album,
                    TimeStamp = GetUnixTimeStamp()
                }, _sessionKey);
                if (!resp.IsSuccess)
                {
                    GlobalToastManager.Show("LastFMScrobbleFailed", resp.Error?.Message, InfoBarSeverity.Error);
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
