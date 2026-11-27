using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.MediaFieldCorrection;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hqub.Lastfm.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class PlaybackSettingsControlViewModel : ObservableRecipient, ITransientViewModel,
    IRecipient<PlaybackLibSettingsNavigationRequestedMessage>
{
    private readonly IAppUIThreadProvider _appUiThreadProvider;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmService _lastFmService;
    private readonly IListenBrainzService _listenBrainzService;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ITranslationService _translationService;
    private readonly IFilePickerProvider _filePickerProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<PlaybackSettingsControlViewModel> _logger;

    public IGsmtcService GsmtcService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial MediaSourceProviderInfo? SelectedMediaSourceProvider { get; set; }

    /// <summary>
    /// 「插入示例」列表，直接展示图案原文。
    /// </summary>
    public ObservableCollection<MediaFieldCorrectionSnippetItemViewModel> FieldCorrectionSnippets { get; } = [];

    /// <summary>
    /// 当前播放器的字段纠正规则列表（已翻译成自然语言）。
    /// </summary>
    public ObservableCollection<MediaFieldRuleItemViewModel> FieldCorrectionRules { get; } = [];

    [ObservableProperty] public partial bool HasFieldCorrectionRules { get; set; }

    [ObservableProperty] public partial bool CanResetFieldCorrectionRules { get; set; }

    [ObservableProperty] public partial string FieldCorrectionPreviewHeader { get; set; } = string.Empty;

    /// <summary>
    /// 只列出被规则改动过的字段；没有变化时显示提示语。
    /// </summary>
    [ObservableProperty] public partial string FieldCorrectionPreviewText { get; set; } = string.Empty;

    private MediaSourceProviderInfo? _fieldCorrectionProvider;

    public ObservableCollection<PlaybackLibSettingsNavMenuItem> NavItems = [];

    [ObservableProperty] public partial PlaybackLibSettingsNavMenuItem? SelectedNavItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAuthorizeLastFm))]
    public partial bool IsLastFmAuthenticated { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAuthorizeLastFm))]
    public partial bool IsLastFmConfigured { get; set; }

    [ObservableProperty] public partial User? LastFmUser { get; set; }

    [ObservableProperty] public partial string LastFmApiKey { get; set; }

    [ObservableProperty] public partial string LastFmSharedSecret { get; set; }

    public bool CanAuthorizeLastFm => IsLastFmConfigured && !IsLastFmAuthenticated;

    [ObservableProperty] public partial bool IsListenBrainzAuthenticated { get; set; }

    [ObservableProperty] public partial string? ListenBrainzUsername { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveListenBrainzToken))]
    public partial string ListenBrainzToken { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveListenBrainzToken))]
    public partial bool IsListenBrainzSavingToken { get; set; }

    public bool CanSaveListenBrainzToken =>
        !string.IsNullOrWhiteSpace(ListenBrainzToken) && !IsListenBrainzSavingToken;

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
    ILastFmService lastFmService, IListenBrainzService listenBrainzService,
    IAppUIThreadProvider appUiThreadProvider, IPasswordVaultProvider passwordVaultProvider,
    IGlobalToastProvider globalToastProvider, IFilePickerProvider filePickerProvider, IDiscordService discordService,
    ILocalizationService localizationService, ILogger<PlaybackSettingsControlViewModel> logger)
    {
        GsmtcService = gsmtcService;

        _logger = logger;
        _translationService = libreTranslationService;
        _appUiThreadProvider = appUiThreadProvider;
        _localizationService = localizationService;
        _passwordVaultProvider = passwordVaultProvider;
        _globalToastProvider = globalToastProvider;
        _filePickerProvider = filePickerProvider;

        _lastFmService = lastFmService;
        _listenBrainzService = listenBrainzService;

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
        IsLastFmConfigured = _lastFmService.IsConfigured;
        LastFmUser = _lastFmService.User;
        LastFmApiKey = _lastFmService.ApiKey ?? string.Empty;
        LastFmSharedSecret = _lastFmService.SharedSecret ?? string.Empty;

        IsListenBrainzAuthenticated = _listenBrainzService.IsAuthenticated;
        ListenBrainzUsername = _listenBrainzService.Username;
        ListenBrainzToken = _listenBrainzService.UserToken ?? string.Empty;

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

        foreach (var snippet in MediaFieldCorrectionPresets.Snippets)
        {
            FieldCorrectionSnippets.Add(new MediaFieldCorrectionSnippetItemViewModel(
                snippet, _localizationService.GetLocalizedString(snippet.DescriptionKey)));
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        _lastFmService.UserChanged += LastFMService_UserChanged;
        _lastFmService.IsAuthenticatedChanged += LastFMService_IsAuthenticatedChanged;
        _listenBrainzService.AuthenticationChanged += ListenBrainzService_AuthenticationChanged;
        _discordService.UserChanged += DiscordService_UserChanged;
        AppSettings.MediaSourceProvidersInfo.CollectionChanged += MediaSourceProvidersInfo_CollectionChanged;
        GsmtcService.PropertyChanged += GsmtcService_PropertyChanged;

        // 重新激活时恢复规则监听，保证面板重新打开后规则编辑仍然实时生效
        AttachFieldCorrectionRules(SelectedMediaSourceProvider);

        // 重新激活时同步一次外部状态，避免控件隐藏期间状态变旧
        IsLastFmAuthenticated = _lastFmService.IsAuthenticated;
        IsLastFmConfigured = _lastFmService.IsConfigured;
        LastFmUser = _lastFmService.User;
        SyncListenBrainzState();
        DiscordUser = _discordService.CurrentUser;
        DiscordUsername = DiscordUser != null ? $"{DiscordUser.Username}" : null;
        IsDiscordConnected = DiscordUser != null;
    }

    protected override void OnDeactivated()
    {
        _lastFmService.UserChanged -= LastFMService_UserChanged;
        _lastFmService.IsAuthenticatedChanged -= LastFMService_IsAuthenticatedChanged;
        _listenBrainzService.AuthenticationChanged -= ListenBrainzService_AuthenticationChanged;
        _discordService.UserChanged -= DiscordService_UserChanged;
        AppSettings.MediaSourceProvidersInfo.CollectionChanged -= MediaSourceProvidersInfo_CollectionChanged;
        GsmtcService.PropertyChanged -= GsmtcService_PropertyChanged;

        DetachFieldCorrectionRules();

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

        AttachFieldCorrectionRules(value);
    }

    private void GsmtcService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IGsmtcService.CurrentRawSongInfo))
        {
            RefreshFieldCorrectionPreview();
        }
    }

    private void DetachFieldCorrectionRules()
    {
        if (_fieldCorrectionProvider == null) return;

        _fieldCorrectionProvider.FieldMappingRules.CollectionChanged -= FieldMappingRules_CollectionChanged;
        _fieldCorrectionProvider.FieldMappingRules.ItemPropertyChanged -= FieldMappingRules_ItemPropertyChanged;
        _fieldCorrectionProvider = null;
    }

    private void AttachFieldCorrectionRules(MediaSourceProviderInfo? provider)
    {
        DetachFieldCorrectionRules();

        _fieldCorrectionProvider = provider;

        if (provider != null)
        {
            provider.FieldMappingRules.CollectionChanged += FieldMappingRules_CollectionChanged;
            provider.FieldMappingRules.ItemPropertyChanged += FieldMappingRules_ItemPropertyChanged;
        }

        CanResetFieldCorrectionRules = PlayerIdHelper.IsAppleMusic(provider?.Provider);
        RefreshFieldCorrectionRules();
    }

    private void FieldMappingRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshFieldCorrectionRules();
    }

    private void FieldMappingRules_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
    {
        RefreshFieldCorrectionPreview();
    }

    /// <summary>
    /// 用当前播放器上报的原始字段演示规则效果；没有正在播放的歌曲时使用示例数据。
    /// </summary>
    public void RefreshFieldCorrectionPreview()
    {
        var raw = GsmtcService.CurrentRawSongInfo;

        var title = raw?.Title;
        var artist = raw?.Artist;
        var album = raw?.Album;

        FieldCorrectionPreviewHeader = raw == null
            ? GetFallbackString("MediaFieldPreviewHeaderSample", "Sample result (nothing is playing)")
            : GetFallbackString("MediaFieldPreviewHeaderCurrent", "Result for the current song");

        if (raw == null)
        {
            title = GetFallbackString("MediaFieldPreviewSampleTitle", "Sample Song (Live)");
            artist = GetFallbackString("MediaFieldPreviewSampleArtist", "Sample Artist — Sample Album - Single");
            album = GetFallbackString("MediaFieldPreviewSampleAlbum", "Sample Album - EP");
        }

        var corrected = MediaFieldCorrectionEngine.Apply(
            _fieldCorrectionProvider?.FieldMappingRules, title, artist, album);

        var rows = new List<string>();
        AppendPreviewRow(rows, MediaField.Title, title, corrected.Title);
        AppendPreviewRow(rows, MediaField.Artist, artist, corrected.Artist);
        AppendPreviewRow(rows, MediaField.Album, album, corrected.Album);

        FieldCorrectionPreviewText = rows.Count > 0
            ? string.Join(Environment.NewLine, rows)
            : GetFallbackString("MediaFieldPreviewNoChange", "No field changed");
    }

    private void AppendPreviewRow(List<string> rows, MediaField field, string? raw, string? corrected)
    {
        if (string.Equals(raw ?? string.Empty, corrected ?? string.Empty, StringComparison.Ordinal)) return;

        var label = MediaFieldRuleDescriber.GetFieldName(field, _localizationService);
        var arrow = GetFallbackString("MediaFieldCorrectionPreviewArrow", "→");
        rows.Add($"{label}: {raw} {arrow} {corrected}");
    }

    private string GetFallbackString(string key, string fallback)
    {
        var value = _localizationService.GetLocalizedString(key);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    public void RefreshFieldCorrectionRules()
    {
        foreach (var item in FieldCorrectionRules)
        {
            item.Detach();
        }

        FieldCorrectionRules.Clear();

        if (_fieldCorrectionProvider != null)
        {
            foreach (var rule in _fieldCorrectionProvider.FieldMappingRules)
            {
                FieldCorrectionRules.Add(new MediaFieldRuleItemViewModel(rule, _localizationService));
            }
        }

        HasFieldCorrectionRules = FieldCorrectionRules.Count > 0;
        RefreshFieldCorrectionPreview();
    }

    public MediaFieldMappingRule CreateFieldCorrectionRule()
    {
        return new MediaFieldMappingRule
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = string.Empty
        };
    }

    /// <summary>
    /// 规则编辑器需要示例数据做实时试算：优先用当前歌曲，没有则用示例值。
    /// </summary>
    public MediaFieldRuleEditorViewModel CreateFieldCorrectionRuleEditor(MediaFieldMappingRule rule)
    {
        var raw = GsmtcService.CurrentRawSongInfo;

        var sample = raw == null
            ? new MediaFieldCorrectionResult(
                GetFallbackString("MediaFieldPreviewSampleTitle", "Sample Song (Live)"),
                GetFallbackString("MediaFieldPreviewSampleArtist", "Sample Artist — Sample Album - Single"),
                GetFallbackString("MediaFieldPreviewSampleAlbum", "Sample Album - EP"))
            : new MediaFieldCorrectionResult(raw.Title, raw.Artist, raw.Album);

        return new MediaFieldRuleEditorViewModel(rule, _localizationService, sample);
    }

    public void AddFieldCorrectionSnippet(MediaFieldCorrectionSnippetItemViewModel? snippet)
    {
        if (_fieldCorrectionProvider == null || snippet == null) return;

        _fieldCorrectionProvider.FieldMappingRules.Add(snippet.CreateRule());
    }

    public void AddFieldCorrectionRule(MediaFieldMappingRule rule)
    {
        _fieldCorrectionProvider?.FieldMappingRules.Add(rule);
    }

    public void RemoveFieldCorrectionRule(MediaFieldRuleItemViewModel? item)
    {
        if (item == null) return;
        _fieldCorrectionProvider?.FieldMappingRules.Remove(item.Rule);
    }

    public void ClearFieldCorrectionRules()
    {
        _fieldCorrectionProvider?.FieldMappingRules.Clear();
    }

    /// <summary>
    /// 恢复该播放器的内置默认规则（目前只有 Apple Music 有默认规则）。
    /// </summary>
    public void ResetFieldCorrectionRules()
    {
        var provider = _fieldCorrectionProvider;
        if (provider == null) return;

        provider.FieldMappingRules.Clear();

        if (PlayerIdHelper.IsAppleMusic(provider.Provider))
        {
            foreach (var rule in MediaFieldCorrectionPresets.CreateAppleMusicRules())
            {
                provider.FieldMappingRules.Add(rule);
            }
        }
    }

    /// <summary>
    /// 列表拖拽排序完成后，把界面顺序写回配置（规则按顺序执行，顺序有意义）。
    /// </summary>
    public void PersistFieldCorrectionRuleOrder()
    {
        var provider = _fieldCorrectionProvider;
        if (provider == null) return;

        var ordered = FieldCorrectionRules.Select(x => x.Rule).ToList();
        if (ordered.Count != provider.FieldMappingRules.Count) return;
        if (ordered.SequenceEqual(provider.FieldMappingRules)) return;

        provider.FieldMappingRules.Clear();
        foreach (var rule in ordered)
        {
            provider.FieldMappingRules.Add(rule);
        }
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

    private void ListenBrainzService_AuthenticationChanged(object? sender,
        ListenBrainzAuthenticationChangedEventArgs e)
    {
        _appUiThreadProvider.Execute(() =>
        {
            IsListenBrainzAuthenticated = e.IsAuthenticated;
            ListenBrainzUsername = e.UserName;

            // The token was cleared (sign-out / server rejection), so the input must be emptied as well.
            if (!e.IsAuthenticated)
            {
                ListenBrainzToken = string.Empty;
            }
        });
    }

    private void SyncListenBrainzState()
    {
        IsListenBrainzAuthenticated = _listenBrainzService.IsAuthenticated;
        ListenBrainzUsername = _listenBrainzService.Username;
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

    private static readonly JsonSerializerOptions _fieldRulePackageJsonOptions = new()
    {
        WriteIndented = true,
        // 让导出的规则文件保持可读（中文、破折号等不会被转义）
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    [RelayCommand]
    private async Task ExportFieldCorrectionRulesAsync()
    {
        var provider = _fieldCorrectionProvider;
        if (provider == null || provider.FieldMappingRules.Count == 0) return;

        var (_, filePath) = await _filePickerProvider.PickSaveFileAsync(
            new Dictionary<string, IList<string>> { { "JSON", new List<string> { ".json" } } },
            "BetterLyrics_FieldRules.json",
            WindowType.SettingsWindow);

        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var package = new MediaFieldRulePackage
            {
                PlayerId = provider.Provider,
                Rules = [.. provider.FieldMappingRules]
            };

            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(package, _fieldRulePackageJsonOptions));
            _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export field correction rules.");
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task ImportFieldCorrectionRulesAsync()
    {
        var provider = _fieldCorrectionProvider;
        if (provider == null) return;

        string[] fileTypeFilter = [".json"];
        var (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.SettingsWindow);
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var package = JsonSerializer.Deserialize<MediaFieldRulePackage>(json, _fieldRulePackageJsonOptions);

            if (package?.Rules is not { Count: > 0 })
            {
                _globalToastProvider.Show("Error", null, MessageSeverity.Error);
                return;
            }

            provider.FieldMappingRules.Clear();
            foreach (var rule in package.Rules)
            {
                provider.FieldMappingRules.Add(rule.Clone());
            }

            _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to import field correction rules.");
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LibreTranslate server connectivity test failed.");
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
    private async Task SaveLastFmCredentialsAsync()
    {
        await _lastFmService.ConfigureAsync(LastFmApiKey, LastFmSharedSecret);

        // Reflect the normalized values (trimmed / cleared) back to the UI.
        LastFmApiKey = _lastFmService.ApiKey ?? string.Empty;
        LastFmSharedSecret = _lastFmService.SharedSecret ?? string.Empty;
        IsLastFmConfigured = _lastFmService.IsConfigured;
        IsLastFmAuthenticated = _lastFmService.IsAuthenticated;
        LastFmUser = _lastFmService.User;

        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
    }

    [RelayCommand]
    private async Task SaveListenBrainzTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(ListenBrainzToken))
        {
            return;
        }

        IsListenBrainzSavingToken = true;
        try
        {
            if (await _listenBrainzService.SaveTokenAsync(ListenBrainzToken))
            {
                // Keep the stored token in the field so it is visible (masked) while connected.
                ListenBrainzToken = _listenBrainzService.UserToken ?? string.Empty;
                _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
            }
        }
        finally
        {
            IsListenBrainzSavingToken = false;
            SyncListenBrainzState();
        }
    }

    [RelayCommand]
    private async Task ClearListenBrainzTokenAsync()
    {
        await _listenBrainzService.ClearTokenAsync();
        ListenBrainzToken = string.Empty;
        SyncListenBrainzState();
        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
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