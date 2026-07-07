using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Controls;

public partial class LyricsWindowSettingsControl : UserControl
{
    public static readonly StyledProperty<LyricsWindowStatus?> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<LyricsWindowSettingsControl, LyricsWindowStatus?>(nameof(LyricsWindowStatus));

    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public LyricsWindowSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LyricsWindowSettingsControlViewModel>();
    }

    public LyricsWindowSettingsControlViewModel ViewModel => (LyricsWindowSettingsControlViewModel)DataContext!;

    public bool HideConfigPanelWhenLoaded { get; set; } = true;

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void DeleteMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is LyricsWindowStatus data)
        {
            var window = _windowManagerProvider.GetWindow(WindowType.NowPlayingWindow, data);
            if (window != null) _windowManagerProvider.CloseWindow(window);
            ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
        }
    }

    private async void ShareMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is LyricsWindowStatus data)
        {
            IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "JSON", new List<string> { ".json" } }
            };
            var suggestedFileName = $"BetterLyrics_LyricsWindow_{data.Name}.json";
            string? filePath;

            // Avalonia 判断父级的方式可能需要根据实际视图层级调整
            if (Parent is Popup)
                (_, filePath) = await _filePickerProvider.PickSaveFileAsync(fileTypeChoices,
                    suggestedFileName, WindowType.NowPlayingWindow);
            else
                (_, filePath) = await _filePickerProvider.PickSaveFileAsync(fileTypeChoices,
                    suggestedFileName, WindowType.SettingsWindow);

            if (filePath != null)
            {
                var clonedData = (LyricsWindowStatus)data.Clone();
                clonedData.IsDefault = false;
                var json = JsonSerializer.Serialize(clonedData,
                    SourceGenerationContext.Default.LyricsWindowStatus);
                await File.WriteAllTextAsync(filePath, json);
                _globalToastProvider.Show("ExportSettingsSuccess", null, MessageSeverity.Success);
            }
        }
    }

    private void CopyMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is LyricsWindowStatus data)
        {
            var clonedData = (LyricsWindowStatus)data.Clone();
            clonedData.IsDefault = false;
            ViewModel.AppSettings.WindowBoundsRecords.Add(clonedData);
        }
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        string[] fileTypeFilter = [".json"];
        string? filePath;

        if (Parent is Popup)
            (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.NowPlayingWindow);
        else
            (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.SettingsWindow);

        if (filePath != null)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize(json,
                SourceGenerationContext.Default.LyricsWindowStatus);
            if (data != null)
            {
                ViewModel.AppSettings.WindowBoundsRecords.Add(data);
                _globalToastProvider.Show("ImportSettingsSuccess", null, MessageSeverity.Success);
            }
        }
    }

    private void ConfigButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is LyricsWindowStatus status)
        {
            ShowConfigPanel(status);
        }
    }

    public void ShowConfigPanel(LyricsWindowStatus? status)
    {
        if (status == null) return;

        ConfigNavView.SelectedItem = WindowSegmentedItem;
        LyricsWindowStatus = status;
        ConfigPanel.Show();
    }

    private void EmbeddedConfigButton_Click(object? sender, RoutedEventArgs e)
    {
        ConfigNavView.SelectedItem = WindowSegmentedItem;
        LyricsWindowStatus = _settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus;
        ConfigPanel.Show();
    }

    private void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        if (HideConfigPanelWhenLoaded) ConfigPanel.Hide();
    }

    private void ConfigNavView_SelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is FANavigationViewItem item && item.Tag != null)
        {
            ViewModel.SelectorBarSelectedItemTag = item.Tag.ToString()!;
        }
    }

    private void CopyAndTransformMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is LyricsWindowStatus data)
        {
            var to = menuItem.Tag?.ToString();
            if (to == null) return;

            var clonedData = (LyricsWindowStatus)data.Clone();
            clonedData.IsDefault = false;
            clonedData.IsPinToTaskbar = false;
            clonedData.IsWorkArea = false;
            clonedData.IsLocked = false;
            clonedData.IsWallpaper = false;

            clonedData.Name = _localizationService.GetLocalizedString(to);
            switch (to)
            {
                case "StandardMode":
                    break;
                case "DesktopMode":
                    clonedData.IsLocked = true;
                    break;
                case "DockedMode":
                    clonedData.IsWorkArea = true;
                    clonedData.IsLocked = true;
                    break;
                case "FullscreenMode":
                    break;
                case "NarrowMode":
                    break;
                case "TaskbarMode":
                    clonedData.IsPinToTaskbar = true;
                    clonedData.IsLocked = true;
                    break;
                case "WallpaperMode":
                    clonedData.IsWallpaper = true;
                    break;
            }

            ViewModel.AppSettings.WindowBoundsRecords.Add(clonedData);
        }
    }

    private static Rect MapToMonitor(AppRect monitorRectBefore, Rect monitorRectAfter, AppRect windowRectBefore)
    {
        var xRatio = monitorRectAfter.Width / monitorRectBefore.Width;
        var yRatio = monitorRectAfter.Height / monitorRectBefore.Height;
        var newX = monitorRectAfter.X + (windowRectBefore.X - monitorRectBefore.X) * xRatio;
        var newY = monitorRectAfter.Y + (windowRectBefore.Y - monitorRectBefore.Y) * yRatio;
        var newWidth = windowRectBefore.Width * xRatio;
        var newHeight = windowRectBefore.Height * yRatio;
        return new Rect(newX, newY, newWidth, newHeight);
    }

    private void MenuFlyout_Opened(object? sender, EventArgs e)
    {
        if (sender is MenuFlyout menuFlyout)
        {
            // 通过 x:Name 在后台直接查找到承载显示器列表的子菜单
            var moveMonitorMenuItem = menuFlyout.Items.OfType<MenuItem>().FirstOrDefault(x => x.Name == "MoveToMonitorMenuItem");
            if (moveMonitorMenuItem == null) return;

            // XAML 里我们去掉了外层的嵌套层级，数据上下文直接在当前的父级菜单项身上
            var status = (LyricsWindowStatus)moveMonitorMenuItem.DataContext!;
            moveMonitorMenuItem.IsEnabled = status.WindowStatus == WindowStatus.Opened;

            //var window = (NowPlayingWindow?)_windowManagerProvider.GetNowPlayingWindow(status);
            //if (window == null) return;

            //var monitorRectBefore = status.MonitorBounds;
            //var windowRectBefore = status.WindowBounds;

            //moveMonitorMenuItem.Items.Clear();
            //var names = MonitorHook.GetAllMonitorDeviceNames();
            //foreach (var name in names)
            //{
            //    var menuItem = new MenuItem { Header = name };
            //    menuItem.Click += async (s, args) =>
            //    {
            //        var monitorInfoEx = MonitorHook.GetMonitorInfoExFromDeviceName(name);
            //        var monitorRectAfter = monitorInfoEx.rcMonitor.ToRect(); // 确保你已实现 Avalonia Rect 的转换拓展
            //        var windowRectAfter = MapToMonitor(monitorRectBefore, monitorRectAfter, windowRectBefore);

            //        status.MonitorDeviceName = name;
            //        status.MonitorBounds = windowRectAfter.ToAppRect();

            //        if (status.IsWallpaper)
            //        {
            //            window.LyricsWindowStatus.IsLocked = false;
            //            await Task.Delay(500);

            //            _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
            //            await Task.Delay(500);

            //            window.LyricsWindowStatus.IsLocked = true;
            //        }
            //        else if (status.IsPinToTaskbar)
            //        {
            //            window.LyricsWindowStatus.IsLocked = false;
            //            await Task.Delay(500);

            //            _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
            //            await Task.Delay(500);

            //            window.LyricsWindowStatus.IsLocked = true;
            //        }
            //        else if (status.IsWorkArea)
            //        {
            //            _windowManagerProvider.MoveAndResize(window, status.GetAppBarBounds());
            //        }
            //        else if (status.IsFullscreen)
            //        {
            //            // 替换 WinUI 的 WindowPresenter 逻辑
            //            window.WindowState = WindowState.Normal;
            //            _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
            //            window.WindowState = WindowState.FullScreen;
            //        }
            //        else if (status.IsMaximized)
            //        {
            //            window.WindowState = WindowState.Normal;
            //            _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
            //            window.WindowState = WindowState.Maximized;
            //        }
            //        else
            //        {
            //            _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
            //        }
            //    };
            //    moveMonitorMenuItem.Items.Add(menuItem);
            //}
        }
    }

    private void CloseConfigPanelButton_Click(object? sender, RoutedEventArgs e)
    {
        ConfigPanel.Hide();
    }
}