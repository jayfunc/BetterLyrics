using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.ResourceService;
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
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

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
                    if (_liveStatesService.LiveStates.LyricsWindowStatus == data)
                    {
                        _liveStatesService.LiveStates.LyricsWindowStatus = ViewModel.AppSettings.WindowBoundsRecords.First();
                    }
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
                        file = await PickerHelper.PickSaveFileAsync<LyricsWindow>(fileTypeChoices);
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
                        DevWinUI.Growl.Success(_resourceService.GetLocalizedString("ExportSettingsSuccess"));
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
                file = await PickerHelper.PickSingleFileAsync<LyricsWindow>(fileTypeFilter);
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
                    DevWinUI.Growl.Success(_resourceService.GetLocalizedString("ImportSettingsSuccess"));
                }
            }
        }
    }
}
