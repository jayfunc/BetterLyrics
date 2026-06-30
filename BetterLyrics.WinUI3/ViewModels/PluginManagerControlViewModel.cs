using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Plugins;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using BetterLyrics.Core.ViewModels;

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
                    await Task.Delay(Time.DebounceTimeout * 2);
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
                    Helper.GlobalToastManager.Show("Error", ex.Message, MessageSeverity.Error);
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
                await Task.Delay(Time.DebounceTimeout * 2);
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
                Helper.GlobalToastManager.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }

    }
}
