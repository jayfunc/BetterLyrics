using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsWindowSettingsControl : UserControl
    {
        public LyricsWindowSettingsControlViewModel ViewModel => (LyricsWindowSettingsControlViewModel)DataContext;

        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public LyricsWindowStatus LyricsWindowStatus
        {
            get { return (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(LyricsWindowSettingsControl), new PropertyMetadata(default));

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

        private void SetDefaultMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (element.DataContext is LyricsWindowStatus data)
                {
                    data.IsDefault = !data.IsDefault;
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
                        ToastHelper.ShowToast("ExportSettingsSuccess", null, InfoBarSeverity.Success);
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
                    ToastHelper.ShowToast("ImportSettingsSuccess", null, InfoBarSeverity.Success);
                }
            }
        }

        private void DisplayGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.DisplayPanelHeight = e.NewSize.Height;
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            WindowSegmentedItem.IsEnabled = LayoutSegmentedItem.IsEnabled = true;
            ConfigSegmented.SelectedItem = WindowSegmentedItem;
            LyricsWindowStatus = (LyricsWindowStatus)((Button)sender).DataContext;
            ViewModel.OpenConfigPanel();
        }

        private void EmbeddedConfigButton_Click(object sender, RoutedEventArgs e)
        {
            WindowSegmentedItem.IsEnabled = LayoutSegmentedItem.IsEnabled = false;
            ConfigSegmented.SelectedItem = AlbumArtStyleSegmentedItem;
            LyricsWindowStatus = _settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus;
            ViewModel.OpenConfigPanel();
        }

        private void DemoWindowGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var status = (LyricsWindowStatus)(((FrameworkElement)sender).DataContext);
            // 多开模式
            if (_settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
            {
                WindowHook.OpenOrShowWindow<NowPlayingWindow>(status);
            }
            // 单例模式
            else
            {
                var openedWindows = WindowHook.GetWindows<NowPlayingWindow>();
                foreach (var item in openedWindows.Where(x => x.LyricsWindowStatus != status))
                {
                    item.CloseWindow();
                }
                WindowHook.OpenOrShowWindow<NowPlayingWindow>(status);
            }
        }

        private void ConfigSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (sender is SelectorBar bar)
            {
                if (bar.SelectedItem is SelectorBarItem item)
                {
                    ViewModel?.SelectorBarSelectedItemTag = item.Tag;
                }
            }
        }

        private void CloseStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (element.DataContext is LyricsWindowStatus data)
                {
                    var window = WindowHook.GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == data);
                    window?.CloseWindow();
                }
            }
        }

        private void ConfigSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectorBarSelectedItemTag = (string)((SegmentedItem)((Segmented)sender).SelectedItem).Tag;
        }
    }
}
