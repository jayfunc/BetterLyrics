using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Sdk.Interfaces.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel，生命周期跟随 PluginManagerControl 实例。
/// </summary>
public partial class PluginManagerControlViewModel : ObservableObject, ITransientViewModel
{
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly IPluginService _pluginService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IFilePickerProvider _filePickerProvider;

    public PluginManagerControlViewModel(IPluginService pluginService, ISettingsService settingsService,
        IGlobalToastProvider globalToastProvider,
        IWindowManagerProvider windowManagerProvider, IFilePickerProvider filePickerProvider)
    {
        _pluginService = pluginService;
        _settingsService = settingsService;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;
        _filePickerProvider = filePickerProvider;
        AppSettings = _settingsService.AppSettings;
    }

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsListEmpty))] public partial AppSettings AppSettings { get; set; }

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