using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
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
            var frameworkElement = ((FrameworkElement)sender).Parent;
            var index = PlayingQueueListView.FindChildIndex(frameworkElement);
            ViewModel.PlayTrackAt(index);
            DispatcherQueue.TryEnqueue(async () =>
            {
                await PlayingQueueListView.SmoothScrollIntoViewWithIndexAsync(index, ScrollItemPlacement.Center);
            });
        }

        private void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.PlayingSongIndex = -1;
            ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
        }

        private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PlayingTrack == null) return;
            DispatcherQueue.TryEnqueue(async () =>
            {
                await PlayingQueueListView.SmoothScrollIntoViewWithIndexAsync(ViewModel.PlayingSongIndex, ScrollItemPlacement.Center);
            });
        }

        private void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = ((FrameworkElement)((FrameworkElement)sender).Parent).Parent;
            bool playNext = false;
            int index = PlayingQueueListView.FindChildIndex(frameworkElement);
            if (index == ViewModel.PlayingSongIndex)
            {
                playNext = true;
            }
            ViewModel.TrackPlayingQueue.RemoveAt(index);
            if (playNext)
            {
                ViewModel.PlayingSongIndex = index;
                ViewModel.PlayTrackAt(index);
            }
        }

        private void SongFileInfoMenuFlyoutSubItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SongFileInfoFlyout.ShowAt(sender as FrameworkElement);
        }

        private void AddSongToQueueNextMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.PlayingSongIndex + 1, SongListView.SelectedItems.Cast<Track>());
            if (startPlaying)
            {
                ViewModel.PlayingSongIndex = ViewModel.PlayingSongIndex + 1;
                ViewModel.PlayTrackAt(ViewModel.PlayingSongIndex);
            }
        }

        private void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.AddRange(SongListView.SelectedItems.Cast<Track>());
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
    }
}
