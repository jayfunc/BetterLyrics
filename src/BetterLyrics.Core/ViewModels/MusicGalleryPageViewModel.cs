using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.ViewModels;

public partial class MusicGalleryPageViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<DateTime?>>,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<PlaybackOrder>>
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IFilePickerProvider _filePickerProvider;

    private readonly Debouncer _refreshSongsDebouncer = new();
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;

    // All songs
    private List<ExtendedTrack> _allTracks = [];

    // Filtered songs based on search query for current playlist
    private List<ExtendedTrack> _filteredTracks = [];

    // Songs in current playlist or songs in current file tree
    private List<ExtendedTrack> _middleTracks = [];

    // Sorted songs based on filtered songs
    private List<ExtendedTrack> _sortedTracks = [];

    public MusicGalleryPageViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IFileSystemService fileSystemService,
        ISmtcService smtcService, IAppUIThreadProvider appUiThreadProvider,
        IGlobalToastProvider globalToastProvider, IWindowManagerProvider windowManagerProvider,
        IFilePickerProvider filePickerProvider)
    {
        _localizationService = localizationService;
        _fileSystemService = fileSystemService;
        SMTCService = smtcService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;
        _filePickerProvider = filePickerProvider;

        _settingsService = settingsService;
        AppSettings = _settingsService.AppSettings;

        RefreshSongs(true, true);

        _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
        _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;
    }

    [ObservableProperty] public partial ISmtcService SMTCService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial bool IsLocalMediaNotFound { get; set; }

    /// <summary>
    ///     Grouped tracks after filtering and sorting for current playlist
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<GroupInfoList> GroupedTracks { get; set; } = [];

    [ObservableProperty] public partial List<ExtendedTrack> SelectedTracks { get; set; } = [];
    [ObservableProperty] public partial ExtendedTrack? SelectedFirstTrack { get; set; }

    [ObservableProperty] public partial int SelectedTracksTotalDuration { get; set; } = 0;

    [ObservableProperty] public partial CommonSongProperty SongOrderType { get; set; } = CommonSongProperty.Title;

    [ObservableProperty] public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

    public SongsTabInfo? SelectedSongsTabInfo =>
        AppSettings.StarredPlaylists.ElementAtOrDefault(SelectedSongsTabInfoIndex);

    [ObservableProperty] public partial bool IsDataSyncing { get; set; } = false;
    [ObservableProperty] public partial bool IsDataSyncError { get; set; } = false;

    [ObservableProperty] public partial string SongSearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AppListViewSelectionMode SongListViewSelectionMode { get; set; } = AppListViewSelectionMode.Single;

    public ObservableCollection<FolderNode> FolderRoots { get; } = new();

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is MediaFolder)
        {
            if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                RefreshSongs(true);
            else if (message.PropertyName == nameof(MediaFolder.IsProcessing)) IsDataSyncing = message.NewValue;
        }
    }

    public void Receive(PropertyChangedMessage<DateTime?> message)
    {
        if (message.Sender is MediaFolder)
            if (message.PropertyName == nameof(MediaFolder.LastSyncTime))
                RefreshSongs(true);
    }

    public void Receive(PropertyChangedMessage<PlaybackOrder> message)
    {
        if (message.Sender is MusicGallerySettings)
            if (message.PropertyName == nameof(MusicGallerySettings.PlaybackOrder))
                SMTCService.ApplyPlaybackOrder(message.NewValue);
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender is MediaFolder)
            if (message.PropertyName == nameof(MediaFolder.Name))
                RefreshTreeView();
    }

    private void LocalMediaFolders_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
    {
        IsDataSyncError = AppSettings.LocalMediaFolders.Any(x => x.StatusSeverity == MessageSeverity.Error);
    }

    private void LocalMediaFolders_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        RefreshSongs(true);
    }

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
        if (SelectedSongsTabInfo?.FilterValue == string.Empty)
            _middleTracks = _allTracks;
        else
            switch (SelectedSongsTabInfo?.FilterProperty)
            {
                case CommonSongProperty.Title:
                    _middleTracks = _allTracks.Where(t =>
                            t.Title.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    break;
                case CommonSongProperty.Album:
                    _middleTracks = _allTracks.Where(t =>
                            t.Album.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    break;
                case CommonSongProperty.Artist:
                    _middleTracks = _allTracks.Where(t =>
                            t.Artist.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    break;
                case CommonSongProperty.Folder:
                    _middleTracks = _allTracks.Where(t =>
                        t.ParentFolderPath.Equals(SelectedSongsTabInfo.FilterValue,
                            StringComparison.OrdinalIgnoreCase)).ToList();
                    break;
                case CommonSongProperty.M3UFilePath:
                    if (SelectedSongsTabInfo.FilterValue is string path)
                    {
                        if (File.Exists(path))
                        {
                            var m3uFileContent = File.ReadAllText(path);
                            _middleTracks = _allTracks
                                .Where(t => m3uFileContent.Contains(t.Uri.ToDecodedAbsoluteUri())).ToList();
                        }
                        else
                        {
                            _middleTracks = [];
                            _globalToastProvider.Show("PlaylistViewFailed", path, MessageSeverity.Success);
                        }
                    }

                    break;
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
        switch (SongOrderType)
        {
            case CommonSongProperty.Title:
                GroupedTracks = _filteredTracks.GetGroupedBy(
                    t => LanguageHelper.GetOrderChar(t.Title),
                    o => ((ExtendedTrack)o).Title
                );
                break;
            case CommonSongProperty.Artist:
                GroupedTracks = _filteredTracks.GetGroupedBy(
                    t => LanguageHelper.GetOrderChar(t.Artist),
                    o => ((ExtendedTrack)o).Artist
                );
                break;
            case CommonSongProperty.Album:
                GroupedTracks = _filteredTracks.GetGroupedBy(
                    t => LanguageHelper.GetOrderChar(t.Album),
                    o => ((ExtendedTrack)o).Album
                );
                break;
            case CommonSongProperty.Folder:
                GroupedTracks = _filteredTracks.GetGroupedBy(
                    t => LanguageHelper.GetOrderChar(t.ParentFolderName),
                    o => ((ExtendedTrack)o).Album
                );
                break;
        }

        _sortedTracks = GroupedTracks.SelectMany(x => x.Cast<ExtendedTrack>()).ToList();
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

    [RelayCommand]
    private async Task ShuffleAsync()
    {
        AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.Shuffle;

        var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
        await SMTCService.UpdatePlaybackListAsync(playQueue);

        var queueCount = playQueue.Count();
        var startIndex = queueCount > 0 ? Random.Shared.Next(0, queueCount) : -1;

        SMTCService.PlayTrackAt(startIndex);
    }

    [RelayCommand]
    private async Task RepeatAllAsync()
    {
        AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.RepeatAll;

        var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
        await SMTCService.UpdatePlaybackListAsync(playQueue);

        SMTCService.PlayTrackAt(0);
    }

    [RelayCommand]
    private async Task PlayAsync(ExtendedTrack invokedTrack)
    {
        var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
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
            new Dictionary<string, IList<string>>
            {
                { "M3U", [".m3u"] }
            }, null, WindowType.MusicGalleryWindow);

        if (fileName != null && filePath != null)
        {
            AddFileToStarredPlaylists(fileName, filePath);
            _globalToastProvider.Show("CreatePlaylistSuccessfully", filePath, MessageSeverity.Success);
        }
    }

    [RelayCommand]
    private async Task ImportPlaylistAsync()
    {
        var (fileName, filePath) =
            await _filePickerProvider.PickSingleFileAsync([".m3u"], WindowType.MusicGalleryWindow);

        if (fileName != null && filePath != null)
        {
            AddFileToStarredPlaylists(fileName, filePath);
            _globalToastProvider.Show("ImportPlaylistSuccessfully", filePath, MessageSeverity.Success);
        }
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
}