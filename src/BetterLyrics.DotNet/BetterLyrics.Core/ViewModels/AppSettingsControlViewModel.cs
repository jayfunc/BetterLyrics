using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.ViewModels;

public partial class AppSettingsControlViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<bool>>
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherProvider _launcherProvider;

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

    public AppSettingsControlViewModel(ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider, ILauncherProvider launcherProvider)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _launcherProvider = launcherProvider;
        AppSettings = _settingsService.AppSettings;

        _initialLanguageCode = AppSettings.GeneralSettings.LanguageCode;
        _initialGlobalFontFamily = AppSettings.GeneralSettings.GlobalFontFamily;
        _initialEnhanceControlInteractiveAnimations = AppSettings.GeneralSettings.EnhanceControlInteractiveAnimations;
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == AppSettings.GeneralSettings)
        {
            if (message.PropertyName == nameof(AppSettings.GeneralSettings.LanguageCode))
                IsLanguageChanged = message.NewValue != _initialLanguageCode;
            else if (message.PropertyName == nameof(AppSettings.GeneralSettings.GlobalFontFamily))
                IsGlobalFontChanged = message.NewValue != _initialGlobalFontFamily;
        }
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == AppSettings.GeneralSettings)
        {
            if (message.PropertyName == nameof(AppSettings.GeneralSettings.EnhanceControlInteractiveAnimations))
                IsEnhanceControlInteractiveAnimationsChanged = message.NewValue != _initialEnhanceControlInteractiveAnimations;
        }
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