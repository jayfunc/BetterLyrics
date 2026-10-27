using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class AppSettingsControlViewModel : ObservableRecipient, ITransientViewModel,
    IRecipient<LanguageCodeChangedMessage>,
    IRecipient<GlobalFontFamilyChangedMessage>,
    IRecipient<EnhanceControlInteractiveAnimationsChangedMessage>,
    IRecipient<WriteDebugLogsChangedMessage>,
    IRecipient<AppSettingsNavigationRequestedMessage>
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherProvider _launcherProvider;
    private readonly ILocalizationService _localizationService;

    private readonly string _initialLanguageCode;
    private readonly string _initialGlobalFontFamily;
    private readonly bool _initialEnhanceControlInteractiveAnimations;
    private readonly bool _initialWriteDebugLogs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsLanguageChanged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsGlobalFontChanged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsEnhanceControlInteractiveAnimationsChanged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsWriteDebugLogsChanged { get; set; }

    public bool IsRestartRequired => IsLanguageChanged || IsGlobalFontChanged ||
                                     IsEnhanceControlInteractiveAnimationsChanged || IsWriteDebugLogsChanged;

    public ObservableCollection<AppSettingsNavMenuItem> NavItems { get; } = [];
    [ObservableProperty] public partial AppSettingsNavMenuItem? SelectedNavItem { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial string GcObjectCheckSummary { get; set; } = string.Empty;

    public ObservableCollection<string> AliveGcObjects { get; } = [];

    public AppSettingsControlViewModel(ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider, ILauncherProvider launcherProvider, ILocalizationService localizationService)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _launcherProvider = launcherProvider;
        _localizationService = localizationService;
        AppSettings = _settingsService.AppSettings;

        _initialLanguageCode = AppSettings.GeneralSettings.LanguageCode;
        _initialGlobalFontFamily = AppSettings.GeneralSettings.GlobalFontFamily;
        _initialEnhanceControlInteractiveAnimations = AppSettings.GeneralSettings.EnhanceControlInteractiveAnimations;
        _initialWriteDebugLogs = AppSettings.AdvancedSettings.WriteDebugLogs;

        NavItems.Add(new AppSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAppAppearance/Text"),
            Section = AppSettingsSection.Appearance
        });
        NavItems.Add(new AppSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageWindow/Text"),
            Section = AppSettingsSection.Window
        });
        NavItems.Add(new AppSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageShortcut/Text"),
            Section = AppSettingsSection.Shortcut
        });
        NavItems.Add(new AppSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("AppSettingsControlSystemTray/Text"),
            Section = AppSettingsSection.SystemTray
        });
        NavItems.Add(new AppSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAdvanced/Text"),
            Section = AppSettingsSection.Advanced
        });
        SelectedNavItem = NavItems[0];
    }

    partial void OnSelectedNavItemChanged(AppSettingsNavMenuItem? value)
    {
        WeakReferenceMessenger.Default.Send(
            new SettingsSectionChangedMessage(this, SettingsSection.App, SelectedNavItem?.Section));
    }

    public void Receive(LanguageCodeChangedMessage message)
    {
        IsLanguageChanged = message.Value != _initialLanguageCode;
    }

    public void Receive(GlobalFontFamilyChangedMessage message)
    {
        IsGlobalFontChanged = message.Value != _initialGlobalFontFamily;
    }

    public void Receive(EnhanceControlInteractiveAnimationsChangedMessage message)
    {
        IsEnhanceControlInteractiveAnimationsChanged = message.Value != _initialEnhanceControlInteractiveAnimations;
    }

    public void Receive(WriteDebugLogsChangedMessage message)
    {
        IsWriteDebugLogsChanged = message.Value != _initialWriteDebugLogs;
    }

    public void Receive(AppSettingsNavigationRequestedMessage message)
    {
        SelectedNavItem = NavItems.FirstOrDefault(item => item.Section == message.Value);
    }

    [RelayCommand]
    private async Task OpenTaskMgrStartupAppsAsync()
    {
        await _launcherProvider.LaunchUriAsync(new Uri("ms-settings:startupapps"));
    }

    [RelayCommand]
    private void RestartApp()
    {
        _windowManagerProvider.RestartApp();
    }

    [RelayCommand]
    private async Task CheckGcObjectsAsync()
    {
        AliveGcObjects.Clear();
        var aliveObjects = await MemoryLeakDetector.CheckAliveObjectsAsync();

        foreach (var name in aliveObjects)
        {
            AliveGcObjects.Add(name);
        }

        GcObjectCheckSummary = _localizationService.GetLocalizedString(
            aliveObjects.Count == 0 ? "SettingsPageGcObjectsNone" : "SettingsPageGcObjectsFound");
    }
}