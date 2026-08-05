using System.Collections.Specialized;
using System.Text.Json;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteFM.Abstractions;

namespace BetterLyrics.Core.ViewModels;

public partial class PlaybackSettingsControlViewModel : BaseViewModel
{
    private readonly IAppUIThreadProvider _appUiThreadProvider;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmService _lastFmService;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ITranslationService _translationService;
    private readonly IFilePickerProvider _filePickerProvider;
    public IGsmtcService GsmtcService { get; }

    public PlaybackSettingsControlViewModel(
        ISettingsService settingsService,
        IGsmtcService gsmtcService,
        ITranslationService libreTranslationService,
        ILastFmService lastFmService, IAppUIThreadProvider appUiThreadProvider,
        IPasswordVaultProvider passwordVaultProvider, IGlobalToastProvider globalToastProvider,
        IFilePickerProvider filePickerProvider, IDiscordService discordService)
    {
        GsmtcService = gsmtcService;

        _translationService = libreTranslationService;
        _appUiThreadProvider = appUiThreadProvider;
        _passwordVaultProvider = passwordVaultProvider;
        _globalToastProvider = globalToastProvider;
        _filePickerProvider = filePickerProvider;

        _lastFmService = lastFmService;
        _lastFmService.UserChanged += LastFMService_UserChanged;
        _lastFmService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;

        _discordService = discordService;
        _discordService.UserChanged += DiscordService_UserChanged;
        DiscordUser = _discordService.CurrentUser;
        DiscordUsername = DiscordUser != null ? $"{DiscordUser.Username}" : null;
        IsDiscordConnected = DiscordUser != null;

        AppSettings = settingsService.AppSettings;
        AppSettings.MediaSourceProvidersInfo.CollectionChanged += MediaSourceProvidersInfo_CollectionChanged;

        AppleMusicMediaUserToken =
            _passwordVaultProvider.Get(Core.Constants.App.AppName, AppleMusic.MediaUserTokenKey) ??
            "";

        SelectedTargetLanguageIndex = LanguageHelper.SupportedTranslationTargetLanguages.ToList().FindIndex(x =>
            x.LanguageCode == AppSettings.TranslationSettings.SelectedTargetLanguageCode);

        IsLastFmAuthenticated = _lastFmService.IsAuthenticated;
        LastFmUser = _lastFmService.User;

        SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
    }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial MediaSourceProviderInfo? SelectedMediaSourceProvider { get; set; }

    [ObservableProperty] public partial bool IsLastFmAuthenticated { get; set; }

    [ObservableProperty] public partial LastFMUser? LastFmUser { get; set; }

    private readonly IDiscordService _discordService;

    [ObservableProperty] public partial DiscordRPC.User? DiscordUser { get; set; }

    [ObservableProperty] public partial bool IsLibreTranslateServerTesting { get; set; } = false;

    [ObservableProperty] public partial bool IsLxMusicServerTesting { get; set; } = false;

    [ObservableProperty] public partial int SelectedTargetLanguageIndex { get; set; }

    [ObservableProperty] public partial string AppleMusicMediaUserToken { get; set; }

    private void MediaSourceProvidersInfo_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();
    }

    private void LastFMService_IsAuthenticatedChanged(object? sender, LastFMIsAuthenticatedChangedEventArgs e)
    {
        IsLastFmAuthenticated = e.IsAuthenticated;
    }

    private void LastFMService_UserChanged(object? sender, LastFMUserChangedEventArgs e)
    {
        LastFmUser = e.User;
    }

    private void DiscordService_UserChanged(object? sender, DiscordRPC.User? e)
    {
        _appUiThreadProvider.Execute(() =>
        {
            DiscordUser = e;
            DiscordUsername = e != null ? $"{e.Username}" : null;
            IsDiscordConnected = e != null;
        });
    }

    [ObservableProperty] public partial string? DiscordUsername { get; set; }

    [ObservableProperty] public partial bool IsDiscordConnected { get; set; }

    [RelayCommand]
    private async Task StopTrackAsync()
    {
        // 该方法应仅在针对内置播放器的 InfoBar 打开时调用
        await GsmtcService.StopAsync();
    }

    [RelayCommand]
    private async Task ImportMemoryReaderConfigAsync()
    {
        string[] fileTypeFilter = [".json"];
        var (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.SettingsWindow);
        if (filePath != null)
        {
            var json = await File.ReadAllTextAsync(filePath);
            SelectedMediaSourceProvider?.MemoryReaderConfig = JsonSerializer.Deserialize(json,
                SourceGenerationContext.Default.MemoryReaderConfig);
            _globalToastProvider.Show("ImportSettingsSuccess", null, MessageSeverity.Success);
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
                await _translationService.TranslateTextAsync(
                    "Hello, world!", AppSettings.TranslationSettings.SelectedTargetLanguageCode,
                    CancellationToken.None);
                _appUiThreadProvider.Execute(() =>
                {
                    _globalToastProvider.Show("SettingsPageServerTestSuccessInfo", null,
                        MessageSeverity.Success);
                });
            }
            catch (Exception)
            {
                _appUiThreadProvider.Execute(() =>
                {
                    _globalToastProvider.Show("SettingsPageServerTestFailedInfo", null, MessageSeverity.Error);
                });
            }

            _appUiThreadProvider.Execute(() => { IsLibreTranslateServerTesting = false; });
        });
    }

    [RelayCommand]
    private async Task LastFmAuthAsync()
    {
        await _lastFmService.AuthAsync();
    }

    [RelayCommand]
    private async Task LastFmUnAuthAsync()
    {
        await _lastFmService.UnAuthAsync();
    }

    [RelayCommand]
    private async Task LastFmRefreshAsync()
    {
        await _lastFmService.RefreshAsync();
    }

    [RelayCommand]
    private void LxMusicServerTest()
    {
        IsLxMusicServerTesting = true;
        _ = Task.Run(async () =>
        {
            var testResult =
                await NetHelper.CheckConnectivityAsync($"{AppSettings.GeneralSettings.LXMusicServer}/status");
            _appUiThreadProvider.Execute(() =>
            {
                if (testResult)
                    _globalToastProvider.Show("SettingsPageServerTestSuccessInfo", null, MessageSeverity.Success);
                else
                    _globalToastProvider.Show("SettingsPageServerTestFailedInfo", null, MessageSeverity.Error);

                IsLxMusicServerTesting = false;
            });
        });
    }

    [RelayCommand]
    private void SaveAmllTtmlDbBaseUrl()
    {
        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
        GsmtcService.UpdateLyrics();
    }

    [RelayCommand]
    private void SaveAppleMusicMediaUserToken()
    {
        _passwordVaultProvider.Delete(App.AppName, AppleMusic.MediaUserTokenKey);
        _passwordVaultProvider.Save(App.AppName, AppleMusic.MediaUserTokenKey,
            AppleMusicMediaUserToken);
        GsmtcService.UpdateLyrics();
        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
    }

    partial void OnSelectedTargetLanguageIndexChanged(int value)
    {
        AppSettings.TranslationSettings.SelectedTargetLanguageCode =
            LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
    }
}