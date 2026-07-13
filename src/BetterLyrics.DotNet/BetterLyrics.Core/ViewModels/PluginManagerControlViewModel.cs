using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Sdk.Interfaces.Plugins;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

public partial class PluginManagerControlViewModel : BaseViewModel
{
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IPluginService _pluginService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IFilePickerProvider _filePickerProvider;

    public PluginManagerControlViewModel(IPluginService pluginService, ISettingsService settingsService,
        ILocalizationService localizationService, IGlobalToastProvider globalToastProvider,
        IWindowManagerProvider windowManagerProvider, IFilePickerProvider filePickerProvider)
    {
        _pluginService = pluginService;
        _settingsService = settingsService;
        _localizationService = localizationService;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;
        _filePickerProvider = filePickerProvider;
        AppSettings = _settingsService.AppSettings;
    }

    public AppSettings AppSettings { get; }

    public bool IsListEmpty => AppSettings.PluginsInfo.Count == 0;

    [RelayCommand]
    private async Task InstallPluginAsync()
    {
        var (_, filePath) = await _filePickerProvider.PickSingleFileAsync([".blp"], WindowType.SettingsWindow);
        await InstallPluginAsync(filePath);
    }

    public async Task InstallPluginAsync(string? filePath)
    {
        if (filePath != null)
            try
            {
                _pluginService.InstallPlugin(filePath);

                // 确保程序已保存设置
                await Task.Delay(Time.DebounceTimeout * 2);

                _windowManagerProvider.RestartApp();
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
    }

    [RelayCommand]
    private async Task UninstallPluginAsync(IPlugin plugin)
    {
        try
        {
            _pluginService.UninstallPlugin(plugin.Id);

            // 确保程序已保存设置
            await Task.Delay(Time.DebounceTimeout * 2);

            _windowManagerProvider.RestartApp();
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
        }
    }
}