using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AppSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public AppSettingsControlViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHelper.RestartApp();
        }

        public async Task<bool> ToggleAutoStartupAsync(bool target)
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
            return await DetectIsAutoStartupEnabledAsync();
        }

        public async Task<bool> DetectIsAutoStartupEnabledAsync()
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
            return result;
        }

    }
}
