using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

public partial class AppSettingsControlViewModel : BaseViewModel
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherProvider  _launcherProvider;

    public AppSettingsControlViewModel(ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider, ILauncherProvider launcherProvider)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _launcherProvider = launcherProvider;
        AppSettings = _settingsService.AppSettings;
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