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

namespace BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;

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
    public List<ExtendedTrack> FilteredTracks => _filteredTracks;

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
    [ObservableProperty] public partial bool IsSortDescending { get; set; } = false;

    [ObservableProperty] public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

    public SongsTabInfo? SelectedSongsTabInfo =>
        AppSettings.StarredPlaylists.ElementAtOrDefault(SelectedSongsTabInfoIndex);

    [ObservableProperty] public partial bool IsDataSyncing { get; set; } = false;
    [ObservableProperty] public partial bool IsDataSyncError { get; set; } = false;

    [ObservableProperty] public partial string SongSearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainView))]
    public partial MusicLibraryViewType CurrentView { get; set; } = MusicLibraryViewType.Songs;

    public bool IsMainView => CurrentView == MusicLibraryViewType.Songs || CurrentView == MusicLibraryViewType.Albums || CurrentView == MusicLibraryViewType.Artists;

    [ObservableProperty] public partial ObservableCollection<AlbumModel> Albums { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedAlbums { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<ArtistModel> Artists { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedArtists { get; set; } = [];

    [ObservableProperty] public partial AlbumModel? SelectedAlbum { get; set; }
    [ObservableProperty] public partial ArtistModel? SelectedArtist { get; set; }

    [ObservableProperty] public partial ObservableCollection<ExtendedTrack> DetailTracks { get; set; } = [];

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

}