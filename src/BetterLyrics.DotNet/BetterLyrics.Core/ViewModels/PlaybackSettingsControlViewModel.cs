using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hqub.Lastfm.Entities;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel，生命周期跟随 PlaybackSettingsControl 实例
/// （设置页与 NowPlayingBar 的浮出面板各自持有独立实例）。
/// </summary>
public partial class PlaybackSettingsControlViewModel : ObservableRecipient, ITransientViewModel,
    IRecipient<PlaybackLibSettingsNavigationRequestedMessage>
{
    private readonly IAppUIThreadProvider _appUiThreadProvider;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmService _lastFmService;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ITranslationService _translationService;
    private readonly IFilePickerProvider _filePickerProvider;
    private readonly ILocalizationService _localizationService;

    public IGsmtcService GsmtcService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial MediaSourceProviderInfo? SelectedMediaSourceProvider { get; set; }

    public ObservableCollection<PlaybackLibSettingsNavMenuItem> NavItems = [];

    [ObservableProperty] public partial PlaybackLibSettingsNavMenuItem? SelectedNavItem { get; set; }

    [ObservableProperty] public partial bool IsLastFmAuthenticated { get; set; }

    [ObservableProperty] public partial User? LastFmUser { get; set; }

    private readonly IDiscordService _discordService;

    [ObservableProperty] public partial DiscordRPC.User? DiscordUser { get; set; }

    [ObservableProperty] public partial bool IsLibreTranslateServerTesting { get; set; } = false;

    [ObservableProperty] public partial bool IsLxMusicServerTesting { get; set; } = false;

    [ObservableProperty] public partial int SelectedTargetLanguageIndex { get; set; }

    [ObservableProperty] public partial string AppleMusicMediaUserToken { get; set; }

    [ObservableProperty] public partial string? DiscordUsername { get; set; }

    [ObservableProperty] public partial bool IsDiscordConnected { get; set; }

    [ObservableProperty] public partial bool IsConfigPanelOpen { get; set; }

    public PlaybackSettingsControlViewModel(
    ISettingsService settingsService, IGsmtcService gsmtcService, ITranslationService libreTranslationService,
    ILastFmService lastFmService, IAppUIThreadProvider appUiThreadProvider, IPasswordVaultProvider passwordVaultProvider,
    IGlobalToastProvider globalToastProvider, IFilePickerProvider filePickerProvider, IDiscordService discordService,
    ILocalizationService localizationService)
    {
        GsmtcService = gsmtcService;

        _translationService = libreTranslationService;
        _appUiThreadProvider = appUiThreadProvider;
        _localizationService = localizationService;
        _passwordVaultProvider = passwordVaultProvider;
        _globalToastProvider = globalToastProvider;
        _filePickerProvider = filePickerProvider;

        _lastFmService = lastFmService;

        _discordService = discordService;
        DiscordUser = _discordService.CurrentUser;
        DiscordUsername = DiscordUser != null ? $"{DiscordUser.Username}" : null;
        IsDiscordConnected = DiscordUser != null;

        AppSettings = settingsService.AppSettings;

        AppleMusicMediaUserToken =
            _passwordVaultProvider.Get(App.AppName, AppleMusic.MediaUserTokenKey) ??
            "";

        SelectedTargetLanguageIndex = LanguageHelper.SupportedTranslationTargetLanguages.ToList().FindIndex(x =>
            x.LanguageCode == AppSettings.TranslationSettings.SelectedTargetLanguageCode);

        IsLastFmAuthenticated = _lastFmService.IsAuthenticated;
        LastFmUser = _lastFmService.User;

        SelectedMediaSourceProvider = AppSettings.MediaSourceProvidersInfo.FirstOrDefault();

        NavItems.Add(new PlaybackLibSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageConfigPlaybackSource/Text"),
            Section = PlaybackLibSettingsSection.General
        });
        NavItems.Add(new PlaybackLibSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsProcessing/Text"),
            Section = PlaybackLibSettingsSection.LyricsProcessing
        });
        NavItems.Add(new PlaybackLibSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageIntegration/Text"),
            Section = PlaybackLibSettingsSection.Integration
        });
        NavItems.Add(new PlaybackLibSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageRealtimeStatus/Text"),
            Section = PlaybackLibSettingsSection.RealtimeStatus
        });
        SelectedNavItem = NavItems[0];

        IsActive = true;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        _lastFmService.UserChanged += LastFMService_UserChanged;
        _lastFmService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;
        _discordService.UserChanged += DiscordService_UserChanged;
        AppSettings.MediaSourceProvidersInfo.CollectionChanged += MediaSourceProvidersInfo_CollectionChanged;

        // 重新激活时同步一次外部状态，避免控件隐藏期间状态变旧
        IsLastFmAuthenticated = _lastFmService.IsAuthenticated;
        LastFmUser = _lastFmService.User;
        DiscordUser = _discordService.CurrentUser;
        DiscordUsername = DiscordUser != null ? $"{DiscordUser.Username}" : null;
        IsDiscordConnected = DiscordUser != null;
    }

    protected override void OnDeactivated()
    {
        _lastFmService.UserChanged -= LastFMService_UserChanged;
        _lastFmService.IsAuthenticatedChanged -= LastFMService_IsAuthenticatedChanged;
        _discordService.UserChanged -= DiscordService_UserChanged;
        AppSettings.MediaSourceProvidersInfo.CollectionChanged -= MediaSourceProvidersInfo_CollectionChanged;

        base.OnDeactivated();
    }

    partial void OnSelectedTargetLanguageIndexChanged(int value)
    {
        AppSettings.TranslationSettings.SelectedTargetLanguageCode =
            LanguageHelper.SupportedTranslationTargetLanguages[value].LanguageCode;
    }

    partial void OnSelectedNavItemChanged(PlaybackLibSettingsNavMenuItem? value)
    {
        WeakReferenceMessenger.Default.Send(
            new SettingsSectionChangedMessage(this, SettingsSection.PlaybackLib, SelectedNavItem?.Section));
    }

    partial void OnSelectedMediaSourceProviderChanged(MediaSourceProviderInfo? value)
    {
        WeakReferenceMessenger.Default.Send(new SettingsSectionChangedMessage(
            this, SettingsSection.PlaybackLib, SelectedNavItem?.Section, SelectedMediaSourceProvider?.Provider));
    }

    private void MediaSourceProvidersInfo_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

            _appUiThreadProvider.Execute(() =>
            {
                IsLibreTranslateServerTesting = false;
            });
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

    [RelayCommand]
    private void CloseConfigPanel()
    {
        SelectedMediaSourceProvider = null;
        IsConfigPanelOpen = false;
    }

    [RelayCommand]
    private void ShowConfigPanel(MediaSourceProviderInfo? info)
    {
        SelectedMediaSourceProvider = info;
        IsConfigPanelOpen = info != null;
        SelectedNavItem = NavItems[0];
    }

    public void Receive(PlaybackLibSettingsNavigationRequestedMessage message)
    {
        var target = message.TargetSessionId != null
            ? AppSettings.MediaSourceProvidersInfo.FirstOrDefault(w => w.Provider == message.TargetSessionId)
            : null;

        ShowConfigPanel(target);
        SelectedNavItem = NavItems.FirstOrDefault(x => x.Section == message.TargetSection);
    }
}