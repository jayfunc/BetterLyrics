using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MusicGalleryPageViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private readonly MediaPlayer _mediaPlayer = new();
        private readonly MediaTimelineController _timelineController = new();
        private readonly SystemMediaTransportControls _smtc;

        private readonly DispatcherQueueTimer _refreshSongsTimer;

        // All songs
        private List<ExtendedTrack> _tracks = [];
        // Songs in current playlist
        private List<ExtendedTrack> _playlistTracks = [];
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
        public partial ObservableCollection<SongsTabInfo> SongsTabInfoList { get; set; } = [];

        [ObservableProperty]
        public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

        public SongsTabInfo? SelectedSongsTabInfo => SongsTabInfoList.ElementAtOrDefault(SelectedSongsTabInfoIndex);

        [ObservableProperty]
        public partial bool IsDataLoading { get; set; } = false;

        [ObservableProperty]
        public partial ExtendedTrack TrackRightTapped { get; set; } = new();

        [ObservableProperty]
        public partial string SongSearchQuery { get; set; } = string.Empty;

        public MusicGalleryPageViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService, ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _refreshSongsTimer = _dispatcherQueue.CreateTimer();

            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;

            TrackPlayingQueue = [.. AppSettings.MusicGallerySettings.PlayQueuePaths.Select(x => new PlayQueueItem(new ExtendedTrack(x)))];
            TrackPlayingQueue.CollectionChanged += TrackPlayingQueue_CollectionChanged;

            SongsTabInfoList.Add(new SongsTabInfo(_localizationService.GetLocalizedString("MusicGalleryPageAllSongs"), "\uE8A9", false, false, CommonSongProperty.Title, string.Empty));

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

            _libWatcherService = libWatcherService;
            _libWatcherService.MusicLibraryFilesChanged += LibWatcherService_MusicLibraryFilesChanged;
        }

        private void TrackPlayingQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AppSettings.MusicGallerySettings.PlayQueuePaths = [.. TrackPlayingQueue.Select(x => x.Track.Path)];
        }

        private void LocalMediaFolders_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            RefreshSongs();
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

        private void LibWatcherService_MusicLibraryFilesChanged(object? sender, Events.LibChangedEventArgs e)
        {
            RefreshSongs();
        }

        public void CancelRefreshSongs()
        {
        }

        public void RefreshSongs()
        {
            _refreshSongsTimer.Debounce(() =>
            {
                IsDataLoading = true;
                _tracks.Clear();

                Task.Run(async () =>
                {
                    try
                    {
                        foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
                        {
                            if (!folder.IsEnabled) continue;

                            try
                            {
                                using var fs = folder.CreateFileSystem();

                                if (fs == null) continue;

                                if (!await fs.ConnectAsync()) continue;

                                // 递归扫描队列
                                var foldersToScan = new Queue<string>();
                                foldersToScan.Enqueue(""); // 从根目录开始

                                while (foldersToScan.Count > 0)
                                {
                                    var currentPath = foldersToScan.Dequeue();
                                    var items = await fs.GetFilesAsync(currentPath);

                                    foreach (var item in items)
                                    {
                                        if (item.IsFolder)
                                        {
                                            foldersToScan.Enqueue(Path.Combine(currentPath, item.Name));
                                            continue;
                                        }

                                        var ext = Path.GetExtension(item.Name).ToLower();
                                        if (FileHelper.MusicExtensions.Contains(ext))
                                        {
                                            try
                                            {
                                                using (var stream = await fs.OpenReadAsync(item.FullPath))
                                                {
                                                    ExtendedTrack track = new ExtendedTrack(item.FullPath, stream);

                                                    if (track.Duration > 0)
                                                    {
                                                        // 读取专辑图写入内存
                                                        // 因为此后该流将关闭无法再次访问
                                                        _ = track.EmbeddedPictures;

                                                        _tracks.Add(track);
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Error loading track {item.Name}: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Folder scan error ({folder.Name}): {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Global scan error: {ex.Message}");
                    }

                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        ApplyPlaylist();
                        ApplySongSearchQuery();
                        IsLocalMediaNotFound = !_filteredTracks.Any();
                        ApplySongOrderType();
                        IsDataLoading = false;
                    });
                });
            }, Constants.Time.DebounceTimeout);
        }

        public void ApplyPlaylist()
        {
            if (SelectedSongsTabInfo?.FilterValue == string.Empty)
            {
                _playlistTracks = _tracks;
            }
            else
            {
                switch (SelectedSongsTabInfo?.FilterProperty)
                {
                    case CommonSongProperty.Title:
                        _playlistTracks = _tracks.Where(t => t.Title.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Album:
                        _playlistTracks = _tracks.Where(t => t.Album.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Artist:
                        _playlistTracks = _tracks.Where(t => t.Artist.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Folder:
                        _playlistTracks = _tracks.Where(t => t.ParentFolderPath.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.M3UFilePath:
                        if (SelectedSongsTabInfo.FilterValue is string path)
                        {
                            if (File.Exists(path))
                            {
                                var m3uFileContent = File.ReadAllText(path);
                                _playlistTracks = _tracks.Where(t => m3uFileContent.Contains(t.Path)).ToList();
                            }
                            else
                            {
                                _playlistTracks = [];
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
                _filteredTracks = _playlistTracks;
                return;
            }
            _filteredTracks = _playlistTracks.Where(t =>
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

        public void UpdateSelectedPlaylist(SongsTabInfo playlist)
        {
            var found = SongsTabInfoList.FirstOrDefault(x => x.FilterProperty == playlist.FilterProperty && x.FilterValue == playlist.FilterValue);
            if (found == null)
            {
                SongsTabInfoList.Add(playlist);
                SelectedSongsTabInfoIndex = SongsTabInfoList.Count - 1;
            }
            else
            {
                SelectedSongsTabInfoIndex = SongsTabInfoList.IndexOf(found);
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
            if (playQueueItem == null)
            {
                _smtc.IsEnabled = false;
            }
            else
            {
                PlayingTrack = playQueueItem.Track;

                var updater = _smtc.DisplayUpdater;
                updater.ClearAll();

                _smtc.IsEnabled = true;
                _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(PlayingTrack.Path));

                var storageFile = await StorageFile.GetFileFromPathAsync(PlayingTrack.Path);

                await updater.CopyFromFileAsync(MediaPlaybackType.Music, storageFile);
                updater.AppMediaId = Package.Current.Id.FullName;
                updater.MusicProperties.AlbumTitle = PlayingTrack.Album;
                updater.MusicProperties.Genres.Add($"{ExtendedGenreFiled.FileName}{Path.GetFileNameWithoutExtension(PlayingTrack.Path)}");
                updater.Update();
            }
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
                IsStarred = true,
                IsClosable = true,
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
        private void SwitchPlaybackOrder()
        {
            AppSettings.MusicGallerySettings.PlaybackOrder = AppSettings.MusicGallerySettings.PlaybackOrder.GetNext();
        }

        [RelayCommand]
        private async Task StopTrackAsync()
        {
            await PlayTrackAtAsync(-1);
        }
    }
}
