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
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml.Templates;
using global::Avalonia.Controls;
using global::Avalonia.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.Avalonia.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicGalleryPage : UserControl
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

    private async void SongPathHyperlinkButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _launcherProvider.SelectAndShowFileAsync(((ExtendedTrack)((HyperlinkButton)sender).DataContext).Uri
            .ToDecodedAbsoluteUri());
    }

    private void AddSongToQueueNextMenuItem_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
        _smtcService.TrackPlayingQueue.InsertRange(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1,
            SongListBox.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)));
        if (startPlaying)
        {
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex =
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
            _smtcService.PlayTrackAt(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }
    }

    private void AddSongToQueueEndMenuItem_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var startPlaying = _smtcService.TrackPlayingQueue.Count == 0;
        foreach (var item in SongListBox.SelectedItems.Cast<ExtendedTrack>().Select(x => new PlayQueueItem(x)))
            _smtcService.TrackPlayingQueue.Add(item);

        if (startPlaying)
        {
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex =
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex + 1;
            _smtcService.PlayTrackAt(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }
    }

    private void SongListBox_SelectionChanged(object sender, global::Avalonia.Controls.SelectionChangedEventArgs e)
    {
        ViewModel.SelectedTracks = SongListBox.SelectedItems.Cast<ExtendedTrack>().ToList();
        ViewModel.SelectedFirstTrack = ViewModel.SelectedTracks.FirstOrDefault();
        ViewModel.SelectedTracksTotalDuration = ViewModel.SelectedTracks.Select(x => x.Duration).Sum();
        if (SelectAllCheckBox != null)
            if (SongListBox.SelectionMode == SelectionMode.Multiple)
            {
                if (SongListBox.SelectedItems.Count == SongListBox.Items.Count)
                    SelectAllCheckBox.IsChecked = true;
                else if (SongListBox.SelectedItems.Count == 0) SelectAllCheckBox.IsChecked = false;
            }
    }

    private void ArtistHyperlibkButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var artist = ((ExtendedTrack)((global::Avalonia.Controls.Control)sender).DataContext).Artist;
        var playlist = new SongsTabInfo
        {
            Name = artist,
            Icon = "\uEFA9",
            FilterProperty = CommonSongProperty.Artist,
            FilterValue = artist
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void AlbumHyperlibkButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var album = ((ExtendedTrack)((global::Avalonia.Controls.Control)sender).DataContext).Album;
        var playlist = new SongsTabInfo
        {
            Name = album,
            Icon = "\uE93C",
            FilterProperty = CommonSongProperty.Album,
            FilterValue = album
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void PathHyperlibkButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var track = (ExtendedTrack)((global::Avalonia.Controls.Control)sender).DataContext;
        var playlist = new SongsTabInfo
        {
            Name = track.ParentFolderName,
            Icon = "\uE8B7",
            FilterProperty = CommonSongProperty.Folder,
            FilterValue = track.ParentFolderPath
        };
        ViewModel.AddToPlaylists(playlist);
    }

    private void PlaylistGrid_Tapped(object sender, global::Avalonia.Input.TappedEventArgs e)
    {
        FolderTreeView.SelectedItem = null;
        var playlist = (SongsTabInfo)((global::Avalonia.Controls.Control)sender).DataContext;
        ViewModel.AddToPlaylists(playlist);
    }

    private void Page_Unloaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.CancelRefreshSongs();
        if (ViewModel.AppSettings.MusicGallerySettings.StopOnWindowClosed) ViewModel.StopTrackCommand.Execute(null);
    }

    private void RemoveFromPlaylistMenuItem_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var playlist = (SongsTabInfo)((global::Avalonia.Controls.Control)sender).DataContext;
        ViewModel.AppSettings.StarredPlaylists.Remove(playlist);
        ViewModel.SelectedSongsTabInfoIndex = 0;
        ViewModel.ApplyPlaylist();
    }

    private async void OpenPlaylistInFileExplorerMenuItem_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var playlist = (SongsTabInfo)((global::Avalonia.Controls.Control)sender).DataContext;
        await _launcherProvider.SelectAndShowFileAsync(playlist.FilterValue);
    }

    private void SelectAllCheckBox_IsCheckedChanged(object sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }

    private void SelectAllCheckBox_Unchecked(object sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }

    private void SongListBoxItem_DoubleTapped(object sender, global::Avalonia.Input.TappedEventArgs e)
    {
        var track = (ExtendedTrack)((global::Avalonia.Controls.Control)sender).DataContext;
        ViewModel.PlayCommand.Execute(track);
    }

    private void FolderTreeView_ItemInvoked(TreeView sender, global::Avalonia.Interactivity.RoutedEventArgs args)
    {
        ViewModel.SelectedSongsTabInfoIndex = -1;
        // if (args.Item is FolderNode selectedFolder) ViewModel.SelectFolder(selectedFolder);
    }

    private void ToBeAddedPlaylistsMenuItem_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var songsTabInfo = (SongsTabInfo)((global::Avalonia.Controls.Control)sender).DataContext;
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
        AddToCustomListMenuItem.Items.Clear();
        foreach (var item in ViewModel.AppSettings.StarredPlaylists)
            if (item.FilterProperty == CommonSongProperty.M3UFilePath)
            {
                var menuFlyoutItem = new MenuItem
                {
                    Header = item.Name,
                    DataContext = item
                };
                menuFlyoutItem.Click += ToBeAddedPlaylistsMenuItem_Click;
                AddToCustomListMenuItem.Items.Add(menuFlyoutItem);
            }
    }

    private void SortComboBox_SelectionChanged(object sender, global::Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (SongListBox == null) return;

        if (sender is ComboBox comboBox)
            SongListBox.ItemTemplate = comboBox.SelectedIndex switch
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

    private void SongListBox_RightTapped(object sender, global::Avalonia.Input.TappedEventArgs e)
    {
        var managedElement = (global::Avalonia.Controls.Control)e.Source;
        var clickedItem = managedElement.DataContext;

        if (clickedItem == null) return;

        if (SongListBox.SelectionMode == SelectionMode.Single)
            SongListBox.SelectedItem = clickedItem;
        else if (SongListBox.SelectionMode == SelectionMode.Multiple)
            if (!SongListBox.SelectedItems.Contains(clickedItem))
                SongListBox.SelectedItems.Add(clickedItem);
    }
}