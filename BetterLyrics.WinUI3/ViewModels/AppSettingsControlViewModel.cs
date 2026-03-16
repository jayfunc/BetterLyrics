using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AppSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public AppSettingsControlViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private static async Task OpenTaskMgrStartupAppsAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps"));
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHook.RestartApp();
        }

    }
}
