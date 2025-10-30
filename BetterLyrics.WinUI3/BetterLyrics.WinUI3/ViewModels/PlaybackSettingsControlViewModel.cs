using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Hqub.Lastfm.Entities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PlaybackSettingsControlViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<LyricsSearchProvider?>>,
        IRecipient<PropertyChangedMessage<TranslationSearchProvider?>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ITranslateService _libreTranslateService;
        private readonly ILastFMService _lastFMService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial MediaSourceProviderInfo? SelectedMediaSourceProvider { get; set; }

        [ObservableProperty]
        public partial bool IsLastFMAuthenticated { get; set; }

        [ObservableProperty]
        public partial User? LastFMUser { get; set; }

        [ObservableProperty]
        public partial bool IsLibreTranslateServerTesting { get; set; } = false;

        [ObservableProperty]
        public partial bool IsLXMusicServerTesting { get; set; } = false;

        [ObservableProperty]
        public partial LyricsSearchProvider? LyricsSearchProvider { get; set; } = null;

        [ObservableProperty]
        public partial TranslationSearchProvider? TranslationSearchProvider { get; set; } = null;

        [ObservableProperty]
        public partial string OriginalLyricsRef { get; set; } = "about:blank";

        [ObservableProperty]
        public partial string TranslatedLyricsRef { get; set; } = "about:blank";

        [ObservableProperty]
        public partial int SelectedTargetLanguageIndex { get; set; }

        [ObservableProperty]
        public partial string AppleMusicMediaUserToken { get; set; }

        public PlaybackSettingsControlViewModel(
            ISettingsService settingsService,
            IMediaSessionsService mediaSessionsService,
            ITranslateService libreTranslateService,
            ILastFMService lastFMService)
        {
            _settingsService = settingsService;
            _mediaSessionsService = mediaSessionsService;
            _libreTranslateService = libreTranslateService;

            _lastFMService = lastFMService;
            _lastFMService.UserChanged += LastFMService_UserChanged;
            _lastFMService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;

            _mediaSessionsService.SongInfoChanged += MediaSessionsService_SongInfoChanged;

            AppSettings = _settingsService.AppSettings;
            AppSettings.MediaSourceProvidersInfo.CollectionChanged += MediaSourceProvidersInfo_CollectionChanged;

            AppleMusicMediaUserToken = PasswordVaultHelper.Get(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey) ?? "";

            SelectedTargetLanguageIndex = LanguageHelper.SupportedTranslationTargetLanguages.ToList().FindIndex(x => x.LanguageCode == AppSettings.TranslationSettings.SelectedTargetLanguageCode);

            IsLastFMAuthenticated = _lastFMService.IsAuthenticated;
            LastFMUser = _lastFMService.User;

            LyricsSearchProvider = _mediaSessionsService.LyricsSearchProvider;
            TranslationSearchProvider = _mediaSessionsService.TranslationSearchProvider;

            SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
        }

        private void MediaSourceProvidersInfo_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
        }

        private void LastFMService_IsAuthenticatedChanged(object? sender, Events.LastFMIsAuthenticatedChangedEventArgs e)
        {
            IsLastFMAuthenticated = e.IsAuthenticated;
        }

        private void LastFMService_UserChanged(object? sender, Events.LastFMUserChangedEventArgs e)
        {
            LastFMUser = e.User;
        }

        private void MediaSessionsService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            var current = AppSettings.MediaSourceProvidersInfo.Where(x => x.Provider == e.SongInfo?.PlayerId)?.FirstOrDefault();
            if (_mediaSessionsService.Position.TotalSeconds <= 1 && current?.ResetPositionOffsetOnSongChanged == true)
            {
                current.PositionOffset = 0;
            }
        }

        private void MediaSessionsService_SessionIdsChanged(object? sender, Events.MediaSourceProvidersInfoEventArgs e)
        {
            SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
        }

        [RelayCommand]
        private void LibreTranslateServerTest()
        {
            IsLibreTranslateServerTesting = true;
            Task.Run(async () =>
            {
                try
                {
                    string result = await _libreTranslateService.TranslateTextAsync(
                        "Hello, world!", AppSettings.TranslationSettings.SelectedTargetLanguageCode, new System.Threading.CancellationToken());
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), InfoBarSeverity.Success);
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), InfoBarSeverity.Error);
                    });
                }
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsLibreTranslateServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private async Task LastFMAuthAsync()
        {
            await _lastFMService.AuthAsync();
        }

        [RelayCommand]
        private async Task LastFMUnAuthAsync()
        {
            await _lastFMService.UnAuthAsync();
        }

        [RelayCommand]
        private async Task LastFMRefreshAsync()
        {
            await _lastFMService.RefreshAsync();
        }

        [RelayCommand]
        private void LXMusicServerTest()
        {
            IsLXMusicServerTesting = true;
            Task.Run(async () =>
            {
                bool testResult = await NetHelper.CheckConnectivity($"{AppSettings.GeneralSettings.LXMusicServer}/status");
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    if (testResult)
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), InfoBarSeverity.Success);
                    }
                    else
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), InfoBarSeverity.Error);
                    }
                    IsLXMusicServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void SaveAppleMusicMediaUserToken()
        {
            PasswordVaultHelper.Save(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey, AppleMusicMediaUserToken);
            _mediaSessionsService.UpdateLyrics();
        }

        public void Receive(PropertyChangedMessage<LyricsSearchProvider?> message)
        {
            if (message.Sender is MediaSessionsService)
            {
                if (message.PropertyName == nameof(MediaSessionsService.LyricsSearchProvider))
                {
                    LyricsSearchProvider = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TranslationSearchProvider?> message)
        {
            if (message.Sender is MediaSessionsService)
            {
                if (message.PropertyName == nameof(MediaSessionsService.TranslationSearchProvider))
                {
                    TranslationSearchProvider = message.NewValue;
                }
            }
        }

        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            AppSettings.TranslationSettings.SelectedTargetLanguageCode = LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
        }
    }
}
