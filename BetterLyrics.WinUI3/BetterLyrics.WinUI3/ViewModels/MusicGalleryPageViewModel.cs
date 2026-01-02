using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MusicGalleryPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<DateTime?>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IFileSystemService _fileSystemService;

        private readonly MediaPlayer _mediaPlayer = new();
        private readonly MediaTimelineController _timelineController = new();
        private readonly SystemMediaTransportControls _smtc;

        private readonly DispatcherQueueTimer _refreshSongsTimer;

        private IRandomAccessStream? _currentStream;
        private Stream? _currentNetStream;
        private IUnifiedFileSystem? _currentProvider;

        // All songs
        private List<ExtendedTrack> _allTracks = [];
        // Songs in current playlist or songs in current file tree
        private List<ExtendedTrack> _middleTracks = [];
        // Filtered songs based on search query for current playlist
        private List<ExtendedTrack> _filteredTracks = [];

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial bool IsLocalMediaNotFound { get; set; }

        /// <summary>
        /// Grouped tracks after filtering and sorting for current playlist
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<GroupInfoList> GroupedTracks { get; set; } = [];

        [ObservableProperty]
        public partial List<ExtendedTrack> SelectedTracks { get; set; } = [];

        [ObservableProperty]
        public partial int SelectedTracksTotalDuration { get; set; } = 0;

        [ObservableProperty]
        public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; }

        public PlayQueueItem? PlayingQueueItem => TrackPlayingQueue.ElementAtOrDefault(AppSettings.MusicGallerySettings.PlayQueueIndex);

        [ObservableProperty]
        public partial ExtendedTrack? PlayingTrack { get; set; } = null;

        [ObservableProperty]
        public partial CommonSongProperty SongOrderType { get; set; } = CommonSongProperty.Title;

        [ObservableProperty]
        public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

        public SongsTabInfo? SelectedSongsTabInfo => AppSettings.StarredPlaylists.ElementAtOrDefault(SelectedSongsTabInfoIndex);

        [ObservableProperty] public partial bool IsDataSyncing { get; set; } = false;
        [ObservableProperty] public partial bool IsDataSyncError { get; set; } = false;

        [ObservableProperty] public partial ExtendedTrack TrackRightTapped { get; set; } = new();

        [ObservableProperty]
        public partial string SongSearchQuery { get; set; } = string.Empty;

        public ObservableCollection<FolderNode> FolderRoots { get; } = new();

        public MusicGalleryPageViewModel(
            ISettingsService settingsService,
            ILocalizationService localizationService,
            IFileSystemService fileSystemService
        )
        {
            _localizationService = localizationService;
            _fileSystemService = fileSystemService;

            _refreshSongsTimer = _dispatcherQueue.CreateTimer();

            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;

            TrackPlayingQueue = [.. AppSettings.MusicGallerySettings.PlayQueuePaths.Select(x => new PlayQueueItem(new ExtendedTrack(x)))];
            TrackPlayingQueue.CollectionChanged += TrackPlayingQueue_CollectionChanged;

            RefreshSongs();

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
            _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;

            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _mediaPlayer.CommandManager.IsEnabled = false;

            _timelineController = _mediaPlayer.TimelineController = new();
            _timelineController.PositionChanged += TimelineController_PositionChanged;

            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.ButtonPressed += Smtc_ButtonPressed;
            _smtc.PlaybackPositionChangeRequested += Smtc_PlaybackPositionChangeRequested;
        }

        private void LocalMediaFolders_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            IsDataSyncError = AppSettings.LocalMediaFolders.Any(x => x.StatusSeverity == InfoBarSeverity.Error);
        }

        private void TrackPlayingQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AppSettings.MusicGallerySettings.PlayQueuePaths = [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];
        }

        private void LocalMediaFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshSongs();
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            PlayNextTrack();
        }

        public void PlayNextTrack()
        {
            switch (AppSettings.MusicGallerySettings.PlaybackOrder)
            {
                case PlaybackOrder.RepeatAll:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                    {
                        if (AppSettings.MusicGallerySettings.PlayQueueIndex < TrackPlayingQueue.Count - 1)
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex++;
                        }
                        else
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex = 0;
                        }
                        await PlayTrackAsync(PlayingQueueItem);
                    });
                    break;
                case PlaybackOrder.RepeatOne:
                    _timelineController.Position = TimeSpan.Zero;
                    break;
                case PlaybackOrder.Shuffle:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                    {
                        if (TrackPlayingQueue.Count > 0)
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        await PlayTrackAsync(PlayingQueueItem);
                    });
                    break;
                default:
                    break;
            }
        }

        private void PlayPreviousTrack()
        {
            switch (AppSettings.MusicGallerySettings.PlaybackOrder)
            {
                case PlaybackOrder.RepeatAll:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                    {
                        if (AppSettings.MusicGallerySettings.PlayQueueIndex > 0)
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex--;
                        }
                        else
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex = TrackPlayingQueue.Count - 1;
                        }
                        await PlayTrackAsync(PlayingQueueItem);
                    });
                    break;
                case PlaybackOrder.RepeatOne:
                    _timelineController.Position = TimeSpan.Zero;
                    break;
                case PlaybackOrder.Shuffle:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                    {
                        if (TrackPlayingQueue.Count > 0)
                        {
                            AppSettings.MusicGallerySettings.PlayQueueIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        await PlayTrackAsync(PlayingQueueItem);
                    });
                    break;
                default:
                    break;
            }
        }

        private void Smtc_PlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            _timelineController.Position = args.RequestedPlaybackPosition;
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            _timelineController.Start();
            _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
        }

        private void TimelineController_PositionChanged(MediaTimelineController sender, object args)
        {
            _smtc.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties()
            {
                Position = sender.Position,
                EndTime = _mediaPlayer.PlaybackSession.NaturalDuration
            });
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    _timelineController.Resume();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                    _timelineController.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    PlayNextTrack();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    PlayPreviousTrack();
                    break;
            }
        }

        public void CancelRefreshSongs()
        {
        }

        public void RefreshSongs()
        {
            _refreshSongsTimer.Debounce(() =>
            {
                _ = Task.Run(async () =>
                {
                    var enabledFolderIds = _settingsService.AppSettings.LocalMediaFolders
                        .Where(f => f.IsEnabled)
                        .Select(f => f.Id)
                        .ToList();

                    var cachedFiles = await _fileSystemService.GetParsedFilesAsync(enabledFolderIds);
                    cachedFiles = cachedFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

                    var newTrackList = cachedFiles
                        .Select(x => new ExtendedTrack(x))
                        .ToList();

                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        _allTracks = newTrackList;

                        // 更新文件夹树
                        RefreshTreeView();

                        // 应用过滤器
                        ApplyPlaylist();
                        ApplySongSearchQuery();

                        IsLocalMediaNotFound = !_filteredTracks.Any();

                        ApplySongOrderType();
                    });
                });
            }, Time.DebounceTimeout);
        }

        public void ApplyPlaylist()
        {
            if (SelectedSongsTabInfo?.FilterValue == string.Empty)
            {
                _middleTracks = _allTracks;
            }
            else
            {
                switch (SelectedSongsTabInfo?.FilterProperty)
                {
                    case CommonSongProperty.Title:
                        _middleTracks = _allTracks.Where(t => t.Title.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Album:
                        _middleTracks = _allTracks.Where(t => t.Album.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Artist:
                        _middleTracks = _allTracks.Where(t => t.Artist.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Folder:
                        _middleTracks = _allTracks.Where(t => t.ParentFolderPath.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.M3UFilePath:
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
                                ToastHelper.ShowToast("PlaylistViewFailed", path, InfoBarSeverity.Success);
                            }
                        }
                        break;
                    default:
                        break;
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
                    // 文件名（包含后缀）
                    t.FileName.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    // 文件所在文件夹的路径
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

            string baseUri = folder.FolderPath;
            if (!baseUri.EndsWith("/")) baseUri += "/";

            _middleTracks = _allTracks.Where(track =>
            {
                if (track.MediaFolderId != folder.MediaFolderId) return false;

                string trackUriDecoded = System.Net.WebUtility.UrlDecode(track.Uri);

                if (!trackUriDecoded.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)) return false;

                string relativePart = trackUriDecoded.Substring(baseUri.Length);

                return !relativePart.Contains('/');
            }).ToList();

            ApplySongSearchQuery();
            IsLocalMediaNotFound = !_filteredTracks.Any();
            ApplySongOrderType();
        }

        public void AddToPlaylists(SongsTabInfo playlist)
        {
            var starredPlaylists = AppSettings.StarredPlaylists;
            var found = starredPlaylists.FirstOrDefault(x => x.FilterProperty == playlist.FilterProperty && x.FilterValue == playlist.FilterValue);
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

        public async Task PlayTrackAtAsync(int index)
        {
            await PlayTrackAsync(TrackPlayingQueue.ElementAtOrDefault(index));
        }

        public async Task PlayTrackAsync(PlayQueueItem? playQueueItem)
        {
            _timelineController.Pause();
            _mediaPlayer.Source = null;

            // 清理旧资源
            _currentStream?.Dispose();
            _currentNetStream?.Dispose();
            _currentStream = null;
            _currentNetStream = null;

            if (playQueueItem == null)
            {
                _smtc.IsEnabled = false;
                _smtc.DisplayUpdater.ClearAll();
            }
            else
            {
                PlayingTrack = playQueueItem.Track;
                _smtc.IsEnabled = true;

                try
                {
                    var targetFolder = _settingsService.AppSettings.LocalMediaFolders.FirstOrDefault(f =>
                    {
                        var fUri = f.GetStandardUri().AbsoluteUri;
                        return PlayingTrack.Uri.StartsWith(fUri, StringComparison.OrdinalIgnoreCase);
                    });

                    if (targetFolder == null)
                    {
                        throw new FileNotFoundException(null, PlayingTrack.Uri.ToDecodedAbsoluteUri());
                    }

                    _currentProvider = targetFolder.CreateFileSystem();
                    if (_currentProvider == null) return;

                    await _currentProvider.ConnectAsync();

                    var fileCacheStub = new FilesIndexItem
                    {
                        Uri = PlayingTrack.Uri
                    };

                    var sourceStream = await _fileSystemService.OpenFileAsync(_currentProvider, fileCacheStub);

                    if (sourceStream == null)
                    {
                        throw new FileNotFoundException(null, fileCacheStub.Uri);
                    }

                    if (sourceStream.CanSeek)
                    {
                        _currentNetStream = sourceStream;
                    }
                    else
                    {
                        var memStream = new MemoryStream();

                        await sourceStream.CopyToAsync(memStream);
                        memStream.Position = 0;

                        sourceStream.Dispose();

                        _currentNetStream = memStream;
                    }

                    _currentStream = _currentNetStream.AsRandomAccessStream();

                    string contentType = GetMimeType(PlayingTrack.FileName);
                    var mediaSource = MediaSource.CreateFromStream(_currentStream, contentType);

                    _mediaPlayer.Source = mediaSource;

                    var updater = _smtc.DisplayUpdater;
                    updater.Type = MediaPlaybackType.Music;

                    updater.MusicProperties.Title = PlayingTrack.Title ?? PlayingTrack.FileName;
                    updater.MusicProperties.Artist = PlayingTrack.Artist ?? "";
                    updater.MusicProperties.AlbumTitle = PlayingTrack.Album ?? "";

                    updater.MusicProperties.Genres.Clear();
                    updater.MusicProperties.Genres.Add($"{ExtendedGenreFiled.FileName}{Path.GetFileNameWithoutExtension(PlayingTrack.FileName)}");

                    updater.AppMediaId = Package.Current.Id.FullName;

                    if (!string.IsNullOrEmpty(PlayingTrack.LocalAlbumArtPath) && File.Exists(PlayingTrack.LocalAlbumArtPath))
                    {
                        var storageFile = await StorageFile.GetFileFromPathAsync(PlayingTrack.LocalAlbumArtPath);
                        updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);
                    }
                    else
                    {
                        updater.Thumbnail = null;
                    }

                    updater.Update();
                }
                catch (Exception ex)
                {
                    ToastHelper.ShowToast("Error", ex.Message, InfoBarSeverity.Error);
                    _timelineController.Pause();
                }
            }
        }

        private string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".mp3" => "audio/mpeg",
                ".flac" => "audio/flac",
                ".wav" => "audio/wav",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".ogg" => "audio/ogg",
                ".wma" => "audio/x-ms-wma",
                _ => "application/octet-stream"
            };
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

        private void AddFileToStarredPlaylists(StorageFile file)
        {
            AppSettings.StarredPlaylists.Add(new SongsTabInfo
            {
                FilterProperty = CommonSongProperty.M3UFilePath,
                FilterValue = file.Path,
                Icon = "\uE7BC",
                Name = file.Name
            });
        }

        [RelayCommand]
        private async Task CreatePlaylistAsync()
        {
            var file = await PickerHelper.PickSaveFileAsync<MusicGalleryWindow>(new Dictionary<string, IList<string>>()
            {
                { "M3U", [".m3u"] }
            });

            if (file != null)
            {
                AddFileToStarredPlaylists(file);
                ToastHelper.ShowToast("CreatePlaylistSuccessfully", file.Path, InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private async Task ImportPlaylistAsync()
        {
            var file = await PickerHelper.PickSingleFileAsync<MusicGalleryWindow>([".m3u"]);

            if (file != null)
            {
                AddFileToStarredPlaylists(file);
                ToastHelper.ShowToast("ImportPlaylistSuccessfully", file.Path, InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private async Task StopTrackAsync()
        {
            await PlayTrackAtAsync(-1);
        }

        [RelayCommand]
        private void OpenMediaSettings()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
            var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
            settingsPageViewModel.NavViewSelectedItemTag = "MediaLib";
        }

        public void Receive(PropertyChangedMessage<DateTime?> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.LastSyncTime))
                {
                    RefreshSongs();
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                {
                    RefreshSongs();
                }
                else if (message.PropertyName == nameof(MediaFolder.IsProcessing))
                {
                    IsDataSyncing = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.Name))
                {
                    RefreshTreeView();
                }
            }
        }

    }
}
