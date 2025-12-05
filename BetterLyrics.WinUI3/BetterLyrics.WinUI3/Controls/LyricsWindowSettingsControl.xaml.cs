using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NTextCat.Commons;
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
            get { return (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty); }
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
                    // TODO  处理状态被删除后的逻辑：提示用户？直接关闭对应窗口如果已经打开？
                    ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
                }
            }
        }

        private void SetDefaultMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LyricsWindowStatus data)
                {
                    ViewModel.AppSettings.WindowBoundsRecords.ForEach(x => x.IsDefault = false);
                    data.IsDefault = true;
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

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Pivot pivot)
            {
                if (pivot.SelectedItem is PivotItem pivotItem)
                {
                    ViewModel?.ListViewSelectedItemTag = pivotItem.Tag;
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
    }
}
