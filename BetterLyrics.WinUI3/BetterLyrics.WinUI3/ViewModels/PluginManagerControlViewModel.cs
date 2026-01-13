using BetterLyrics.Core.Interfaces;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PluginManagerControlViewModel : BaseViewModel
    {
        private readonly IPluginService _pluginService;
        private readonly ISettingsService _settingsService;

        public AppSettings AppSettings { get; }

        public Visibility IsListEmpty => AppSettings.PluginsInfo.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public PluginManagerControlViewModel(IPluginService pluginService, ISettingsService settingsService)
        {
            _pluginService = pluginService;
            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private async Task InstallPluginAsync()
        {
            var file = await Helper.PickerHelper.PickSingleFileAsync<SettingsWindow>([".zip"]);

            if (file != null)
            {
                try
                {
                    _pluginService.InstallPlugin(file.Path);
                    // 确保程序已保存设置
                    await Task.Delay(Constants.Time.DebounceTimeout * 2);
                    WindowHook.RestartApp();
                }
                catch (Exception ex)
                {
                    Helper.ToastHelper.ShowToast("Error", ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                }
            }
        }

        [RelayCommand]
        private async Task UninstallPluginAsync(IPlugin plugin)
        {
            try
            {
                _pluginService.UninstallPlugin(plugin.Id);
                // 确保程序已保存设置
                await Task.Delay(Constants.Time.DebounceTimeout * 2);
                WindowHook.RestartApp();
            }
            catch (Exception ex)
            {
                Helper.ToastHelper.ShowToast("Error", ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

    }
}
