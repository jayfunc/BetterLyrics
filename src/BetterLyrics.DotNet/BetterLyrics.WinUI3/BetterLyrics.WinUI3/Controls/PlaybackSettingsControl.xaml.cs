using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PlaybackSettingsControl : UserControl
{
    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
    private readonly IFilePickerProvider _filePickerProvider = Ioc.Default.GetRequiredService<IFilePickerProvider>();

    public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext;

    public PlaybackSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlaybackSettingsControlViewModel>();
        WeakReferenceMessenger.Default.RegisterAll(this);

        Loaded += (_, _) =>
        {
            if (DataContext is PlaybackSettingsControlViewModel viewModel) viewModel.IsActive = true;
        };
        Unloaded += (_, _) =>
        {
            if (DataContext is PlaybackSettingsControlViewModel viewModel) viewModel.IsActive = false;
            Bindings?.StopTracking();
        };
    }

    private void AlbumArtSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        ViewModel.SelectedMediaSourceProvider?.AlbumArtSearchProvidersInfo?.Refresh();
    }

    private void LyricsSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        ViewModel.SelectedMediaSourceProvider?.LyricsSearchProvidersInfo?.Refresh();
    }

    private void MediaSourceProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        ViewModel.AppSettings.MediaSourceProvidersInfo?.Refresh();
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowConfigPanelCommand.Execute((MediaSourceProviderInfo)((Button)sender).DataContext);
    }

    public void ShowCurrentConfigPanel()
    {
        ViewModel.ShowConfigPanelCommand.Execute(ViewModel.GsmtcService.CurrentMediaSourceProviderInfo);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var data = (MediaSourceProviderInfo)((Button)sender).DataContext;
        ViewModel.AppSettings.MediaSourceProvidersInfo.Remove(data);
    }

    private async Task SaveLyricsAsync(LyricsFormat lyricsFormat)
    {
        var lyricsSearchResult = ViewModel.GsmtcService.CurrentLyricsSearchResult;
        if (lyricsSearchResult == null) return;

        var contentToWrite = LyricsConverter.Convert(
            ViewModel.GsmtcService.CurrentLyricsData,
            lyricsSearchResult!.Title,
            lyricsSearchResult!.Artist,
            lyricsSearchResult!.Album,
            lyricsSearchResult!.Duration,
            ViewModel.AppSettings.LyricsSaveConfig,
            lyricsFormat);

        if (contentToWrite == null) return;

        var ext = lyricsFormat.ToFileExtension();
        var pattern = ViewModel.AppSettings.LyricsSaveConfig.FileNamePattern;
        if (string.IsNullOrWhiteSpace(pattern)) pattern = "{Artist} - {Title}";

        var name = pattern
            .Replace("{Artist}", lyricsSearchResult.Artist ?? string.Empty)
            .Replace("{Title}", lyricsSearchResult.Title ?? string.Empty)
            .Replace("{Album}", lyricsSearchResult.Album ?? string.Empty)
            .Trim();

        var safeTitle = FileHelper.SanitizeFileName(name);
        var fileName = $"{safeTitle}{ext}";

        var folderPath = ViewModel.AppSettings.LyricsSaveConfig.SaveLocation;

        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var storageFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteTextAsync(storageFile, contentToWrite);

            _globalToastProvider.Show("ActionCompleted", storageFile.Path, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
        }
    }

    private async void BrowseLyricsSaveLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var (_, folderPath) = await _filePickerProvider.PickSingleFolderAsync(WindowType.SettingsWindow);
        if (folderPath == null) return;

        ViewModel.AppSettings.LyricsSaveConfig.SaveLocation = folderPath;
    }

    private async void SaveLyricsAsLrcMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        await SaveLyricsAsync(LyricsFormat.Lrc);
    }

    private async void SaveLyricsAsTtmlMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        await SaveLyricsAsync(LyricsFormat.Ttml);
    }
}
