using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;

/// <summary>
/// Singleton ViewModel
/// </summary>
public partial class MusicGalleryPageViewModel : ObservableRecipient, ISingletonViewModel,
    IRecipient<MediaFolderLastSyncTimeChangedMessage>,
    IRecipient<MediaFolderEnabledChangedMessage>,
    IRecipient<MediaFolderProcessingChangedMessage>,
    IRecipient<MediaFolderNameChangedMessage>,
    IRecipient<PlaybackOrderChangedMessage>
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IFilePickerProvider _filePickerProvider;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IPlaybackStateService _playbackStateService;
    private readonly IGsmtcService _gsmtcService;
    private bool _shouldAutoPlayOnSongsLoaded;

    private readonly Debouncer _refreshSongsDebouncer = new();

    // All songs
    private List<ExtendedTrack> _allTracks = [];

    // Filtered songs based on search query for current playlist
    private List<ExtendedTrack> _filteredTracks = [];
    public List<ExtendedTrack> FilteredTracks => _filteredTracks;

    // Songs in current playlist or songs in current file tree
    private List<ExtendedTrack> _middleTracks = [];

    // Sorted songs based on filtered songs
    private List<ExtendedTrack> _sortedTracks = [];

    // Parsed entries of the m3u playlist that is currently displayed
    private List<M3uPlaylistEntry>? _m3uEntries;
    private string? _m3uPath;

    // Last column sort, restored when leaving the file-order mode
    private CommonSongProperty _lastSongOrderType = CommonSongProperty.Title;
    private bool _lastIsSortDescending;

    [ObservableProperty] public partial ISmtcService SMTCService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial bool IsLocalMediaNotFound { get; set; }

    /// <summary>
    /// Grouped tracks after filtering and sorting for current playlist
    /// </summary>
    [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedTracks { get; set; } = [];

    /// <summary>
    /// Flat track list shown while a local playlist keeps its original order. Reordering this list is persisted to the m3u file.
    /// </summary>
    [ObservableProperty] public partial ObservableCollection<ExtendedTrack> PlaylistOrderTracks { get; set; } = [];

    [ObservableProperty] public partial bool IsPlaylistOrderMode { get; set; }

    [ObservableProperty] public partial int FilteredTracksCount { get; set; }

    public bool IsPlaylistReorderEnabled => IsPlaylistOrderMode
        && CurrentView == MusicLibraryViewType.Songs
        && string.IsNullOrWhiteSpace(SongSearchQuery)
        && SongListViewSelectionMode == AppListViewSelectionMode.Single;

    public bool IsLocalPlaylistSelected => CurrentView == MusicLibraryViewType.Songs
        && SelectedSongsTabInfo?.FilterProperty == CommonSongProperty.M3UFilePath;

    [ObservableProperty] public partial List<ExtendedTrack> SelectedTracks { get; set; } = [];
    [ObservableProperty] public partial ExtendedTrack? SelectedFirstTrack { get; set; }

    [ObservableProperty] public partial int SelectedTracksTotalDuration { get; set; } = 0;

    [ObservableProperty] public partial CommonSongProperty SongOrderType { get; set; } = CommonSongProperty.Title;
    [ObservableProperty] public partial bool IsSortDescending { get; set; } = false;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsLocalPlaylistSelected))] public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

    public SongsTabInfo? SelectedSongsTabInfo => AppSettings.StarredPlaylists.ElementAtOrDefault(SelectedSongsTabInfoIndex);

    [ObservableProperty] public partial bool IsDataSyncing { get; set; } = false;
    [ObservableProperty] public partial bool IsDataSyncError { get; set; } = false;

    [ObservableProperty] public partial string SongSearchQuery { get; set; } = string.Empty;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsMainView))][NotifyPropertyChangedFor(nameof(IsPlaylistReorderEnabled))][NotifyPropertyChangedFor(nameof(IsLocalPlaylistSelected))] public partial MusicLibraryViewType CurrentView { get; set; } = MusicLibraryViewType.Songs;

    public bool IsMainView => CurrentView == MusicLibraryViewType.Songs || CurrentView == MusicLibraryViewType.Albums || CurrentView == MusicLibraryViewType.Artists;

    [ObservableProperty] public partial ObservableCollection<AlbumModel> Albums { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedAlbums { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<ArtistModel> Artists { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedArtists { get; set; } = [];

    [ObservableProperty] public partial AlbumModel? SelectedAlbum { get; set; }
    [ObservableProperty] public partial ArtistModel? SelectedArtist { get; set; }

    [ObservableProperty] public partial ObservableCollection<ExtendedTrack> DetailTracks { get; set; } = [];

    [ObservableProperty] public partial AppListViewSelectionMode SongListViewSelectionMode { get; set; } = AppListViewSelectionMode.Single;

    public ObservableCollection<FolderNode> FolderRoots { get; } = new();

    public MusicGalleryPageViewModel(
    ISettingsService settingsService,
    ILocalizationService localizationService,
    IFileSystemService fileSystemService,
    ISmtcService smtcService, IAppUIThreadProvider appUiThreadProvider,
    IGlobalToastProvider globalToastProvider, IWindowManagerProvider windowManagerProvider,
    IFilePickerProvider filePickerProvider, IPlaybackStateService playbackStateService,
    IGsmtcService gsmtcService)
    {
        _localizationService = localizationService;
        _fileSystemService = fileSystemService;
        SMTCService = smtcService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;
        _filePickerProvider = filePickerProvider;
        _playbackStateService = playbackStateService;
        _gsmtcService = gsmtcService;

        _settingsService = settingsService;
        AppSettings = _settingsService.AppSettings;

        RefreshSongs(true, AppSettings.MusicGallerySettings.AutoPlay);

        _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
        _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;

        IsActive = true;
    }

    public void HandleAutoPlay()
    {
        if (!AppSettings.MusicGallerySettings.AutoPlay)
        {
            return;
        }

        if (_gsmtcService.IsExternalSourceActive)
        {
            return;
        }

        if (SMTCService.IsPlaying)
        {
            return;
        }

        if (SMTCService.PlayingTrack != null)
        {
            SMTCService.Play();
            return;
        }

        var savedIndex = _playbackStateService.CurrentState.PlayQueueIndex;
        if (SMTCService.TrackPlayingQueue.Count > 0 && savedIndex >= 0 && savedIndex < SMTCService.TrackPlayingQueue.Count)
        {
            SMTCService.PlayTrackAt(savedIndex, recoverPlaybackPosition: true);
            return;
        }

        _shouldAutoPlayOnSongsLoaded = true;
    }

    public void PausePlayback()
    {
        SMTCService.Pause();
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

    public void Receive(MediaFolderEnabledChangedMessage message)
    {
        RefreshSongs(true);
    }

    public void Receive(MediaFolderProcessingChangedMessage message)
    {
        IsDataSyncing = message.IsProcessing;
    }

    public void Receive(MediaFolderLastSyncTimeChangedMessage message)
    {
        RefreshSongs(true);
    }

    public void Receive(PlaybackOrderChangedMessage message)
    {
        SMTCService.ApplyPlaybackOrder(message.Value);
    }

    public void Receive(MediaFolderNameChangedMessage message)
    {
        RefreshTreeView();
    }
}