using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;

using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using DevWinUI;
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
        public MusicGalleryPageViewModel ViewModel => (MusicGalleryPageViewModel)DataContext;

        public bool IsPlayingQueueOpened
        {
            get { return (bool)GetValue(IsPlayingQueueOpenedProperty); }
            set { SetValue(IsPlayingQueueOpenedProperty, value); }
        }

        public static readonly DependencyProperty IsPlayingQueueOpenedProperty =
            DependencyProperty.Register(nameof(IsPlayingQueueOpened), typeof(bool), typeof(MusicGalleryPage), new PropertyMetadata(false, OnDependencyPropertyChanged));

        public MusicGalleryPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MusicGalleryPageViewModel>();
            ViewModel.AppSettings.MusicGallerySettings.PropertyChanged += MusicGallerySettings_PropertyChanged;
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MusicGalleryPage self)
            {
                if (e.Property == IsPlayingQueueOpenedProperty)
                {
                    var newValue = (bool)e.NewValue;
                    self.PlayQueue.Translation = newValue ? new() : new(310, 0, 0);
                }
            }
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
            await LauncherHelper.SelectAndShowFile(((ExtendedTrack)((HyperlinkButton)sender).DataContext).DecodedAbsoluteUri);
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
            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private async void AddSongToQueueEndMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool startPlaying = ViewModel.TrackPlayingQueue.Count == 0;
            ViewModel.TrackPlayingQueue.AddRange(SongListView.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)));
            if (startPlaying)
            {
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
                await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTracks = SongListView.SelectedItems.Cast<ExtendedTrack>().ToList();
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
            SongListView.SelectAll();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SongListView.SelectedItems.Clear();
        }

        private async void SongListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var displayedTracks = SongListView.Items.Cast<ExtendedTrack>();
            var track = (ExtendedTrack)((FrameworkElement)sender).DataContext;

            // Play all the songs
            ViewModel.TrackPlayingQueue.Clear();
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;

            ViewModel.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1, displayedTracks.Select(x => new PlayQueueItem(x)));
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = displayedTracks.ToList().IndexOf(track);
            await ViewModel.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = ViewModel.AppSettings.MusicGallerySettings;
            if (settings.AutoPlay)
            {
                _ = ViewModel.PlayTrackAtAsync(settings.PlayQueueIndex);
            }
            ScrollToPlayingItem();
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
                        foreach (var item in ViewModel.SelectedTracks.Select(x => x.DecodedAbsoluteUri).ToList())
                        {
                            if (!content.Contains(item))
                            {
                                content += Environment.NewLine;
                                content += item;
                            }
                        }
                        File.WriteAllText(path, content);
                        ToastHelper.ShowToast("TracksAddToPlaylistSuccessfully", null, InfoBarSeverity.Success);
                    }
                    else
                    {
                        ToastHelper.ShowToast("TracksAddToPlaylistFailed", null, InfoBarSeverity.Error);
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
