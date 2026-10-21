using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Models.WallpaperEngine;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class WindowSettingsControl : UserControl
{
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();
    private readonly IWallpaperEngineService _wallpaperEngineService = Ioc.Default.GetRequiredService<IWallpaperEngineService>();
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private IReadOnlyList<WallpaperEngineWallpaper> _installedWallpapers = [];
    private bool _isUpdatingWallpaperSelection;
    private bool _isLoadingWallpaperOptions;
    private bool _isWallpaperServiceSubscribed;

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(WindowSettingsControl), new PropertyMetadata(default, OnLyricsWindowStatusChanged));

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public static readonly DependencyProperty IsManagedLyricsWindowProperty =
        DependencyProperty.Register(nameof(IsManagedLyricsWindow), typeof(bool),
            typeof(WindowSettingsControl), new PropertyMetadata(false));

    public bool IsManagedLyricsWindow
    {
        get => (bool)GetValue(IsManagedLyricsWindowProperty);
        private set => SetValue(IsManagedLyricsWindowProperty, value);
    }

    public ObservableCollection<string> MonitorDeviceNames { get; set; } = [];

    public static readonly DependencyProperty WallpaperEngineStatusMessageProperty =
        DependencyProperty.Register(nameof(WallpaperEngineStatusMessage), typeof(string),
            typeof(WindowSettingsControl), new PropertyMetadata(string.Empty));

    public string WallpaperEngineStatusMessage
    {
        get => (string)GetValue(WallpaperEngineStatusMessageProperty);
        private set => SetValue(WallpaperEngineStatusMessageProperty, value);
    }

    public static readonly DependencyProperty WallpaperEngineStatusSeverityProperty =
        DependencyProperty.Register(nameof(WallpaperEngineStatusSeverity), typeof(InfoBarSeverity),
            typeof(WindowSettingsControl), new PropertyMetadata(InfoBarSeverity.Informational));

    public InfoBarSeverity WallpaperEngineStatusSeverity
    {
        get => (InfoBarSeverity)GetValue(WallpaperEngineStatusSeverityProperty);
        private set => SetValue(WallpaperEngineStatusSeverityProperty, value);
    }

    public ObservableCollection<WallpaperEngineBindingOption> WallpaperOptions { get; } = [];

    public WindowSettingsControl()
    {
        InitializeComponent();
        MonitorDeviceNames = [.. _monitorProvider.GetAllMonitorDeviceNames()];
        WallpaperComboBox.ItemsSource = WallpaperOptions;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;

        if (!_isWallpaperServiceSubscribed)
        {
            _wallpaperEngineService.ActiveWallpaperChanged += WallpaperEngineService_ActiveWallpaperChanged;
            _isWallpaperServiceSubscribed = true;
        }

        if (IsWindowManaged(LyricsWindowStatus))
        {
            await LoadWallpaperOptionsAsync();
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;

        if (_isWallpaperServiceSubscribed)
        {
            _wallpaperEngineService.ActiveWallpaperChanged -= WallpaperEngineService_ActiveWallpaperChanged;
            _isWallpaperServiceSubscribed = false;
        }
    }

    private static void OnLyricsWindowStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WindowSettingsControl control) return;

        control.IsManagedLyricsWindow = control.IsWindowManaged(control.LyricsWindowStatus);

        if (!control.IsManagedLyricsWindow) return;

        if (control.WallpaperOptions.Count == 0)
        {
            _ = control.LoadWallpaperOptionsAsync();
        }
        else
        {
            control.RefreshSelectedWallpaper();
        }
    }

    /// <summary>
    /// Whether the given status belongs to a lyrics window managed by the lyrics window manager.
    /// </summary>
    public bool IsWindowManaged(LyricsWindowStatus? status) =>
        status != null && _settingsService.AppSettings.WindowBoundsRecords.Contains(status);

    private void MonitorProvider_MonitorsChanged(object? sender, EventArgs e)
    {
        // Wait a bit to ensure Windows display topology is fully updated
        Task.Run(async () =>
        {
            await Task.Delay(500);
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshMonitorDeviceNames();
            });
        });
    }

    private void RefreshMonitorDeviceNames()
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

    private void RefreshMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshMonitorDeviceNames();
    }

    private async void RefreshWallpaperButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadWallpaperOptionsAsync(forceRefresh: true);
    }

    private void WallpaperComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingWallpaperSelection || LyricsWindowStatus == null) return;
        if (WallpaperComboBox.SelectedItem is not WallpaperEngineBindingOption option) return;

        if (PathEquals(option.ProjectPath, LyricsWindowStatus.WallpaperEngineBindingPath)) return;

        LyricsWindowStatus.WallpaperEngineBindingPath = option.ProjectPath ?? string.Empty;
        LyricsWindowStatus.WallpaperEngineBindingName = option.ProjectPath == null ? string.Empty : option.DisplayName;
    }

    private void WallpaperEngineService_ActiveWallpaperChanged(object? sender,
        WallpaperEngineActiveWallpaperChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateWallpaperEngineStatus);
    }

    private async Task LoadWallpaperOptionsAsync(bool forceRefresh = false)
    {
        if (!IsWindowManaged(LyricsWindowStatus) || _isLoadingWallpaperOptions) return;

        _isLoadingWallpaperOptions = true;
        try
        {
            _installedWallpapers = await _wallpaperEngineService.GetInstalledWallpapersAsync(forceRefresh);

            var options = new List<WallpaperEngineBindingOption>
            {
                new()
                {
                    ProjectPath = null,
                    DisplayName = _localizationService.GetLocalizedString("WallpaperEngineNotLinked")
                }
            };

            options.AddRange(_installedWallpapers.Select(wallpaper => new WallpaperEngineBindingOption
            {
                ProjectPath = wallpaper.ProjectPath,
                DisplayName = wallpaper.DisplayName
            }));

            var currentPath = LyricsWindowStatus.WallpaperEngineBindingPath;
            if (!string.IsNullOrWhiteSpace(currentPath) &&
                !options.Any(option => PathEquals(option.ProjectPath, currentPath)))
            {
                var name = string.IsNullOrWhiteSpace(LyricsWindowStatus.WallpaperEngineBindingName)
                    ? GetFallbackWallpaperName(currentPath)
                    : LyricsWindowStatus.WallpaperEngineBindingName;

                options.Add(new WallpaperEngineBindingOption
                {
                    ProjectPath = currentPath,
                    DisplayName =
                        $"{name} ({_localizationService.GetLocalizedString("WallpaperEngineMissing")})"
                });
            }

            _isUpdatingWallpaperSelection = true;
            try
            {
                WallpaperOptions.Clear();
                foreach (var option in options)
                {
                    WallpaperOptions.Add(option);
                }

                WallpaperComboBox.SelectedItem =
                    WallpaperOptions.FirstOrDefault(option => PathEquals(option.ProjectPath, currentPath)) ??
                    WallpaperOptions[0];
            }
            finally
            {
                _isUpdatingWallpaperSelection = false;
            }

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

        _isUpdatingWallpaperSelection = true;
        try
        {
            var currentPath = LyricsWindowStatus.WallpaperEngineBindingPath;
            WallpaperComboBox.SelectedItem =
                WallpaperOptions.FirstOrDefault(option => PathEquals(option.ProjectPath, currentPath)) ??
                WallpaperOptions[0];
        }
        finally
        {
            _isUpdatingWallpaperSelection = false;
        }
    }

    private void UpdateWallpaperEngineStatus()
    {
        if (!_wallpaperEngineService.IsAvailable)
        {
            WallpaperEngineStatusMessage =
                _localizationService.GetLocalizedString("WallpaperEngineNotInstalled");
            WallpaperEngineStatusSeverity = InfoBarSeverity.Warning;
            return;
        }

        var activeProjectPaths = _wallpaperEngineService.GetActiveWallpaperProjectPaths();
        if (activeProjectPaths == null || activeProjectPaths.Count == 0)
        {
            WallpaperEngineStatusMessage =
                _localizationService.GetLocalizedString("WallpaperEngineNoActiveWallpaper");
            WallpaperEngineStatusSeverity = InfoBarSeverity.Warning;
            return;
        }

        var names = activeProjectPaths
            .Select(GetWallpaperName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase);

        WallpaperEngineStatusMessage =
            $"{_localizationService.GetLocalizedString("WallpaperEngineCurrentWallpaper")} {string.Join(", ", names)}";
        WallpaperEngineStatusSeverity = InfoBarSeverity.Informational;
    }

    private string GetWallpaperName(string projectPath)
    {
        var installed = _installedWallpapers.FirstOrDefault(wallpaper =>
            PathEquals(wallpaper.ProjectPath, projectPath));

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

        return string.Equals(left.Replace('/', '\\'), right.Replace('/', '\\'),
            StringComparison.OrdinalIgnoreCase);
    }
}
