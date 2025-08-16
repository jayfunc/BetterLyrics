using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicGalleryPage : Page
    {
        public MusicGalleryViewModel ViewModel => (MusicGalleryViewModel)DataContext;

        public MusicGalleryPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MusicGalleryViewModel>();
        }

        private void SongListViewItemGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ViewModel.TrackRightTapped = (Track)((FrameworkElement)sender).DataContext;
            SongFileInfoFlyout.ShowAt(sender as FrameworkElement);
        }

        private async void SongPathHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await LauncherHelper.SelectAndShowFile($"{((HyperlinkButton)sender).Content}");
        }

        private void PlayingQueueListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
            ViewModel.PlayTrack(item);
            PlayingQueueListView.ScrollIntoView(item);
        }

        private void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.PlayingSongIndex = -1;
            ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
        }

        private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PlayingQueueItem == null) return;
            PlayingQueueListView.ScrollIntoView(ViewModel.PlayingQueueItem);
        }

        private void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
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
                ViewModel.PlayingSongIndex = index;
                ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
            }
        }

        private void SongFileInfoMenuFlyoutSubItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SongFileInfoFlyout.ShowAt(sender as FrameworkElement);
        }

        private void AddSongToQueueNextMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.PlayingSongIndex + 1, SongListView.SelectedItems.Cast<Track>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.PlayingSongIndex = ViewModel.PlayingSongIndex + 1;
                ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
            }
        }

        private void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.AddRange(SongListView.SelectedItems.Cast<Track>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.PlayingSongIndex = ViewModel.PlayingSongIndex + 1;
                ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
            }
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTracks = SongListView.SelectedItems.Cast<Track>().ToList();
            SelectAllToggleButton.IsChecked = SongListView.SelectedItems.Count == SongListView.Items.Count;
        }

        private void SelectAllToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllToggleButton.IsChecked == true)
            {
                SongListView.SelectAll();
            }
            else
            {
                SongListView.SelectedItems.Clear();
            }
        }

        private void ArtistHyperlibkButton_Click(object sender, RoutedEventArgs e)
        {
            var artist = (string)((HyperlinkButton)sender).Tag;
            var playlist = new SongsTabInfo(artist, "\uEFA9", true, CommonSongProperty.Artist, artist);
            ViewModel.UpdateSelectedPlaylist(playlist);
        }

        private void AlbumHyperlibkButton_Click(object sender, RoutedEventArgs e)
        {
            var album = (string)((HyperlinkButton)sender).Tag;
            var playlist = new SongsTabInfo(album, "\uE93C", true, CommonSongProperty.Album, album);
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

        private void PlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.PlayingSongIndex = -1;

            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.PlayingSongIndex + 1, SongListView.Items.Cast<Track>().Select(x => new PlayQueueItem(x)));
            ViewModel.PlayingSongIndex = ViewModel.PlayingSongIndex + 1;
            ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelRefreshSongs();
        }
    }
}
