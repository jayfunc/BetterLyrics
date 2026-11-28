using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PlayQueue : BaseUserControl
{
    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    public PlayQueueViewModel ViewModel => (PlayQueueViewModel)DataContext;

    public PlayQueue()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
    }

    protected override void BindGeneratedControls()
    {
        Bindings?.Update();
    }

    override protected void UnbindGeneratedControls()
    {
        Bindings?.StopTracking();
    }

    private void ScrollToPlayingItem()
    {
        if (PlayingQueueListView == null) return;

        var targetItem = ViewModel.SMTCService.TrackPlayingQueue.ElementAtOrDefault(ViewModel.SMTCService.PlaybackState.PlayQueueIndex);

        if (targetItem == null) return;

        PlayingQueueListView.ScrollIntoView(targetItem);
    }

    private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollToPlayingItem();
    }

    private void PlayingQueueListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
        ViewModel.PlayTrackCommand.Execute(item);
    }

    private void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
        ViewModel.RemoveFromPlayingQueueCommand.Execute(item);
    }

    private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not PlayQueueItem item) return;

        var playlists = ViewModel.AppSettings.StarredPlaylists
            .Where(x => x.FilterProperty == CommonSongProperty.M3UFilePath && x.FilterValue is string)
            .ToList();

        var flyout = new MenuFlyout();
        if (playlists.Count == 0)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = _localizationService.GetLocalizedString("MusicGalleryPageNoPlaylistsAvailable"),
                IsEnabled = false
            });
        }
        else
        {
            foreach (var playlist in playlists)
            {
                var playlistPath = (string)playlist.FilterValue!;
                var menuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name ?? Path.GetFileName(playlistPath)
                };
                menuItem.Click += (_, _) => AddTrackToPlaylist(playlistPath, item.Track);
                flyout.Items.Add(menuItem);
            }
        }

        flyout.ShowAt(element);
    }

    private void AddTrackToPlaylist(string playlistPath, ExtendedTrack track)
    {
        try
        {
            if (!File.Exists(playlistPath)) throw new FileNotFoundException(null, playlistPath);

            var trackPath = M3uPlaylistFile.GetTrackPath(track)
                ?? throw new InvalidOperationException("Track path is not a local file.");

            var entries = M3uPlaylistFile.Read(playlistPath);
            if (!entries.Any(x => string.Equals(x.TrackPath, trackPath, StringComparison.OrdinalIgnoreCase)))
            {
                entries.Add(M3uPlaylistFile.CreateTrackEntry(trackPath));
                M3uPlaylistFile.Write(playlistPath, entries);
            }

            _globalToastProvider.Show("TracksAddToPlaylistSuccessfully", null, MessageSeverity.Success);

            var gallery = Ioc.Default.GetRequiredService<MusicGalleryPageViewModel>();
            if (gallery.SelectedSongsTabInfo?.FilterValue == playlistPath)
            {
                gallery.ApplyPlaylist();
            }
        }
        catch (Exception)
        {
            _globalToastProvider.Show("TracksAddToPlaylistFailed", null, MessageSeverity.Error);
        }
    }

    protected override void OnLoaded()
    {
        ScrollToPlayingItem();
    }

    private void PlayingQueueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ScrollToPlayingItem();
    }

    private void PlayingQueueListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        ViewModel.SMTCService.BeginQueueReorder();
    }

    private void PlayingQueueListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        ViewModel.SMTCService.EndQueueReorder();
    }
}