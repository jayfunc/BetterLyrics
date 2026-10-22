using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Models.WallpaperEngine;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel，生命周期跟随 WindowSettingsControl 实例（绑定单个 LyricsWindowStatus）。
/// </summary>
public partial class WindowSettingsControlViewModel : ObservableObject, ITransientViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IWallpaperEngineService _wallpaperEngineService;
    private readonly IMonitorProvider _monitorProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IAppUIThreadProvider _appUiThreadProvider;

    private IReadOnlyList<WallpaperEngineWallpaper> _installedWallpapers = [];
    private bool _isLoadingWallpaperOptions;
    private bool _isAttached;

    public WindowSettingsControlViewModel(ISettingsService settingsService,
        IWallpaperEngineService wallpaperEngineService, IMonitorProvider monitorProvider,
        ILocalizationService localizationService, IAppUIThreadProvider appUiThreadProvider)
    {
        _settingsService = settingsService;
        _wallpaperEngineService = wallpaperEngineService;
        _monitorProvider = monitorProvider;
        _localizationService = localizationService;
        _appUiThreadProvider = appUiThreadProvider;

        foreach (var name in _monitorProvider.GetAllMonitorDeviceNames())
        {
            MonitorDeviceNames.Add(name);
        }
    }

    [ObservableProperty] public partial LyricsWindowStatus? LyricsWindowStatus { get; set; }

    [ObservableProperty] public partial bool IsManaged { get; set; }

    public ObservableCollection<string> MonitorDeviceNames { get; } = [];

    public ObservableCollection<WallpaperEngineBindingOption> WallpaperOptions { get; } = [];

    [ObservableProperty] public partial WallpaperEngineBindingOption? SelectedWallpaperOption { get; set; }

    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial MessageSeverity StatusSeverity { get; set; } = MessageSeverity.Informational;

    [ObservableProperty] public partial string CurrentWallpaperNames { get; set; } = string.Empty;

    partial void OnSelectedWallpaperOptionChanged(WallpaperEngineBindingOption? value)
    {
        if (value == null || LyricsWindowStatus == null) return;

        if (PathEquals(value.ProjectPath, LyricsWindowStatus.WallpaperEngineBindingPath)) return;

        LyricsWindowStatus.WallpaperEngineBindingPath = value.ProjectPath ?? string.Empty;
        LyricsWindowStatus.WallpaperEngineBindingName =
            value.ProjectPath == null ? string.Empty : value.CopyName;
    }

    public void Attach()
    {
        if (_isAttached) return;

        _wallpaperEngineService.ActiveWallpaperChanged += WallpaperEngineService_ActiveWallpaperChanged;
        _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;
        _isAttached = true;
    }

    public void Detach()
    {
        if (!_isAttached) return;

        _wallpaperEngineService.ActiveWallpaperChanged -= WallpaperEngineService_ActiveWallpaperChanged;
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;
        _isAttached = false;
    }

    /// <summary>
    /// 绑定窗口配置，并在管理中的窗口上加载或刷新壁纸选项。
    /// </summary>
    public void SetLyricsWindowStatus(LyricsWindowStatus? status)
    {
        LyricsWindowStatus = status;
        IsManaged = status != null && _settingsService.AppSettings.WindowBoundsRecords.Contains(status);

        if (!IsManaged) return;

        if (WallpaperOptions.Count == 0) _ = LoadWallpaperOptionsAsync();
        else RefreshSelectedWallpaper();
    }

    public async Task InitializeAsync()
    {
        if (!IsManaged) return;

        if (WallpaperOptions.Count == 0) await LoadWallpaperOptionsAsync();
        else UpdateWallpaperEngineStatus();
    }

    public async Task LoadWallpaperOptionsAsync(bool forceRefresh = false)
    {
        if (!IsManaged || LyricsWindowStatus == null || _isLoadingWallpaperOptions) return;

        _isLoadingWallpaperOptions = true;
        try
        {
            _installedWallpapers = await _wallpaperEngineService.GetInstalledWallpapersAsync(forceRefresh);

            var status = LyricsWindowStatus;
            if (status == null) return;

            var notLinkedName = _localizationService.GetLocalizedString("WallpaperEngineNotLinked");
            var options = new List<WallpaperEngineBindingOption>
            {
                new()
                {
                    ProjectPath = null,
                    DisplayName = notLinkedName,
                    CopyName = notLinkedName
                }
            };

            options.AddRange(_installedWallpapers.Select(wallpaper => new WallpaperEngineBindingOption
            {
                ProjectPath = wallpaper.ProjectPath,
                DisplayName = wallpaper.DisplayName,
                CopyName = wallpaper.Title,
                PreviewImagePath = wallpaper.PreviewImagePath
            }));

            var currentPath = status.WallpaperEngineBindingPath;
            if (!string.IsNullOrWhiteSpace(currentPath) &&
                !options.Any(option => PathEquals(option.ProjectPath, currentPath)))
            {
                var name = string.IsNullOrWhiteSpace(status.WallpaperEngineBindingName)
                    ? GetFallbackWallpaperName(currentPath)
                    : status.WallpaperEngineBindingName;

                options.Add(new WallpaperEngineBindingOption
                {
                    ProjectPath = currentPath,
                    DisplayName =
                        $"{name} ({_localizationService.GetLocalizedString("WallpaperEngineMissing")})",
                    CopyName = name
                });
            }

            WallpaperOptions.Clear();
            foreach (var option in options)
            {
                WallpaperOptions.Add(option);
            }

            SelectedWallpaperOption = GetSelectedWallpaperOption(currentPath);

            UpdateWallpaperEngineStatus();
        }
        finally
        {
            _isLoadingWallpaperOptions = false;
        }
    }

    private void RefreshSelectedWallpaper()
    {
        if (LyricsWindowStatus == null || WallpaperOptions.Count == 0) return;

        SelectedWallpaperOption = GetSelectedWallpaperOption(LyricsWindowStatus.WallpaperEngineBindingPath);
    }

    private WallpaperEngineBindingOption GetSelectedWallpaperOption(string currentPath) =>
        WallpaperOptions.FirstOrDefault(option => PathEquals(option.ProjectPath, currentPath)) ??
        WallpaperOptions[0];

    private void WallpaperEngineService_ActiveWallpaperChanged(object? sender,
        WallpaperEngineActiveWallpaperChangedEventArgs e)
    {
        _appUiThreadProvider.Execute(UpdateWallpaperEngineStatus);
    }

    private void MonitorProvider_MonitorsChanged(object? sender, EventArgs e)
    {
        // 等待系统显示拓扑更新完成
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            _appUiThreadProvider.Execute(RefreshMonitorDeviceNames);
        });
    }

    public void RefreshMonitorDeviceNames()
    {
        var currentNames = _monitorProvider.GetAllMonitorDeviceNames().ToList();

        MonitorDeviceNames.Clear();
        foreach (var name in currentNames)
        {
            MonitorDeviceNames.Add(name);
        }

        if (LyricsWindowStatus != null && !currentNames.Contains(LyricsWindowStatus.MonitorDeviceName))
        {
            LyricsWindowStatus.MonitorDeviceName = currentNames.FirstOrDefault() ?? "";
        }
    }

    private void UpdateWallpaperEngineStatus()
    {
        if (!_wallpaperEngineService.IsAvailable)
        {
            CurrentWallpaperNames = string.Empty;
            StatusMessage = _localizationService.GetLocalizedString("WallpaperEngineNotInstalled");
            StatusSeverity = MessageSeverity.Warning;
            return;
        }

        var activeProjectPaths = _wallpaperEngineService.GetActiveWallpaperProjectPaths();
        if (activeProjectPaths == null || activeProjectPaths.Count == 0)
        {
            CurrentWallpaperNames = string.Empty;
            StatusMessage = _localizationService.GetLocalizedString("WallpaperEngineNoActiveWallpaper");
            StatusSeverity = MessageSeverity.Warning;
            return;
        }

        var names = activeProjectPaths
            .Select(GetWallpaperName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase);

        CurrentWallpaperNames = string.Join(", ", names);
        StatusMessage =
            $"{_localizationService.GetLocalizedString("WallpaperEngineCurrentWallpaper")} {CurrentWallpaperNames}";
        StatusSeverity = MessageSeverity.Informational;
    }

    private string GetWallpaperName(string projectPath)
    {
        var installed = _installedWallpapers.FirstOrDefault(wallpaper => PathEquals(wallpaper.ProjectPath, projectPath));

        return installed?.Title ?? GetFallbackWallpaperName(projectPath);
    }

    private static string GetFallbackWallpaperName(string projectPath)
    {
        try
        {
            return Path.GetFileName(Path.GetDirectoryName(projectPath)) ?? projectPath;
        }
        catch
        {
            return projectPath;
        }
    }

    private static bool PathEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right);
        }

        return string.Equals(left.Replace('/', '\\'), right.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase);
    }
}
