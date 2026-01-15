using BetterLyrics.Core.Interfaces;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PluginManagerControlViewModel : BaseViewModel
    {
        private readonly IPluginService _pluginService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        public AppSettings AppSettings { get; }

        public Visibility IsListEmpty => AppSettings.PluginsInfo.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public PluginManagerControlViewModel(IPluginService pluginService, ISettingsService settingsService, ILocalizationService localizationService)
        {
            _pluginService = pluginService;
            _settingsService = settingsService;
            _localizationService = localizationService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private async Task InstallPluginAsync()
        {
            var file = await Helper.PickerHelper.PickSingleFileAsync<SettingsWindow>([".blp"]);
            await InstallPluginByFileAsync(file);
        }

        public async Task InstallPluginByFileAsync(StorageFile? file)
        {
            if (file != null)
            {
                try
                {
                    var settingsWindow = WindowHook.GetWindow<SettingsWindow>();
                    _pluginService.InstallPlugin(file.Path);
                    // 确保程序已保存设置
                    await Task.Delay(Constants.Time.DebounceTimeout * 2);
                    await new ContentDialog
                    {
                        Title = _localizationService.GetLocalizedString("PluginManagerControlInstallSuccessful"),
                        Content = _localizationService.GetLocalizedString("PluginManagerControlRestartRequired"),
                        PrimaryButtonText = _localizationService.GetLocalizedString("PluginManagerControlRestartNow"),
                        XamlRoot = settingsWindow?.Content.XamlRoot
                    }.ShowAsync();
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
                var settingsWindow = WindowHook.GetWindow<SettingsWindow>();
                _pluginService.UninstallPlugin(plugin.Id);
                // 确保程序已保存设置
                await Task.Delay(Constants.Time.DebounceTimeout * 2);
                await new ContentDialog
                {
                    Title = _localizationService.GetLocalizedString("PluginManagerControlUninstallSuccessful"),
                    Content = _localizationService.GetLocalizedString("PluginManagerControlRestartRequired"),
                    PrimaryButtonText = _localizationService.GetLocalizedString("PluginManagerControlRestartNow"),
                    XamlRoot = settingsWindow?.Content.XamlRoot
                }.ShowAsync();
                WindowHook.RestartApp();
            }
            catch (Exception ex)
            {
                Helper.ToastHelper.ShowToast("Error", ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

    }
}
