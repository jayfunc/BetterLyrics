using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hqub.Lastfm;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace BetterLyrics.WinUI3.Services.LastFMService
{
    public partial class LastFMService : ILastFMService
    {
        private readonly ISettingsService _settingsService;
        private readonly LastfmClient _client;

        public event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
        public event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

        public Hqub.Lastfm.Entities.User? User { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public LastFMService(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            _client = new LastfmClient(Constants.LastFM.ApiKey, Constants.LastFM.SharedSecret);
            _client.Session.SessionKey = _settingsService.LastFMSessionKey;
            UpdateAuthStatusAsync();
        }

        public async Task ConfirmAuth()
        {
            try
            {
                await _client.AuthenticateViaWebAsync();
                _settingsService.LastFMSessionKey = _client.Session.SessionKey;
                await UpdateAuthStatusAsync();
            }
            catch (Exception)
            {
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("LastFMAuthFailed") ?? "", InfoBarSeverity.Error);
            }
        }

        public async Task ConfirmUnAuthAsync()
        {
            _client.Session.SessionKey = "";
            _settingsService.LastFMSessionKey = "";
            await UpdateAuthStatusAsync();
        }

        public async Task AuthAsync()
        {
            var dialogXamlRoot = WindowHelper.GetWindowByWindowType<SettingsWindow>()?.Content.XamlRoot;
            if (dialogXamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = App.ResourceLoader?.GetString("LastFMRequestAuthTitle") ?? "",
                Content = App.ResourceLoader?.GetString("LastFMRequestAuthDesc") ?? "",
                PrimaryButtonText = App.ResourceLoader?.GetString("LastFMRequestAuthConfirm") ?? "",
                CloseButtonText = App.ResourceLoader?.GetString("Cancel") ?? "",
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
            var dialogXamlRoot = WindowHelper.GetWindowByWindowType<SettingsWindow>()?.Content.XamlRoot;
            if (dialogXamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = App.ResourceLoader?.GetString("LastFMRequestUnAuthTitle") ?? "",
                Content = App.ResourceLoader?.GetString("LastFMRequestUnAuthDesc") ?? "",
                PrimaryButtonText = App.ResourceLoader?.GetString("LastFMRequestUnAuthConfirm") ?? "",
                CloseButtonText = App.ResourceLoader?.GetString("Cancel") ?? "",
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
                    Artist = songInfo.Artist,
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
