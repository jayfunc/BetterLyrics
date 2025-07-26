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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshSongs();
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

        private void SongListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var track = (Track)((FrameworkElement)sender).DataContext;
            ViewModel.InitPlayingQueue(track);
            PlayingQueueListView.ScrollIntoView(track, ScrollIntoViewAlignment.Leading);
        }

        private void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.PlayingSongIndex = -1;
        }

        private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PlayingTrack == null) return;
            PlayingQueueListView.ScrollIntoView(ViewModel.PlayingTrack, ScrollIntoViewAlignment.Leading);
        }

        private void RemoveFromPlayingQueueMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var track = (Track)((FrameworkElement)sender).DataContext;
            ViewModel.TrackPlayingQueue.Remove(track);
        }
    }
}
