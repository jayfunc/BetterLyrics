using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinUIEx;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel(ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty]
        public partial string ToolTipText { get; set; } = Constants.App.AppName;

        [RelayCommand]
        private static void ExitApp()
        {
            WindowHook.ExitApp();
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHook.RestartApp();
        }

        [RelayCommand]
        private static void ResetWindowPosition()
        {
            var lyricsWindow = WindowHook.GetWindow<NowPlayingWindow>();
            lyricsWindow?.MoveAndResize(100, 100, 800, 500);
        }

        [RelayCommand]
        private static void OpenSettings()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private static void OpenMusicGallery()
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        [RelayCommand]
        private static void OpenLyrics()
        {
            WindowHook.OpenOrShowWindow<NowPlayingWindow>();
        }

        [RelayCommand]
        private static void OpenLyricsWindowSwitch()
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        [RelayCommand]
        private static void OpenLyricsSearchWindow()
        {
            WindowHook.OpenOrShowWindow<LyricsSearchWindow>();
        }
    }
}
