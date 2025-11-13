using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicGalleryPage : Page
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public MusicGalleryViewModel ViewModel => (MusicGalleryViewModel)DataContext;

        public MusicGalleryPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MusicGalleryViewModel>();
            ViewModel.AppSettings.MusicGallerySettings.PropertyChanged += MusicGallerySettings_PropertyChanged;
        }

        private void ScrollToPlayingItem()
        {
            if (ViewModel.PlayingQueueItem == null) return;
            if (PlayingQueueListView == null) return;
            PlayingQueueListView.ScrollIntoView(ViewModel.PlayingQueueItem);
        }

        private void MusicGallerySettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicGallerySettings.PlayQueueIndex))
            {
                ScrollToPlayingItem();
            }
        }

        private async void SongPathHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await LauncherHelper.SelectAndShowFile(((Track)((HyperlinkButton)sender).DataContext).Path);
        }

        private async void PlayingQueueListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
            await ViewModel.PlayTrackAsync(item);
            PlayingQueueListView.ScrollIntoView(item);
        }

        private async void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;
            await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }

        private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollToPlayingItem();
        }

        private async void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            bool playNext = false;
            var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
            int index = ViewModel.TrackPlayingQueue.IndexOf(item);
            if (item == ViewModel.PlayingQueueItem)
            {
                playNext = true;
            }
            ViewModel.TrackPlayingQueue.Remove(item);
            if (playNext)
            {
                if (ViewModel.TrackPlayingQueue.Count == 0)
                {
                    index = -1;
                }
                else if (index >= ViewModel.TrackPlayingQueue.Count)
                {
                    index = ViewModel.TrackPlayingQueue.Count - 1;
                }
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = index;
                await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private async void AddSongToQueueNextMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, SongListView.SelectedItems.Cast<Track>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private async void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.AddRange(SongListView.SelectedItems.Cast<Track>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTracks = SongListView.SelectedItems.Cast<Track>().ToList();
            ViewModel.SelectedTracksTotalDuration = ViewModel.SelectedTracks.Select(x => x.Duration).Sum();
            if (SelectAllCheckBox != null)
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

        private void ArtistHyperlibkButton_Click(object sender, RoutedEventArgs e)
        {
            var artist = ((Track)((FrameworkElement)sender).DataContext).Artist;
            var playlist = new SongsTabInfo(artist, "\uEFA9", true, false, CommonSongProperty.Artist, artist);
            ViewModel.UpdateSelectedPlaylist(playlist);
        }

        private void AlbumHyperlibkButton_Click(object sender, RoutedEventArgs e)
        {
            var album = ((Track)((FrameworkElement)sender).DataContext).Album;
            var playlist = new SongsTabInfo(album, "\uE93C", true, false, CommonSongProperty.Album, album);
            ViewModel.UpdateSelectedPlaylist(playlist);
        }

        private void PathHyperlibkButton_Click(object sender, RoutedEventArgs e)
        {
            var track = ((Track)((FrameworkElement)sender).DataContext);
            var playlist = new SongsTabInfo(track.GetParentFolderName(), "\uE8B7", true, false, CommonSongProperty.Folder, track.GetParentFolderPath());
            ViewModel.UpdateSelectedPlaylist(playlist);
        }

        private void PlaylistGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
            ViewModel.UpdateSelectedPlaylist(playlist);
        }

        private void PlaylistCloseButton_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
            ViewModel.SongsTabInfoList.Remove(playlist);
            ViewModel.SelectedSongsTabInfoIndex = 0;
            ViewModel.ApplyPlaylist();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelRefreshSongs();
        }

        private void PlaylistFavButton_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (SongsTabInfo)((FrameworkElement)sender).DataContext;
            var targetStatus = !playlist.IsStarred;
            if (targetStatus)
            {
                ViewModel.AppSettings.StarredPlaylists.Add(playlist);
            }
            else
            {
                ViewModel.AppSettings.StarredPlaylists.Remove(playlist);
            }
            playlist.IsStarred = targetStatus;
        }

        private void StarredPlaylistsListViewItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var songsTabInfo = ((SongsTabInfo)((FrameworkElement)sender).DataContext);
            if (!ViewModel.SongsTabInfoList.Contains(songsTabInfo))
            {
                ViewModel.SongsTabInfoList.Add(songsTabInfo);
            }
        }

        private void SongListViewItemMoreButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackRightTapped = (Track)((FrameworkElement)sender).DataContext;
            SongFileInfoFlyout.ShowAt(sender as FrameworkElement);
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SongListView.SelectAll();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SongListView.SelectedItems.Clear();
        }

        private void AddToPlaylistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ((MenuFlyoutItem)sender).ContextFlyout.ShowAt(PlaylistButton);
        }

        private void ToBeAddedPlaylistsListViewItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var songsTabInfo = ((SongsTabInfo)((FrameworkElement)sender).DataContext);
            if (songsTabInfo.FilterProperty == CommonSongProperty.M3UFilePath)
            {
                if (songsTabInfo.FilterValue is string path)
                {
                    if (File.Exists(path))
                    {
                        var content = File.ReadAllText(path);
                        foreach (var item in ViewModel.SelectedTracks.Select(x => x.Path).ToList())
                        {
                            if (!content.Contains(item))
                            {
                                content += Environment.NewLine;
                                content += item;
                            }
                        }
                        File.WriteAllText(path, content);
                        DevWinUI.Growl.Success(_resourceService.GetLocalizedString("TracksAddToPlaylistSuccessfully"), path);
                    }
                    else
                    {
                        DevWinUI.Growl.Error(_resourceService.GetLocalizedString("TracksAddToPlaylistFailed"), path);
                    }
                }
            }
        }

        private async void SongListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var displayedTracks = SongListView.Items.Cast<Track>();
            var track = (Track)((FrameworkElement)sender).DataContext;

            // Play all the songs
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;

            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, displayedTracks.Select(x => new PlayQueueItem(x)));
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = displayedTracks.ToList().IndexOf(track);
            await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToPlayingItem();
        }
    }
}
