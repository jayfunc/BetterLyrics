using System.Collections.ObjectModel;
using System.Net;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;

public partial class MusicGalleryPageViewModel
{
    public void CancelRefreshSongs()
    {
    }

    public void RefreshSongs(bool recoverPlaybackPosition = false, bool allowAutoPlay = false)
    {
        _ = _refreshSongsDebouncer.RunAsync(() =>
        {
            _ = Task.Run(async () =>
            {
                var enabledFolderIds = _settingsService.AppSettings.LocalMediaFolders
                    .Where(f => f.IsEnabled)
                    .Select(f => f.Id)
                    .ToList();
                var cachedFiles = await _fileSystemService.GetParsedFilesAsync(enabledFolderIds);
                cachedFiles = cachedFiles.Where(x =>
                    FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName).ToLower())).ToList();

                var newTrackList = cachedFiles
                    .Select(x => new ExtendedTrack(x))
                    .ToList();
                var sourceDict = newTrackList.ToDictionary(s => s.Uri, s => s);

                var playQueue = _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                    {
                        var encodedUri = new Uri(x).AbsoluteUri;
                        if (sourceDict.TryGetValue(encodedUri, out var found)) return new PlayQueueItem(found);

                        return null;
                    })
                    .Where(x => x != null)
                    .ToList();

                _appUIThreadProvider.Execute(async () =>
                {
                    _allTracks = newTrackList;

                    // 更新文件夹树
                    RefreshTreeView();

                    // 应用过滤器
                    ApplyPlaylist();
                    ApplySongSearchQuery();

                    IsLocalMediaNotFound = !_filteredTracks.Any();

                    ApplySongOrderType();

                    await SMTCService.UpdatePlaybackListAsync(playQueue, recoverPlaybackPosition, allowAutoPlay);
                });
            });
        });
    }

    public void ApplyPlaylist()
    {
        CurrentView = MusicLibraryViewType.Songs;
        if (SelectedSongsTabInfo?.FilterValue == string.Empty)
        {
            _middleTracks = _allTracks;
        }
        else if (SelectedSongsTabInfo != null)
        {
            if (SelectedSongsTabInfo.FilterProperty == CommonSongProperty.M3UFilePath)
            {
                if (SelectedSongsTabInfo.FilterValue is string path)
                {
                    if (File.Exists(path))
                    {
                        var m3uFileContent = File.ReadAllText(path);
                        _middleTracks = _allTracks.Where(t => m3uFileContent.Contains(t.Uri.ToDecodedAbsoluteUri())).ToList();
                    }
                    else
                    {
                        _middleTracks = [];
                        _globalToastProvider.Show("PlaylistViewFailed", path, MessageSeverity.Success);
                    }
                }
            }
            else
            {
                _middleTracks = _allTracks.Where(t =>
                {
                    var propValue = GetTrackPropertyValue(t, SelectedSongsTabInfo.FilterProperty);
                    return string.Equals(propValue, SelectedSongsTabInfo.FilterValue?.ToString(), StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }
        }

        ApplySongSearchQuery();
        IsLocalMediaNotFound = !_filteredTracks.Any();
        ApplySongOrderType();
    }

    public void ApplySongSearchQuery()
    {
        if (string.IsNullOrWhiteSpace(SongSearchQuery))
        {
            _filteredTracks = _middleTracks;
            return;
        }

        _filteredTracks = _middleTracks.Where(t =>
            t.Title.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
            t.Artist.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
            t.Album.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
            t.FileName.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
            t.ParentFolderPath.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void ApplySongOrderType()
    {
        GroupedTracks = _filteredTracks.GetGroupedBy(
            t => LanguageHelper.GetOrderChar(GetTrackSortValue(t, SongOrderType)),
            o => GetTrackDisplayValue((ExtendedTrack)o, SongOrderType),
            IsSortDescending
        );

        _sortedTracks = GroupedTracks.SelectMany(x => x.Cast<ExtendedTrack>()).ToList();
        ApplyAlbumsAndArtists();

        if (CurrentView == MusicLibraryViewType.AlbumDetail && SelectedAlbum != null)
        {
            DetailTracks = new ObservableCollection<ExtendedTrack>(_sortedTracks.Where(t => t.Album.Equals(SelectedAlbum.Title, StringComparison.OrdinalIgnoreCase)));
        }
        else if (CurrentView == MusicLibraryViewType.ArtistDetail && SelectedArtist != null)
        {
            DetailTracks = new ObservableCollection<ExtendedTrack>(_sortedTracks.Where(t => t.Artist.Equals(SelectedArtist.Name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private void ApplyAlbumsAndArtists()
    {
        var albumsQuery = _filteredTracks
            .GroupBy(t => t.Album)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new
            {
                Album = new AlbumModel
                {
                    Title = g.Key,
                    LocalAlbumArtPath = g.FirstOrDefault(t => !string.IsNullOrEmpty(t.LocalAlbumArtPath))?.LocalAlbumArtPath ?? g.First().LocalAlbumArtPath,
                    SongCount = g.Count()
                },
                FirstTrack = g.First()
            });

        albumsQuery = albumsQuery.OrderBy(a => a.Album.Title);
        var albumsList = albumsQuery.Select(a => a.Album).ToList();
        Albums = new ObservableCollection<AlbumModel>(albumsList);
        GroupedAlbums = albumsList.GetGroupedBy(
            a => LanguageHelper.GetOrderChar(a.Title),
            o => ((AlbumModel)o).Title
        );

        var artistsQuery = _filteredTracks
            .GroupBy(t => t.Artist)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new
            {
                Artist = new ArtistModel
                {
                    Name = g.Key,
                },
                FirstTrack = g.First()
            });

        artistsQuery = artistsQuery.OrderBy(a => a.Artist.Name);
        var artistsList = artistsQuery.Select(a => a.Artist).ToList();
        Artists = new ObservableCollection<ArtistModel>(artistsList);
        GroupedArtists = artistsList.GetGroupedBy(
            a => LanguageHelper.GetOrderChar(a.Name),
            o => ((ArtistModel)o).Name
        );
    }



    private void RefreshTreeView()
    {
        var roots = FolderTreeBuilder.Build(_allTracks, AppSettings.LocalMediaFolders.ToList());

        FolderRoots.Clear();
        foreach (var r in roots) FolderRoots.Add(r);
    }

    public void SelectFolder(FolderNode? folder)
    {
        if (folder == null) return;
        if (_allTracks == null) return;

        CurrentView = MusicLibraryViewType.Songs;

        var baseUri = folder.FolderPath;
        if (!baseUri.EndsWith("/")) baseUri += "/";
        var decodedBaseUri = WebUtility.UrlDecode(baseUri);

        _middleTracks = _allTracks.Where(track =>
        {
            if (track.MediaFolderId != folder.MediaFolderId) return false;

            var decodedTrackUri = WebUtility.UrlDecode(track.Uri);

            if (!decodedTrackUri.StartsWith(decodedBaseUri, StringComparison.OrdinalIgnoreCase)) return false;

            var relativePart = decodedTrackUri.Substring(decodedBaseUri.Length);

            return !relativePart.Contains('/');
        }).ToList();

        ApplySongSearchQuery();
        IsLocalMediaNotFound = !_filteredTracks.Any();
        ApplySongOrderType();
    }

    public void AddToPlaylists(SongsTabInfo playlist)
    {
        var starredPlaylists = AppSettings.StarredPlaylists;
        var found = starredPlaylists.FirstOrDefault(x =>
            x.FilterProperty == playlist.FilterProperty && x.FilterValue == playlist.FilterValue);
        if (found == null)
        {
            starredPlaylists.Add(playlist);
            SelectedSongsTabInfoIndex = starredPlaylists.Count - 1;
        }
        else
        {
            SelectedSongsTabInfoIndex = starredPlaylists.IndexOf(found);
        }

        ApplyPlaylist();
    }

    partial void OnSongOrderTypeChanged(CommonSongProperty value)
    {
        ApplySongOrderType();
        IsLocalMediaNotFound = !_filteredTracks.Any();
    }

    partial void OnSongSearchQueryChanged(string value)
    {
        ApplySongSearchQuery();
        IsLocalMediaNotFound = !_filteredTracks.Any();
        ApplySongOrderType();
    }

    private void AddFileToStarredPlaylists(string fileName, string filePath)
    {
        AppSettings.StarredPlaylists.Add(new SongsTabInfo
        {
            FilterProperty = CommonSongProperty.M3UFilePath,
            FilterValue = filePath,
            Icon = "\uE7BC",
            Name = fileName
        });
    }

    private string GetTrackPropertyValue(ExtendedTrack track, CommonSongProperty property) => property switch
    {
        CommonSongProperty.Title => track.Title,
        CommonSongProperty.Album => track.Album,
        CommonSongProperty.Artist => track.Artist,
        CommonSongProperty.Folder => track.ParentFolderPath,
        _ => string.Empty
    };

    private string GetTrackSortValue(ExtendedTrack track, CommonSongProperty property) => property switch
    {
        CommonSongProperty.Title => track.Title,
        CommonSongProperty.Artist => track.Artist,
        CommonSongProperty.Album => track.Album,
        CommonSongProperty.Folder => track.ParentFolderName,
        CommonSongProperty.Genre => track.Genre ?? string.Empty,
        CommonSongProperty.Year => track.Year.ToString(),
        CommonSongProperty.TrackNumber => track.TrackNumber.ToString(),
        CommonSongProperty.Bitrate => track.Bitrate.ToString(),
        CommonSongProperty.SampleRate => track.SampleRate.ToString(),
        CommonSongProperty.AudioFormat => track.AudioFormatShortName ?? string.Empty,
        CommonSongProperty.FileSize => track.FileSize.ToString(),
        CommonSongProperty.DateCreated => track.DateCreated?.ToString() ?? string.Empty,
        CommonSongProperty.DateModified => track.DateModified?.ToString() ?? string.Empty,
        CommonSongProperty.Duration => track.Duration.ToString(),
        _ => track.Title
    };

    private object GetTrackDisplayValue(ExtendedTrack track, CommonSongProperty property) => property switch
    {
        CommonSongProperty.Title => track.Title,
        CommonSongProperty.Artist => track.Artist,
        CommonSongProperty.Album => track.Album,
        CommonSongProperty.Folder => track.Album,
        CommonSongProperty.Genre => track.Genre ?? string.Empty,
        CommonSongProperty.Year => track.Year,
        CommonSongProperty.TrackNumber => track.TrackNumber,
        CommonSongProperty.Bitrate => track.Bitrate,
        CommonSongProperty.SampleRate => track.SampleRate,
        CommonSongProperty.AudioFormat => track.AudioFormatShortName ?? string.Empty,
        CommonSongProperty.FileSize => track.FileSize,
        CommonSongProperty.DateCreated => track.DateCreated ?? DateTime.MinValue,
        CommonSongProperty.DateModified => track.DateModified ?? DateTime.MinValue,
        CommonSongProperty.Duration => track.Duration,
        _ => track.Title
    };
}
