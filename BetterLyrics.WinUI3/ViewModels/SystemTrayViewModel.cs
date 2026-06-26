using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel(INavigationService navigationService, IAppLifecycleService appLifecycleService, ISettingsService settingsService) : BaseViewModel
    {
        public INavigationService NavigationService { get; } = navigationService;
        public IAppLifecycleService AppLifecycleService { get; } = appLifecycleService;

        private readonly ISettingsService _settingsService = settingsService;

        private static void TrayIconClickedCallback(SystemTrayClickCallback callback)
        {
            switch (callback)
            {
                case SystemTrayClickCallback.None:
                    break;
                case SystemTrayClickCallback.LyricsWindowSwitchWindow:
                    WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
                    break;
                case SystemTrayClickCallback.LyricsSearchWindow:
                    WindowHook.OpenOrShowWindow<LyricsSearchWindow>();
                    break;
                case SystemTrayClickCallback.MusicGalleryWindow:
                    WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
                    break;
                case SystemTrayClickCallback.StatsWindow:
                    WindowHook.OpenOrShowWindow<StatsDashboardWindow>();
                    break;
                case SystemTrayClickCallback.LyricsCardWindow:
                    WindowHook.OpenOrShowWindow<LyricsShareWindow>();
                    break;
                case SystemTrayClickCallback.SettingsWindow:
                    WindowHook.OpenOrShowWindow<SettingsWindow>();
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void TrayIconClicked() => TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayClickCallback);

        [RelayCommand]
        private void TrayIconDoubleClicked() => TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayDoubleClickCallback);

        [RelayCommand]
        private void TrayIconMiddleClicked() => TrayIconClickedCallback(_settingsService.AppSettings.SystemTraySettings.SystemTrayMiddleClickCallback);
    }
}
