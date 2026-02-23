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

        [ObservableProperty] public partial bool IsAutoStartupEnabled { get; set; } = false;

        public AppSettingsControlViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;
            _ = DetectIsAutoStartupEnabledAsync();
        }

        public async Task ToggleAutoStartupAsync(bool target)
        {
            StartupTask startupTask = await StartupTask.GetAsync(Constants.App.AutoStartupTaskId);
            if (target)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }
            await DetectIsAutoStartupEnabledAsync();
        }

        private async Task DetectIsAutoStartupEnabledAsync()
        {
            bool result = false;
            var startupTask = await StartupTask.GetAsync(Constants.App.AutoStartupTaskId);
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                case StartupTaskState.DisabledByUser:
                case StartupTaskState.DisabledByPolicy:
                    result = false;
                    break;
                case StartupTaskState.Enabled:
                    result = true;
                    break;
            }
            IsAutoStartupEnabled = result;
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHook.RestartApp();
        }

    }
}
