using System;
using System.IO;
using System.Linq;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicGalleryPage : Page
{
    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly ILauncherProvider _launcherProvider =
        Ioc.Default.GetRequiredService<ILauncherProvider>();

    private readonly ISmtcService _smtcService = Ioc.Default.GetRequiredService<ISmtcService>();

    public MusicGalleryPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MusicGalleryPageViewModel>();
    }

    public MusicGalleryPageViewModel ViewModel => (MusicGalleryPageViewModel)DataContext;

    private async void SongPathHyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        await _launcherProvider.SelectAndShowFileAsync(((ExtendedTrack)((HyperlinkButton)sender).DataContext).Uri
            .ToDecodedAbsoluteUri());
    }

    private void AddSongToQueueNextMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
        _smtcService.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1,
            SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)));
        if (startPlaying)
        {
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex =
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
            _smtcService.PlayTrackAt(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }
    }

    private void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
        foreach (var item in SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)))
            _smtcService.TrackPlayingQueue.Add(item);

        if (startPlaying)
        {
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex =
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
            _smtcService.PlayTrackAt(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }
    }

    private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedTracks = SongListView.SelectedItems.Cast<ExtendedTrack>().ToList();
        ViewModel.SelectedFirstTrack = ViewModel.SelectedTracks.FirstOrDefault();
        ViewModel.SelectedTracksTotalDuration = ViewModel.SelectedTracks.Select(x => x.Duration).Sum();
        if (SelectAllCheckBox != null)
            if (SongListView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                if (SongListView.SelectedItems.Count == SongListView.Items.Count)
                    SelectAllCheckBox.IsChecked = true;
                else if (SongListView.SelectedItems.Count == 0) SelectAllCheckBox.IsChecked = false;
            }
    }

    private void ArtistHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var artist = ((ExtendedTrack)((FrameworkElement)sender).DataContext).Artist;
        var playlist = new SongsTabInfo
        {
            Name = artist,
            Icon = "\uEFA9",
            FilterProperty = CommonSongProperty.Artist,
            FilterValue = artist
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void AlbumHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var album = ((ExtendedTrack)((FrameworkElement)sender).DataContext).Album;
        var playlist = new SongsTabInfo
        {
            Name = album,
            Icon = "\uE93C",
            FilterProperty = CommonSongProperty.Album,
            FilterValue = album
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void PathHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var track = (ExtendedTrack)((FrameworkElement)sender).DataContext;
        var playlist = new SongsTabInfo
        {
            Name = track.ParentFolderName,
            Icon = "\uE8B7",
            FilterProperty = CommonSongProperty.Folder,
            FilterValue = track.ParentFolderPath
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void PlaylistGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        FolderTreeView.SelectedItem = null;
        var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
        ViewModel.AddToPlaylists(playlist);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelRefreshSongs();
        if (ViewModel.AppSettings.MusicGallerySettings.StopOnWindowClosed) ViewModel.StopTrackCommand.Execute(null);
    }

    private void RemoveFromPlaylistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
        ViewModel.AppSettings.StarredPlaylists.Remove(playlist);
        ViewModel.SelectedSongsTabInfoIndex = 0;
        ViewModel.ApplyPlaylist();
    }

    private async void OpenPlaylistInFileExplorerMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
        await _launcherProvider.SelectAndShowFileAsync(playlist.FilterValue);
    }

    private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SongListViewSelectionMode == AppListViewSelectionMode.Multiple) SongListView.SelectAll();
    }

    private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        SongListView.SelectedItems.Clear();
    }

    private void SongListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var track = (ExtendedTrack)((FrameworkElement)sender).DataContext;
        ViewModel.PlayCommand.Execute(track);
    }

    private void FolderTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        ViewModel.SelectedSongsTabInfoIndex = -1;
        if (args.InvokedItem is FolderNode selectedFolder) ViewModel.SelectFolder(selectedFolder);
    }

    private void ToBeAddedPlaylistsMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var songsTabInfo = (SongsTabInfo)((FrameworkElement)sender).DataContext;
        if (songsTabInfo.FilterProperty == CommonSongProperty.M3UFilePath)
            if (songsTabInfo.FilterValue is string path)
            {
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    foreach (var item in ViewModel.SelectedTracks.Select(x => x.Uri.ToDecodedAbsoluteUri())
                                 .ToList())
                        if (!content.Contains(item))
                        {
                            content += Environment.NewLine;
                            content += item;
                        }

                    File.WriteAllText(path, content);
                    _globalToastProvider.Show("TracksAddToPlaylistSuccessfully", null, MessageSeverity.Success);
                }
                else
                {
                    _globalToastProvider.Show("TracksAddToPlaylistFailed", null, MessageSeverity.Error);
                }
            }
    }

    private void AddToMenuBarItemFlyout_Opened(object sender, object e)
    {
        AddToCustomListMenuFlyoutSubItem.Items.Clear();
        foreach (var item in ViewModel.AppSettings.StarredPlaylists)
            if (item.FilterProperty == CommonSongProperty.M3UFilePath)
            {
                var menuFlyoutItem = new MenuFlyoutItem
                {
                    Text = item.Name,
                    DataContext = item
                };
                menuFlyoutItem.Click += ToBeAddedPlaylistsMenuFlyoutItem_Click;
                AddToCustomListMenuFlyoutSubItem.Items.Add(menuFlyoutItem);
            }
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongListView == null) return;

        if (sender is ComboBox comboBox)
            SongListView.ItemTemplate = comboBox.SelectedIndex switch
            {
                // 标题
                0 => (DataTemplate)Resources["TitleSortTemplate"],
                // 专辑
                1 => (DataTemplate)Resources["AlbumSortTemplate"],
                // 艺术家
                2 => (DataTemplate)Resources["ArtistSortTemplate"],
                // 文件夹
                3 => (DataTemplate)Resources["FolderSortTemplate"],
                _ => (DataTemplate)Resources["TitleSortTemplate"]
            };
    }

    private void SongListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var managedElement = (FrameworkElement)e.OriginalSource;
        var clickedItem = managedElement.DataContext;

        if (clickedItem == null) return;

        if (SongListView.SelectionMode == ListViewSelectionMode.Single)
            SongListView.SelectedItem = clickedItem;
        else if (SongListView.SelectionMode == ListViewSelectionMode.Multiple)
            if (!SongListView.SelectedItems.Contains(clickedItem))
                SongListView.SelectedItems.Add(clickedItem);
    }
}