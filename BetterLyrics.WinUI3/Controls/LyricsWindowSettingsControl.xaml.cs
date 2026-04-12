using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsWindowSettingsControl : UserControl
    {
        public LyricsWindowSettingsControlViewModel ViewModel => (LyricsWindowSettingsControlViewModel)DataContext;

        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public LyricsWindowStatus? LyricsWindowStatus
        {
            get { return (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(LyricsWindowSettingsControl), new PropertyMetadata(null));

        public LyricsWindowSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<LyricsWindowSettingsControlViewModel>();
        }

        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
                {
                    var windows = WindowHook.GetWindows<NowPlayingWindow>();
                    var window = windows.FirstOrDefault(x => x.LyricsWindowStatus == data);
                    window?.CloseWindow();
                    ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
                }
            }
        }

        private async void ShareMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
                {
                    IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
                    {
                        { "JSON", new List<string>() { ".json" } }
                    };
                    StorageFile? file;
                    if (this.Parent is FlyoutPresenter)
                    {
                        file = await PickerHelper.PickSaveFileAsync<NowPlayingWindow>(fileTypeChoices);
                    }
                    else
                    {
                        file = await PickerHelper.PickSaveFileAsync<SettingsWindow>(fileTypeChoices);
                    }
                    if (file != null)
                    {
                        var clonedData = (LyricsWindowStatus)data.Clone();
                        clonedData.IsDefault = false;
                        var json = System.Text.Json.JsonSerializer.Serialize(clonedData, SourceGenerationContext.Default.LyricsWindowStatus);
                        File.WriteAllText(file.Path, json);
                        GlobalToastManager.Show("ExportSettingsSuccess", null, InfoBarSeverity.Success);
                    }
                }
            }
        }

        private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
                {
                    var clonedData = (LyricsWindowStatus)data.Clone();
                    clonedData.IsDefault = false;
                    ViewModel.AppSettings.WindowBoundsRecords.Add(clonedData);
                }
            }
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                if (stackPanel.DataContext is MenuBarItemFlyout menuBarItemFlyout)
                {
                    menuBarItemFlyout.ShowAt(stackPanel);
                }
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            string[] fileTypeFilter = [".json"];
            StorageFile? file;
            if (this.Parent is FlyoutPresenter)
            {
                file = await PickerHelper.PickSingleFileAsync<NowPlayingWindow>(fileTypeFilter);
            }
            else
            {
                file = await PickerHelper.PickSingleFileAsync<SettingsWindow>(fileTypeFilter);
            }
            if (file != null)
            {
                var json = File.ReadAllText(file.Path);
                var data = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LyricsWindowStatus);
                if (data != null)
                {
                    ViewModel.AppSettings.WindowBoundsRecords.Add(data);
                    GlobalToastManager.Show("ImportSettingsSuccess", null, InfoBarSeverity.Success);
                }
            }
        }

        private void DisplayGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.DisplayPanelHeight = e.NewSize.Height;
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigNavView.SelectedItem = WindowSegmentedItem;
            LyricsWindowStatus = (LyricsWindowStatus)((Button)sender).DataContext;
            ViewModel.OpenConfigPanel();
        }

        private void EmbeddedConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigNavView.SelectedItem = WindowSegmentedItem;
            LyricsWindowStatus = _settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus;
            ViewModel.OpenConfigPanel();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseConfigPanelCommand.Execute(null);
        }

        private void ConfigNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            ViewModel.SelectorBarSelectedItemTag = (string)((NavigationViewItem)sender.SelectedItem).Tag;
        }

        private void CopyAndTransformMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
                {
                    var to = menuFlyoutItem.Tag.ToString();
                    if (to == null)
                    {
                        return;
                    }

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
                        default:
                            break;
                    }

                    ViewModel.AppSettings.WindowBoundsRecords.Add(clonedData);
                }
            }
        }

        private void WindowStatusListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            ViewModel.AppSettings.WindowBoundsRecords?.Refresh();
        }

        private void ResetPositionMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var status = (LyricsWindowStatus)((FrameworkElement)sender).DataContext;
            var window = WindowHook.GetNowPlayingWindow(status);
            window?.MoveAndResize(new(100, 100, 800, 500));
        }

        private Rect MapToMonitor(Rect monitorRectBefore, Rect monitorRectAfter, Rect windowRectBefore)
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
            menuFlyoutSubItem.IsEnabled = status.WindowStatus == Enums.WindowStatus.Opened;

            var window = WindowHook.GetNowPlayingWindow(status);
            if (window == null) return;

            var monitorRectBefore = status.MonitorBounds;
            var windowRectBefore = status.WindowBounds;

            menuFlyoutSubItem.Items.Clear();
            var names = MonitorHook.GetAllMonitorDeviceNames();
            foreach (var name in names)
            {
                var menuFlyoutItem = new MenuFlyoutItem() { Text = name };
                menuFlyoutItem.Click += async (s, args) =>
                {
                    var monitorInfoEx = MonitorHook.GetMonitorInfoExFromDeviceName(name);
                    var monitorRectAfter = monitorInfoEx.rcMonitor.ToRect();
                    var windowRectAfter = MapToMonitor(monitorRectBefore, monitorRectAfter, windowRectBefore);

                    status.MonitorDeviceName = name;
                    status.MonitorBounds = monitorRectAfter;

                    if (status.IsWallpaper)
                    {
                        window.LyricsWindowStatus.IsLocked = false;
                        await Task.Delay(500);

                        window.MoveAndResize(windowRectAfter);
                        await Task.Delay(500);

                        window.LyricsWindowStatus.IsLocked = true;
                    }
                    else if (status.IsPinToTaskbar)
                    {
                        window.LyricsWindowStatus.IsLocked = false;
                        await Task.Delay(500);

                        window.MoveAndResize(windowRectAfter);
                        await Task.Delay(500);

                        window.LyricsWindowStatus.IsLocked = true;
                    }
                    else if (status.IsWorkArea)
                    {
                        window.MoveAndResize(status.GetAppBarBounds());
                    }
                    else if (status.IsFullscreen)
                    {
                        window.SetWindowPresenter(AppWindowPresenterKind.Overlapped);
                        window.MoveAndResize(windowRectAfter);
                        window.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
                    }
                    else if (status.IsMaximized)
                    {
                        window.Restore();
                        window.MoveAndResize(windowRectAfter);
                        window.Maximize();
                    }
                    else
                    {
                        window.MoveAndResize(windowRectAfter);
                    }
                };
                menuFlyoutSubItem.Items.Add(menuFlyoutItem);
            }
        }
    }
}
