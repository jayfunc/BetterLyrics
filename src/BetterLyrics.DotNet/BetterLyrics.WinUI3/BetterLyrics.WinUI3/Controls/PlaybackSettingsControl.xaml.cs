using System;
using Windows.Storage;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PlaybackSettingsControl : UserControl, IRecipient<PropertyChangedMessage<bool>>
{
    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    public PlaybackSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlaybackSettingsControlViewModel>();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == ViewModel && message.PropertyName == nameof(ViewModel.IsDeepLinkRequested))
        {
            if (this.IsLoaded)
            {
                CheckAndProcessDeepLink();
            }
        }
    }

    private bool CheckAndProcessDeepLink()
    {
        if (ViewModel.IsDeepLinkRequested)
        {
            ViewModel.IsDeepLinkRequested = false;

            foreach (NavigationViewItem item in ConfigNavView.MenuItems.Cast<NavigationViewItem>())
            {
                if ((PlaybackLibSettingsSection)item.Tag == ViewModel.SelectedPlaybackLibSettingsSection)
                {
                    ConfigNavView.SelectedItem = item;
                    break;
                }
            }

            if (ViewModel.SelectedMediaSourceProvider != null)
            {
                PlaybackConfigPanel.Show();
                return true;
            }
        }
        return false;
    }

    public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext;

    public bool HideConfigPanelWhenLoaded { get; set; } = true;

    private void AlbumArtSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        //  AlbumArtSearchProvidersInfo  CollectionChanged ?
        ViewModel.SelectedMediaSourceProvider?.AlbumArtSearchProvidersInfo?.Refresh();
    }

    private void LyricsSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        //  LyricsSearchProvidersInfo  CollectionChanged ?
        ViewModel.SelectedMediaSourceProvider?.LyricsSearchProvidersInfo?.Refresh();
    }

    private void MediaSourceProvidersListView_DragItemsCompleted(ListViewBase sender,
        DragItemsCompletedEventArgs args)
    {
        //  MediaSourceProvidersInfo  CollectionChanged ?
        ViewModel.AppSettings.MediaSourceProvidersInfo?.Refresh();
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ShowConfigPanel((MediaSourceProviderInfo)((Button)sender).DataContext);
    }

    private void ShowConfigPanel(MediaSourceProviderInfo? info)
    {
        if (info == null) return;

        ViewModel.SelectedMediaSourceProvider = info;
        PlaybackConfigPanel.Show();
    }

    public void ShowCurrentConfigPanel()
    {
        ShowConfigPanel(ViewModel.GsmtcService.CurrentMediaSourceProviderInfo);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var data = (MediaSourceProviderInfo)((Button)sender).DataContext;
        ViewModel.AppSettings.MediaSourceProvidersInfo.Remove(data);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (HideConfigPanelWhenLoaded)
        {
            if (!CheckAndProcessDeepLink())
            {
                ViewModel.SelectedMediaSourceProvider = null;
                PlaybackConfigPanel.Hide();
            }
        }

        if (ConfigNavView.SelectedItem == null)
        {
            GeneralNavViewItem.IsSelected = true;
        }
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

    private void CloseConfigPanelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedMediaSourceProvider = null;
        PlaybackConfigPanel.Hide();
    }

    private void ConfigNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is PlaybackLibSettingsSection section)
        {
            ViewModel.SelectedPlaybackLibSettingsSection = section;
        }
    }
}
