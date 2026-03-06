using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SMTCService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicGalleryPage : Page
    {
        public MusicGalleryPageViewModel ViewModel => (MusicGalleryPageViewModel)DataContext;
        private readonly ISMTCService _smtcService = Ioc.Default.GetRequiredService<ISMTCService>();

        public MusicGalleryPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MusicGalleryPageViewModel>();
        }

        private async void SongPathHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await LauncherHelper.SelectAndShowFileAsync(((ExtendedTrack)((HyperlinkButton)sender).DataContext).Uri.ToDecodedAbsoluteUri());
        }

        private async void AddSongToQueueNextMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
            _smtcService.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await _smtcService.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private async void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
            foreach (var item in SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)))
            {
                _smtcService.TrackPlayingQueue.Add(item);
            }
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await _smtcService.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTracks = SongListView.SelectedItems.Cast<ExtendedTrack>().ToList();
            ViewModel.SelectedTracksTotalDuration = ViewModel.SelectedTracks.Select(x => x.Duration).Sum();
            if (SelectAllCheckBox != null)
            {
                if (SongListView.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    if (SongListView.SelectedItems.Count == SongListView.Items.Count)
                    {
                        SelectAllCheckBox.IsChecked = true;
                    }
                    else if (SongListView.SelectedItems.Count == 0)
                    {
                        SelectAllCheckBox.IsChecked = false;
                    }
                }
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
            var track = ((ExtendedTrack)((FrameworkElement)sender).DataContext);
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
            if (ViewModel.AppSettings.MusicGallerySettings.StopOnWindowClosed)
            {
                ViewModel.StopTrackCommand.Execute(null);
            }
        }

        private void RemoveFromPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
            ViewModel.AppSettings.StarredPlaylists.Remove(playlist);
            ViewModel.SelectedSongsTabInfoIndex = 0;
            ViewModel.ApplyPlaylist();
        }

        private void SongListViewItemMoreButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackRightTapped = (ExtendedTrack)((FrameworkElement)sender).DataContext;
            SongFileInfoFlyout.ShowAt(sender as FrameworkElement);
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SongListViewSelectionMode == ListViewSelectionMode.Multiple)
            {
                SongListView.SelectAll();
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SongListView.SelectedItems.Clear();
        }

        private void SongListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var displayedTracks = SongListView.Items.Cast<ExtendedTrack>();
            var track = (ExtendedTrack)((FrameworkElement)sender).DataContext;

            // Play all the songs
            _smtcService.TrackPlayingQueue.Clear();
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;

            _smtcService.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, displayedTracks.Select(x => new PlayQueueItem(x)));
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = displayedTracks.ToList().IndexOf(track);
            _ = _smtcService.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = ViewModel.AppSettings.MusicGallerySettings;
            if (settings.AutoPlay)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    _ = _smtcService.PlayTrackAtAsync(settings.PlayQueueIndex);
                });
            }
        }

        private void FolderTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            ViewModel.SelectedSongsTabInfoIndex = -1;
            if (args.InvokedItem is FolderNode selectedFolder)
            {
                ViewModel.SelectFolder(selectedFolder);
            }
        }

        private void ToBeAddedPlaylistsMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var songsTabInfo = ((SongsTabInfo)((FrameworkElement)sender).DataContext);
            if (songsTabInfo.FilterProperty == CommonSongProperty.M3UFilePath)
            {
                if (songsTabInfo.FilterValue is string path)
                {
                    if (File.Exists(path))
                    {
                        var content = File.ReadAllText(path);
                        foreach (var item in ViewModel.SelectedTracks.Select(x => x.Uri.ToDecodedAbsoluteUri()).ToList())
                        {
                            if (!content.Contains(item))
                            {
                                content += Environment.NewLine;
                                content += item;
                            }
                        }
                        File.WriteAllText(path, content);
                        GlobalToastManager.Show("TracksAddToPlaylistSuccessfully", null, InfoBarSeverity.Success);
                    }
                    else
                    {
                        GlobalToastManager.Show("TracksAddToPlaylistFailed", null, InfoBarSeverity.Error);
                    }
                }
            }
        }

        private void AddToMenuBarItemFlyout_Opened(object sender, object e)
        {
            AddToCustomListMenuFlyoutSubItem.Items.Clear();
            foreach (var item in ViewModel.AppSettings.StarredPlaylists)
            {
                if (item.FilterProperty == CommonSongProperty.M3UFilePath)
                {
                    var menuFlyoutItem = new MenuFlyoutItem
                    {
                        Text = item.Name,
                        DataContext = item,
                    };
                    menuFlyoutItem.Click += ToBeAddedPlaylistsMenuFlyoutItem_Click;
                    AddToCustomListMenuFlyoutSubItem.Items.Add(menuFlyoutItem);
                }
            }
        }

    }
}
