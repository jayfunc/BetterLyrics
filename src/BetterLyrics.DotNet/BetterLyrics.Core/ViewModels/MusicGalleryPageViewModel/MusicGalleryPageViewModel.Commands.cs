using System.Collections.ObjectModel;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;

public partial class MusicGalleryPageViewModel
{
    [RelayCommand]
    private async Task ShuffleAsync()
    {
        AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.Shuffle;

        var playQueue = GetTargetTracksForPlayback().Select(x => new PlayQueueItem(x)).ToList();
        await SMTCService.UpdatePlaybackListAsync(playQueue);

        var queueCount = playQueue.Count;
        var startIndex = queueCount > 0 ? Random.Shared.Next(0, queueCount) : -1;

        SMTCService.PlayTrackAt(startIndex);
    }

    [RelayCommand]
    private async Task RepeatAllAsync()
    {
        AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.RepeatAll;

        var playQueue = GetTargetTracksForPlayback().Select(x => new PlayQueueItem(x));
        await SMTCService.UpdatePlaybackListAsync(playQueue);

        SMTCService.PlayTrackAt(0);
    }

    [RelayCommand]
    private async Task PlayAsync(ExtendedTrack invokedTrack)
    {
        var playQueue = GetTargetTracksForPlayback().Select(x => new PlayQueueItem(x));
        await SMTCService.UpdatePlaybackListAsync(playQueue);

        var target = SMTCService.TrackPlayingQueue.FirstOrDefault(x => x.Track == invokedTrack);
        if (target != null)
        {
            var index = SMTCService.TrackPlayingQueue.IndexOf(target);
            if (index != -1) SMTCService.PlayTrackAt(index);
        }
    }

    [RelayCommand]
    private async Task CreatePlaylistAsync()
    {
        var (fileName, filePath) = await _filePickerProvider.PickSaveFileAsync(
            new Dictionary<string, IList<string>> { { "M3U", [".m3u"] } }, null, WindowType.MusicGalleryWindow);

        HandlePlaylistResult(fileName, filePath, "CreatePlaylistSuccessfully");
    }

    [RelayCommand]
    private async Task ImportPlaylistAsync()
    {
        var (fileName, filePath) = await _filePickerProvider.PickSingleFileAsync([".m3u"], WindowType.MusicGalleryWindow);

        HandlePlaylistResult(fileName, filePath, "ImportPlaylistSuccessfully");
    }

    [RelayCommand]
    private void StopTrack()
    {
        SMTCService.PlayTrackAt(-1);
    }

    [RelayCommand]
    private void OpenMediaSettings()
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.SettingsWindow);
        var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        settingsPageViewModel.NavigateToSection(SettingsSection.MediaLib);
    }

    [RelayCommand]
    private void ToggleSongListViewSelectionMode()
    {
        SongListViewSelectionMode =
            SongListViewSelectionMode == AppListViewSelectionMode.Single
                ? AppListViewSelectionMode.Multiple
                : AppListViewSelectionMode.Single;
    }

    [RelayCommand]
    private void SelectAlbum(AlbumModel album)
    {
        var completeAlbum = Albums.FirstOrDefault(a => a.Title.Equals(album.Title, StringComparison.OrdinalIgnoreCase));
        if (completeAlbum == null) 
        {
            var tracks = _sortedTracks.Where(t => t.Album.Equals(album.Title, StringComparison.OrdinalIgnoreCase)).ToList();
            completeAlbum = new AlbumModel 
            {
                Title = album.Title,
                LocalAlbumArtPath = tracks.FirstOrDefault(t => !string.IsNullOrEmpty(t.LocalAlbumArtPath))?.LocalAlbumArtPath,
                SongCount = tracks.Count
            };
        }
        SelectedAlbum = completeAlbum;
        DetailTracks = new ObservableCollection<ExtendedTrack>(_sortedTracks.Where(t => t.Album.Equals(completeAlbum.Title, StringComparison.OrdinalIgnoreCase)));
        CurrentView = MusicLibraryViewType.AlbumDetail;
    }

    [RelayCommand]
    private void SelectArtist(ArtistModel artist)
    {
        var completeArtist = Artists.FirstOrDefault(a => a.Name.Equals(artist.Name, StringComparison.OrdinalIgnoreCase));
        if (completeArtist == null)
        {
            var tracks = _sortedTracks.Where(t => t.Artist.Equals(artist.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            completeArtist = new ArtistModel 
            {
                Name = artist.Name,
            };
        }
        SelectedArtist = completeArtist;
        DetailTracks = new ObservableCollection<ExtendedTrack>(_sortedTracks.Where(t => t.Artist.Equals(completeArtist.Name, StringComparison.OrdinalIgnoreCase)));
        CurrentView = MusicLibraryViewType.ArtistDetail;
    }
    private List<ExtendedTrack> GetTargetTracksForPlayback()
    {
        return CurrentView is MusicLibraryViewType.AlbumDetail or MusicLibraryViewType.ArtistDetail
            ? DetailTracks.ToList()
            : _sortedTracks.ToList();
    }

    private void HandlePlaylistResult(string? fileName, string? filePath, string successMessageKey)
    {
        if (fileName != null && filePath != null)
        {
            AddFileToStarredPlaylists(fileName, filePath);
            _globalToastProvider.Show(successMessageKey, filePath, MessageSeverity.Success);
        }
    }

    [RelayCommand]
    private void SetSongOrderType(CommonSongProperty property)
    {
        if (SongOrderType == property)
        {
            IsSortDescending = !IsSortDescending;
        }
        else
        {
            IsSortDescending = false;
            SongOrderType = property;
        }
        
        ApplySongOrderType();
    }
}
