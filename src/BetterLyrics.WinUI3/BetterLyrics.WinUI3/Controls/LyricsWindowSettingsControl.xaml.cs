using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsWindowSettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(LyricsWindowSettingsControl), new PropertyMetadata(null));

    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    private readonly ISettingsService _settingsService =
        Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private readonly IMonitorProvider _monitorProvider =
        Ioc.Default.GetRequiredService<IMonitorProvider>();

    public LyricsWindowSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LyricsWindowSettingsControlViewModel>();
    }

    public LyricsWindowSettingsControlViewModel ViewModel => (LyricsWindowSettingsControlViewModel)DataContext;

    public bool HideConfigPanelWhenLoaded { get; set; } = true;

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuFlyoutItem)
            if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
            {
                var windows = _windowManagerProvider.GetWindows<NowPlayingWindow>();
                var window = windows.FirstOrDefault(x => x.LyricsWindowStatus == data);
                if (window != null) _windowManagerProvider.CloseWindow(window);
                ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
            }
    }

    private async void ShareMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuFlyoutItem)
            if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
            {
                IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
                {
                    { "JSON", new List<string> { ".json" } }
                };
                var suggestedFileName = $"BetterLyrics_LyricsWindow_{data.Name}.json";
                string? filePath;
                if (Parent is FlyoutPresenter)
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

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuFlyoutItem)
            if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
            {
                var clonedData = (LyricsWindowStatus)data.Clone();
                clonedData.IsDefault = false;
                ViewModel.AppSettings.WindowBoundsRecords.Add(clonedData);
            }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        string[] fileTypeFilter = [".json"];
        string? filePath;
        if (Parent is FlyoutPresenter)
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

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ShowConfigPanel((LyricsWindowStatus)((Button)sender).DataContext);
    }

    public void ShowConfigPanel(LyricsWindowStatus? status)
    {
        if (status == null) return;

        ConfigNavView.SelectedItem = WindowSegmentedItem;
        LyricsWindowStatus = status;
        ConfigPanel.Show();
    }

    private void EmbeddedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigNavView.SelectedItem = WindowSegmentedItem;
        LyricsWindowStatus = _settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus;
        ConfigPanel.Show();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (HideConfigPanelWhenLoaded) ConfigPanel.Hide();
    }

    private void ConfigNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ViewModel.SelectorBarSelectedItemTag = (string)((NavigationViewItem)sender.SelectedItem).Tag;
    }

    private void CopyAndTransformMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuFlyoutItem)
            if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
            {
                var to = menuFlyoutItem.Tag.ToString();
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

    private void WindowStatusListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        ViewModel.AppSettings.WindowBoundsRecords?.Refresh();
    }

    private static Rect MapToMonitor(AppRect monitorRectBefore, AppRect monitorRectAfter, AppRect windowRectBefore)
    {
        var xRatio = monitorRectAfter.Width / monitorRectBefore.Width;
        var yRatio = monitorRectAfter.Height / monitorRectBefore.Height;
        var newX = monitorRectAfter.X + (windowRectBefore.X - monitorRectBefore.X) * xRatio;
        var newY = monitorRectAfter.Y + (windowRectBefore.Y - monitorRectBefore.Y) * yRatio;
        var newWidth = windowRectBefore.Width * xRatio;
        var newHeight = windowRectBefore.Height * yRatio;
        return new Rect(newX, newY, newWidth, newHeight);
    }

    private void MenuBarItemFlyout_Opened(object sender, object e)
    {
        var menuFlyout = (MenuFlyout)sender;
        var menuFlyoutSubItem = (MenuFlyoutSubItem)menuFlyout.Items.Last();
        var status = (LyricsWindowStatus)menuFlyoutSubItem.DataContext;
        menuFlyoutSubItem.IsEnabled = status.WindowStatus == WindowStatus.Opened;

        var window = (NowPlayingWindow?)_windowManagerProvider.GetNowPlayingWindow(status);
        if (window == null) return;

        var monitorRectBefore = status.MonitorBounds;
        var windowRectBefore = status.WindowBounds;

        menuFlyoutSubItem.Items.Clear();
        var names = _monitorProvider.GetAllMonitorDeviceNames();
        foreach (var name in names)
        {
            var menuFlyoutItem = new MenuFlyoutItem { Text = name };
            menuFlyoutItem.Click += async (s, args) =>
            {
                var monitorRectAfter = _monitorProvider.GetMonitorRectFromDeviceName(name);
                var windowRectAfter = MapToMonitor(monitorRectBefore, monitorRectAfter, windowRectBefore);

                status.MonitorDeviceName = name;
                status.MonitorBounds = monitorRectAfter;

                if (status.IsWallpaper)
                {
                    window.LyricsWindowStatus.IsLocked = false;
                    await Task.Delay(500);

                    _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
                    await Task.Delay(500);

                    window.LyricsWindowStatus.IsLocked = true;
                }
                else if (status.IsPinToTaskbar)
                {
                    window.LyricsWindowStatus.IsLocked = false;
                    await Task.Delay(500);

                    _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
                    await Task.Delay(500);

                    window.LyricsWindowStatus.IsLocked = true;
                }
                else if (status.IsWorkArea)
                {
                    _windowManagerProvider.MoveAndResize(window, status.GetAppBarBounds());
                }
                else if (status.IsFullscreen)
                {
                    window.SetWindowPresenter(AppWindowPresenterKind.Overlapped);
                    _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
                    window.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
                }
                else if (status.IsMaximized)
                {
                    window.Restore();
                    _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
                    window.Maximize();
                }
                else
                {
                    _windowManagerProvider.MoveAndResize(window, windowRectAfter.ToAppRect());
                }
            };
            menuFlyoutSubItem.Items.Add(menuFlyoutItem);
        }
    }

    private void CloseConfigPanelButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigPanel.Hide();
    }
}