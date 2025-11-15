using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hqub.Lastfm.Entities;
using Microsoft.UI.Dispatching;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PlaybackSettingsControlViewModel : BaseViewModel
    {
        public IMediaSessionsService MediaSessionsService;
        private readonly ITranslateService _libreTranslateService;
        private readonly ILastFMService _lastFMService;
        private readonly ISettingsService _settingsService;
        private readonly IResourceService _resourceService;

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
            ILastFMService lastFMService,
            IResourceService resourceService)
        {
            MediaSessionsService = mediaSessionsService;

            _settingsService = settingsService;
            _libreTranslateService = libreTranslateService;
            _resourceService = resourceService;

            _lastFMService = lastFMService;
            _lastFMService.UserChanged += LastFMService_UserChanged;
            _lastFMService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;

            AppSettings = _settingsService.AppSettings;
            AppSettings.MediaSourceProvidersInfo.CollectionChanged += MediaSourceProvidersInfo_CollectionChanged;

            AppleMusicMediaUserToken = PasswordVaultHelper.Get(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey) ?? "";

            SelectedTargetLanguageIndex = LanguageHelper.SupportedTranslationTargetLanguages.ToList().FindIndex(x => x.LanguageCode == AppSettings.TranslationSettings.SelectedTargetLanguageCode);

            IsLastFMAuthenticated = _lastFMService.IsAuthenticated;
            LastFMUser = _lastFMService.User;

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
                        DevWinUI.Growl.Success(_resourceService.GetLocalizedString("SettingsPageServerTestSuccessInfo"));
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        DevWinUI.Growl.Error(_resourceService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
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
                        DevWinUI.Growl.Success(_resourceService.GetLocalizedString("SettingsPageServerTestSuccessInfo"));
                    }
                    else
                    {
                        DevWinUI.Growl.Error(_resourceService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
                    }
                    IsLXMusicServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void SaveAppleMusicMediaUserToken()
        {
            PasswordVaultHelper.Save(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey, AppleMusicMediaUserToken);
            MediaSessionsService.UpdateLyrics();
        }

        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            AppSettings.TranslationSettings.SelectedTargetLanguageCode = LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
        }
    }
}
