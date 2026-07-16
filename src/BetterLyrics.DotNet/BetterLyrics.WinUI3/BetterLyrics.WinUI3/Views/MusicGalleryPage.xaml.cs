using System;
using System.IO;
using System.Linq;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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

    private void OnGenericSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListViewBase listViewBase)
        {
            if (listViewBase.SelectedItems != null)
            {
                var isAlbumOrArtist = listViewBase.Name == "AlbumGridView" || listViewBase.Name == "ArtistGridView";
                if (isAlbumOrArtist)
                {
                    if (listViewBase.Name == "AlbumGridView")
                    {
                        var selectedAlbums = listViewBase.SelectedItems.Cast<AlbumModel>().ToList();
                        ViewModel.SelectedTracks = ViewModel.FilteredTracks.Where(t => selectedAlbums.Any(a => t.Album.Equals(a.Title, StringComparison.OrdinalIgnoreCase))).ToList();
                    }
                    else
                    {
                        var selectedArtists = listViewBase.SelectedItems.Cast<ArtistModel>().ToList();
                        ViewModel.SelectedTracks = ViewModel.FilteredTracks.Where(t => selectedArtists.Any(a => t.Artist.Equals(a.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                    }
                }
                else
                {
                    ViewModel.SelectedTracks = listViewBase.SelectedItems.Cast<ExtendedTrack>().ToList();
                }

                ViewModel.SelectedFirstTrack = ViewModel.SelectedTracks.FirstOrDefault();
                ViewModel.SelectedTracksTotalDuration = ViewModel.SelectedTracks.Select(x => x.Duration).Sum();

                if (SelectAllCheckBox != null && listViewBase.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    if (listViewBase.SelectedItems.Count == listViewBase.Items.Count)
                        SelectAllCheckBox.IsChecked = true;
                    else if (listViewBase.SelectedItems.Count == 0) 
                        SelectAllCheckBox.IsChecked = false;
                }
            }
        }
    }

    private void NavigateToPlaylist(string name, string icon, CommonSongProperty filterProperty, string filterValue, System.Windows.Input.ICommand? command, object? commandParam)
    {
        var playlist = new SongsTabInfo
        {
            Name = name,
            Icon = icon,
            FilterProperty = filterProperty,
            FilterValue = filterValue
        };
        ViewModel.AddToPlaylists(playlist);
        command?.Execute(commandParam);
    }

    private void ArtistHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var artist = ((ExtendedTrack)((FrameworkElement)sender).DataContext).Artist;
        NavigateToPlaylist(artist, "\uEFA9", CommonSongProperty.Artist, artist, ViewModel.SelectArtistCommand, new ArtistModel { Name = artist });
    }

    private void AlbumHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var album = ((ExtendedTrack)((FrameworkElement)sender).DataContext).Album;
        NavigateToPlaylist(album, "\uE93C", CommonSongProperty.Album, album, ViewModel.SelectAlbumCommand, new AlbumModel { Title = album });
    }

    private void PathHyperlibkButton_Click(object sender, RoutedEventArgs e)
    {
        var track = (ExtendedTrack)((FrameworkElement)sender).DataContext;
        NavigateToPlaylist(track.ParentFolderName, "\uE8B7", CommonSongProperty.Folder, track.ParentFolderPath, null, null);
    }

    private void PlaylistGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        FolderTreeView.SelectedItem = null;
        var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
        ViewModel.AddToPlaylists(playlist);
        if (playlist.FilterProperty == CommonSongProperty.Album)
        {
            ViewModel.SelectAlbumCommand.Execute(new AlbumModel { Title = playlist.FilterValue });
        }
        else if (playlist.FilterProperty == CommonSongProperty.Artist)
        {
            ViewModel.SelectArtistCommand.Execute(new ArtistModel { Name = playlist.FilterValue });
        }
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
        if (ViewModel.SongListViewSelectionMode == AppListViewSelectionMode.Multiple)
        {
            SongListView.SelectAll();
            AlbumDetailSongListView?.SelectAll();
            ArtistDetailSongListView?.SelectAll();
            AlbumGridView?.SelectAll();
            ArtistGridView?.SelectAll();
        }
    }

    private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        SongListView.SelectedItems.Clear();
        AlbumDetailSongListView?.SelectedItems.Clear();
        ArtistDetailSongListView?.SelectedItems.Clear();
        AlbumGridView?.SelectedItems.Clear();
        ArtistGridView?.SelectedItems.Clear();
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
        if (sender is MenuBarItemFlyout menuBarItemFlyout)
        {
            var targetSubItem = menuBarItemFlyout.Items.OfType<MenuFlyoutSubItem>().LastOrDefault();
            if (targetSubItem != null)
            {
                targetSubItem.Items.Clear();
                foreach (var item in ViewModel.AppSettings.StarredPlaylists)
                    if (item.FilterProperty == CommonSongProperty.M3UFilePath)
                    {
                        var menuFlyoutItem = new MenuFlyoutItem { Text = item.Name, DataContext = item };
                        menuFlyoutItem.Click += ToBeAddedPlaylistsMenuFlyoutItem_Click;
                        targetSubItem.Items.Add(menuFlyoutItem);
                    }
            }
        }
    }

    private void SongListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var listView = (ListView)sender;
        var managedElement = (FrameworkElement)e.OriginalSource;
        var clickedItem = managedElement.DataContext;

        if (clickedItem == null) return;

        if (listView.SelectionMode == ListViewSelectionMode.Single)
            listView.SelectedItem = clickedItem;
        else if (listView.SelectionMode == ListViewSelectionMode.Multiple)
            if (!listView.SelectedItems.Contains(clickedItem))
                listView.SelectedItems.Add(clickedItem);
    }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AlbumModel model)
        {
            var album = model.Title;
            var playlist = new SongsTabInfo
            {
                Name = album,
                Icon = "\uE93C",
                FilterProperty = CommonSongProperty.Album,
                FilterValue = album
            };
            ViewModel.AddToPlaylists(playlist);

            ViewModel.SelectAlbumCommand.Execute(model);
        }
    }

    private void ArtistGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ArtistModel model)
        {
            var artist = model.Name;
            var playlist = new SongsTabInfo
            {
                Name = artist,
                Icon = "\uEFA9",
                FilterProperty = CommonSongProperty.Artist,
                FilterValue = artist
            };
            ViewModel.AddToPlaylists(playlist);

            ViewModel.SelectArtistCommand.Execute(model);
        }
    }



}