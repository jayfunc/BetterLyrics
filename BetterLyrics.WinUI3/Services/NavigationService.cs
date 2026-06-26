using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.Services
{
    public partial class NavigationService : ObservableObject, INavigationService
    {
        [RelayCommand]
        private void OpenSettingsWindow() => WindowHook.OpenOrShowWindow<SettingsWindow>();

        [RelayCommand]
        private void OpenMusicGalleryWindow() => WindowHook.OpenOrShowWindow<MusicGalleryWindow>();

        [RelayCommand]
        private void OpenLyricsWindowSwitchWindow() => WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();

        [RelayCommand]
        private void OpenLyricsSearchWindow() => WindowHook.OpenOrShowWindow<LyricsSearchWindow>();

        [RelayCommand]
        private void OpenLyricsShareWindow() => WindowHook.OpenOrShowWindow<LyricsShareWindow>();

        [RelayCommand]
        private void OpenStatsDashboardWindow() => WindowHook.OpenOrShowWindow<StatsDashboardWindow>();
    }
}
