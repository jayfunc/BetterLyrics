using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.Implementations.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    [RelayCommand]
    private void OpenSettingsWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.SettingsWindow);
    }

    [RelayCommand]
    private void OpenMusicGalleryWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
    }

    [RelayCommand]
    private void OpenLyricsWindowSwitchWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);
    }

    [RelayCommand]
    private void OpenLyricsSearchWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsSearchWindow);
    }

    [RelayCommand]
    private void OpenLyricsShareWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsShareWindow);
    }

    [RelayCommand]
    private void OpenStatsDashboardWindow()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.StatsDashboardWindow);
    }
}