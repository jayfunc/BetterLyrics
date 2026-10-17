using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Singleton ViewModel
/// </summary>
/// <param name="navigationService"></param>
/// <param name="appLifecycleService"></param>
/// <param name="windowManagerProvider"></param>
/// <param name="settingsService"></param>
/// <param name="systemUiProvider"></param>
public partial class SystemTrayViewModel : ObservableRecipient,
    IRecipient<ColorfulSystemTrayIconChangedMessage>
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISystemUIProvider _systemUiProvider;

    public INavigationService NavigationService { get; }
    public IAppLifecycleService AppLifecycleService { get; }

    [ObservableProperty] public partial string TrayIconPath { get; set; } = "ms-appx:///Assets/Logo.ico";

    public SystemTrayViewModel(
        INavigationService navigationService,
        IAppLifecycleService appLifecycleService,
        IWindowManagerProvider windowManagerProvider,
        ISettingsService settingsService,
        ISystemUIProvider systemUiProvider)
    {
        _windowManagerProvider = windowManagerProvider;
        _settingsService = settingsService;
        _systemUiProvider = systemUiProvider;
        NavigationService = navigationService;
        AppLifecycleService = appLifecycleService;

        IsActive = true;
    }

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

    public void UpdateSystemTrayIcon()
    {
        if (_settingsService.AppSettings.SystemTraySettings.ColorfulSystemTrayIcon)
        {
            TrayIconPath = "ms-appx:///Assets/Logo.ico";
        }
        else
        {
            var isDark = _systemUiProvider.GetSystemTheme() == AppTheme.Dark;
            TrayIconPath = $"ms-appx:///Assets/Logo{(isDark ? "White" : "Black")}.ico";
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

    public void Receive(ColorfulSystemTrayIconChangedMessage message)
    {
        UpdateSystemTrayIcon();
    }
}