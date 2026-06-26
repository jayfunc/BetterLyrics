using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteFM.Abstractions;
using System;
using System.IO;
using System.Linq;
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

            AppleMusicMediaUserToken =
                PasswordVaultHelper.Get(Core.Constants.App.AppName, Core.Constants.AppleMusic.MediaUserTokenKey) ?? "";

            SelectedTargetLanguageIndex = LanguageHelper.SupportedTranslationTargetLanguages.ToList().FindIndex(x =>
                x.LanguageCode == AppSettings.TranslationSettings.SelectedTargetLanguageCode);

            IsLastFMAuthenticated = _lastFMService.IsAuthenticated;
            LastFMUser = _lastFMService.User;

            SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
        }

        private void MediaSourceProvidersInfo_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
        }

        private void LastFMService_IsAuthenticatedChanged(object? sender, LastFMIsAuthenticatedChangedEventArgs e)
        {
            IsLastFMAuthenticated = e.IsAuthenticated;
        }

        private void LastFMService_UserChanged(object? sender, LastFMUserChangedEventArgs e)
        {
            LastFMUser = e.User;
        }

        [RelayCommand]
        private async Task StopTrackAsync()
        {
            // 该方法应仅在针对内置播放器的 InfoBar 打开时调用
            await GSMTCService.StopAsync();
        }

        [RelayCommand]
        private async Task ImportMemoryReaderConfigAsync()
        {
            string[] fileTypeFilter = [".json"];
            var file = await PickerHelper.PickSingleFileAsync<SettingsWindow>(fileTypeFilter);
            if (file != null)
            {
                var json = File.ReadAllText(file.Path);
                SelectedMediaSourceProvider?.MemoryReaderConfig = JsonSerializer.Deserialize(json,
                    Core.Serialization.SourceGenerationContext.Default.MemoryReaderConfig);
                GlobalToastManager.Show("ImportSettingsSuccess", null, MessageSeverity.Success);
            }
        }

        [RelayCommand]
        private void LibreTranslateServerTest()
        {
            IsLibreTranslateServerTesting = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    string result = await _translationService.TranslateTextAsync(
                        "Hello, world!", AppSettings.TranslationSettings.SelectedTargetLanguageCode,
                        new System.Threading.CancellationToken());
                    AppUIThread.Execute(() =>
                    {
                        GlobalToastManager.Show("SettingsPageServerTestSuccessInfo", null, MessageSeverity.Success);
                    });
                }
                catch (Exception)
                {
                    AppUIThread.Execute(() =>
                    {
                        GlobalToastManager.Show("SettingsPageServerTestFailedInfo", null, MessageSeverity.Error);
                    });
                }

                AppUIThread.Execute(() => { IsLibreTranslateServerTesting = false; });
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
            _ = Task.Run(async () =>
            {
                bool testResult =
                    await NetHelper.CheckConnectivityAsync($"{AppSettings.GeneralSettings.LXMusicServer}/status");
                AppUIThread.Execute(() =>
                {
                    if (testResult)
                    {
                        GlobalToastManager.Show("SettingsPageServerTestSuccessInfo", null, MessageSeverity.Success);
                    }
                    else
                    {
                        GlobalToastManager.Show("SettingsPageServerTestFailedInfo", null, MessageSeverity.Error);
                    }

                    IsLXMusicServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void SaveAppleMusicMediaUserToken()
        {
            PasswordVaultHelper.Delete(Core.Constants.App.AppName, Core.Constants.AppleMusic.MediaUserTokenKey);
            PasswordVaultHelper.Save(Core.Constants.App.AppName, Core.Constants.AppleMusic.MediaUserTokenKey,
                AppleMusicMediaUserToken);
            GSMTCService.UpdateLyrics();
        }

        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            AppSettings.TranslationSettings.SelectedTargetLanguageCode =
                LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
        }
    }
}