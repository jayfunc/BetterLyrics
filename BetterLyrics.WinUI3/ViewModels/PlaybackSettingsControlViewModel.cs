using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslationService;
using BetterLyrics.WinUI3.Services.TransliterationService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteFM.Abstractions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PlaybackSettingsControlViewModel : BaseViewModel
    {
        public IGSMTCService GSMTCService;
        private readonly ITranslationService _translationService;
        private readonly ILastFMService _lastFMService;
        private readonly ISettingsService _settingsService;
        private readonly ITransliterationService _transliterationService;

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        [ObservableProperty] public partial MediaSourceProviderInfo? SelectedMediaSourceProvider { get; set; }

        [ObservableProperty] public partial bool IsLastFMAuthenticated { get; set; }

        [ObservableProperty] public partial LastFMUser? LastFMUser { get; set; }

        [ObservableProperty] public partial bool IsLibreTranslateServerTesting { get; set; } = false;

        [ObservableProperty] public partial bool IsLXMusicServerTesting { get; set; } = false;

        [ObservableProperty] public partial int SelectedTargetLanguageIndex { get; set; }

        [ObservableProperty] public partial string AppleMusicMediaUserToken { get; set; }

        [ObservableProperty] public partial double PlaybackListGridHeight { get; set; } = 0;

        [ObservableProperty] public partial bool IsConfigPanelOpened { get; set; } = false;

        [ObservableProperty] public partial Vector3 ConfigPanelTranslation { get; set; } = new();

        public PlaybackSettingsControlViewModel(
            ISettingsService settingsService,
            IGSMTCService gsmtcService,
            ITranslationService libreTranslationService,
            ILastFMService lastFMService,
            ITransliterationService transliterationService)
        {
            GSMTCService = gsmtcService;

            _settingsService = settingsService;
            _translationService = libreTranslationService;
            _transliterationService = transliterationService;

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

        [RelayCommand]
        private async Task ImportMemoryReaderConfigAsync()
        {
            string[] fileTypeFilter = [".json"];
            var file = await PickerHelper.PickSingleFileAsync<SettingsWindow>(fileTypeFilter);
            if (file != null)
            {
                var json = File.ReadAllText(file.Path);
                SelectedMediaSourceProvider?.MemoryReaderConfig = JsonSerializer.Deserialize(json, Serialization.SourceGenerationContext.Default.MemoryReaderConfig);
                GlobalToastManager.Show( "ImportSettingsSuccess", null, InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private void LibreTranslateServerTest()
        {
            IsLibreTranslateServerTesting = true;
            Task.Run(async () =>
            {
                try
                {
                    string result = await _translationService.TranslateTextAsync(
                        "Hello, world!", AppSettings.TranslationSettings.SelectedTargetLanguageCode, new System.Threading.CancellationToken());
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        GlobalToastManager.Show("SettingsPageServerTestSuccessInfo", null, InfoBarSeverity.Success);
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        GlobalToastManager.Show("SettingsPageServerTestFailedInfo", null, InfoBarSeverity.Error);
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
                        GlobalToastManager.Show("SettingsPageServerTestSuccessInfo", null, InfoBarSeverity.Success);
                    }
                    else
                    {
                        GlobalToastManager.Show("SettingsPageServerTestFailedInfo", null, InfoBarSeverity.Error);
                    }
                    IsLXMusicServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void SaveAppleMusicMediaUserToken()
        {
            PasswordVaultHelper.Delete(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey);
            PasswordVaultHelper.Save(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey, AppleMusicMediaUserToken);
            GSMTCService.UpdateLyrics();
        }

        [RelayCommand]
        private void CloseConfigPanel()
        {
            IsConfigPanelOpened = false;
            ConfigPanelTranslation = new(0, (float)PlaybackListGridHeight, 0);
        }

        public void OpenConfigPanel()
        {
            IsConfigPanelOpened = true;
            ConfigPanelTranslation = new();
        }

        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            AppSettings.TranslationSettings.SelectedTargetLanguageCode = LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
        }

        partial void OnPlaybackListGridHeightChanged(double value)
        {
            if (IsConfigPanelOpened)
            {
                OpenConfigPanel();
            }
            else
            {
                CloseConfigPanel();
            }
        }
    }
}
