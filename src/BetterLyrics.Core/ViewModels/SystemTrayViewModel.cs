using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

public partial class SystemTrayViewModel(
    INavigationService navigationService,
    IAppLifecycleService appLifecycleService,
    IWindowManagerProvider windowManagerProvider,
    ISettingsService settingsService) : BaseViewModel
{
    private readonly IWindowManagerProvider _windowManagerProvider = windowManagerProvider;
    private readonly ISettingsService _settingsService = settingsService;

    public INavigationService NavigationService { get; } = navigationService;
    public IAppLifecycleService AppLifecycleService { get; } = appLifecycleService;

    private void TrayIconClickedCallback(SystemTrayClickCallback callback)
    {
        switch (callback)
        {
            case SystemTrayClickCallback.None:
                break;
            case SystemTrayClickCallback.LyricsWindowSwitchWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);
                break;
            case SystemTrayClickCallback.LyricsSearchWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsSearchWindow);
                break;
            case SystemTrayClickCallback.MusicGalleryWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
                break;
            case SystemTrayClickCallback.StatsWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.StatsDashboardWindow);
                break;
            case SystemTrayClickCallback.LyricsCardWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsShareWindow);
                break;
            case SystemTrayClickCallback.SettingsWindow:
                _windowManagerProvider.OpenOrShowWindow(WindowType.SettingsWindow);
                break;
        }
    }

    [RelayCommand]
    private void TrayIconClicked()
    {
        TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayClickCallback);
    }

    [RelayCommand]
    private void TrayIconDoubleClicked()
    {
        TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayDoubleClickCallback);
    }

    [RelayCommand]
    private void TrayIconMiddleClicked()
    {
        TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayMiddleClickCallback);
    }
}