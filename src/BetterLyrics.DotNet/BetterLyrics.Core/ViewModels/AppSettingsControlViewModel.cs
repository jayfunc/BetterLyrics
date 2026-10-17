using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
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
/// Singleton ViewModel
/// </summary>
public partial class AppSettingsControlViewModel : ObservableRecipient,
    IRecipient<LanguageCodeChangedMessage>,
    IRecipient<GlobalFontFamilyChangedMessage>,
    IRecipient<EnhanceControlInteractiveAnimationsChangedMessage>,
    IRecipient<AppSettingsNavigationRequestedMessage>
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherProvider _launcherProvider;
    private readonly ILocalizationService _localizationService;

    private readonly string _initialLanguageCode;
    private readonly string _initialGlobalFontFamily;
    private readonly bool _initialEnhanceControlInteractiveAnimations;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsLanguageChanged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsGlobalFontChanged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestartRequired))]
    public partial bool IsEnhanceControlInteractiveAnimationsChanged { get; set; }

    public bool IsRestartRequired => IsLanguageChanged || IsGlobalFontChanged || IsEnhanceControlInteractiveAnimationsChanged;

    public ObservableCollection<AppSettingsNavMenuItem> NavItems { get; } = [];
    [ObservableProperty] public partial AppSettingsNavMenuItem? SelectedNavItem { get; set; }

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
        SelectedNavItem = NavItems[0];

        IsActive = true;
    }

    partial void OnSelectedNavItemChanged(AppSettingsNavMenuItem? value)
    {
        WeakReferenceMessenger.Default.Send(new SettingsSectionChangedMessage(this));
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

    public void Receive(AppSettingsNavigationRequestedMessage message)
    {
        SelectedNavItem = NavItems.FirstOrDefault(item => item.Section == message.Value);
    }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

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
}