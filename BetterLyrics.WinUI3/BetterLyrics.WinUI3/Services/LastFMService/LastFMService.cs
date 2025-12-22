using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using Hqub.Lastfm;
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

        private readonly LastfmClient _client;

        public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
        public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

        public Hqub.Lastfm.Entities.User? User { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public LastFMService(ISettingsService settingsService, ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _settingsService = settingsService;

            _client = new LastfmClient(Constants.LastFM.ApiKey, Constants.LastFM.SharedSecret);
            _client.Session.SessionKey = PasswordVaultHelper.Get(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey) ?? string.Empty;
            UpdateAuthStatusAsync();
        }

        public async Task ConfirmAuth()
        {
            try
            {
                await _client.AuthenticateViaWebAsync();
                PasswordVaultHelper.Save(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey, _client.Session.SessionKey);
                await UpdateAuthStatusAsync();
            }
            catch (Exception)
            {
                ToastHelper.ShowToast("LastFMAuthFailed", null, InfoBarSeverity.Error);
            }
        }

        public async Task ConfirmUnAuthAsync()
        {
            _client.Session.SessionKey = "";
            PasswordVaultHelper.Delete(Constants.App.AppName, Constants.LastFM.SessionKeyCredentialKey);
            await UpdateAuthStatusAsync();
        }

        public async Task AuthAsync()
        {
            var dialogXamlRoot = WindowHook.GetWindow<SettingsWindow>()?.Content.XamlRoot;
            if (dialogXamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _localizationService.GetLocalizedString("LastFMRequestAuthTitle") ?? "",
                Content = _localizationService.GetLocalizedString("LastFMRequestAuthDesc") ?? "",
                PrimaryButtonText = _localizationService.GetLocalizedString("LastFMRequestAuthConfirm") ?? "",
                CloseButtonText = _localizationService.GetLocalizedString("Cancel") ?? "",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = dialogXamlRoot,
            };
            dialog.PrimaryButtonClick += async (s, args) =>
            {
                await ConfirmAuth();
            };

            string url = await _client.GetWebAuthenticationUrlAsync();
            await Launcher.LaunchUriAsync(new Uri(url));
            await dialog.ShowAsync();
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
            IsAuthenticated = _client.Session.Authenticated;
            IsAuthenticatedChanged?.Invoke(this, new LastFMIsAuthenticatedChangedEventArgs(IsAuthenticated));
            if (IsAuthenticated)
            {
                User = await _client.User.GetInfoAsync();
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
                await _client.Track.ScrobbleAsync(new Hqub.Lastfm.Entities.Scrobble
                {
                    Track = songInfo.Title,
                    Artist = songInfo.DisplayArtists,
                    Date = DateTime.Now,
                });
            }
        }

        public async Task RefreshAsync()
        {
            await UpdateAuthStatusAsync();
        }
    }
}
