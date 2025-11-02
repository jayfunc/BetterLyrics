using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MusicGalleryViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly ISettingsService _settingsService;

        private readonly MediaPlayer _mediaPlayer = new();
        private readonly MediaTimelineController _timelineController = new();
        private readonly SystemMediaTransportControls _smtc;

        private readonly DispatcherQueueTimer _refreshSongsTimer;

        // All songs
        private List<Track> _tracks = [];
        // Songs in current playlist
        private List<Track> _playlistTracks = [];
        // Filtered songs based on search query for current playlist
        private List<Track> _filteredTracks = [];

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
        public partial List<Track> SelectedTracks { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; } = [];

        public PlayQueueItem? PlayingQueueItem => TrackPlayingQueue.ElementAtOrDefault(PlayingSongIndex);

        [ObservableProperty]
        public partial PlaybackOrder PlaybackOrder { get; set; }

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
        public partial Track TrackRightTapped { get; set; } = new();

        [ObservableProperty]
        public partial int PlayingSongIndex { get; set; } = -1;

        [ObservableProperty]
        public partial int DisplayedPlayingSongIndex { get; set; } = 0;

        [ObservableProperty]
        public partial string SongSearchQuery { get; set; } = string.Empty;

        public MusicGalleryViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService)
        {
            _refreshSongsTimer = _dispatcherQueue.CreateTimer();

            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;

            SongsTabInfoList.Add(new SongsTabInfo(App.ResourceLoader!.GetString("MusicGalleryPageAllSongs"), "\uE8A9", false, false, CommonSongProperty.Title, string.Empty));

            RefreshSongs();

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
            _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;

            PlaybackOrder = _settingsService.AppSettings.MusicGallerySettings.PlaybackOrder;

            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _timelineController = _mediaPlayer.TimelineController = new();
            _timelineController.PositionChanged += TimelineController_PositionChanged;
            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _mediaPlayer.CommandManager.IsEnabled = false;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.ButtonPressed += Smtc_ButtonPressed;
            _smtc.PlaybackPositionChangeRequested += Smtc_PlaybackPositionChangeRequested;

            _libWatcherService = libWatcherService;
            _libWatcherService.MusicLibraryFilesChanged += LibWatcherService_MusicLibraryFilesChanged;
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
            switch (PlaybackOrder)
            {
                case PlaybackOrder.RepeatAll:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        if (PlayingSongIndex < TrackPlayingQueue.Count - 1)
                        {
                            PlayingSongIndex++;
                        }
                        else
                        {
                            PlayingSongIndex = 0;
                        }
                        PlayTrack(PlayingQueueItem);
                    });
                    break;
                case PlaybackOrder.RepeatOne:
                    _timelineController.Position = TimeSpan.Zero;
                    break;
                case PlaybackOrder.Shuffle:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        if (TrackPlayingQueue.Count > 0)
                        {
                            PlayingSongIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        PlayTrack(PlayingQueueItem);
                    });
                    break;
                default:
                    break;
            }
        }

        private void PlayPreviousTrack()
        {
            switch (PlaybackOrder)
            {
                case PlaybackOrder.RepeatAll:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        if (PlayingSongIndex > 0)
                        {
                            PlayingSongIndex--;
                        }
                        else
                        {
                            PlayingSongIndex = TrackPlayingQueue.Count - 1;
                        }
                        PlayTrack(PlayingQueueItem);
                    });
                    break;
                case PlaybackOrder.RepeatOne:
                    _timelineController.Position = TimeSpan.Zero;
                    break;
                case PlaybackOrder.Shuffle:
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        if (TrackPlayingQueue.Count > 0)
                        {
                            PlayingSongIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        PlayTrack(PlayingQueueItem);
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

                Task.Run(() =>
                {
                    try
                    {
                        foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
                        {
                            if (Directory.Exists(folder.Path) && folder.IsEnabled)
                            {
                                foreach (var file in DirectoryHelper.GetAllFiles(folder.Path))
                                {
                                    if (FileHelper.MusicExtensions.Contains(Path.GetExtension(file)))
                                    {
                                        Track track = new(file);
                                        if (track.Duration <= 0) continue;
                                        _tracks.Add(track);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
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
                        _playlistTracks = _tracks.Where(t => t.GetParentFolderPath().Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
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
                    t.GetParentFolderPath().Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void ApplySongOrderType()
        {
            switch (SongOrderType)
            {
                case CommonSongProperty.Title:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Title),
                        o => ((Track)o).Title
                    );
                    break;
                case CommonSongProperty.Artist:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Artist),
                        o => ((Track)o).Artist
                    );
                    break;
                case CommonSongProperty.Album:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Album),
                        o => ((Track)o).Album
                    );
                    break;
                case CommonSongProperty.Folder:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.GetParentFolderName()),
                        o => ((Track)o).Album
                    );
                    break;
            }
        }

        public void UpdateSelectedPlaylist(SongsTabInfo playlist)
        {
            var found = SongsTabInfoList.Where(x => x.FilterProperty == playlist.FilterProperty && x.FilterValue == playlist.FilterValue)
                .ToList().FirstOrDefault();
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

        public void PlayTrackAt(int index)
        {
            PlayTrack(TrackPlayingQueue.ElementAtOrDefault(index));
        }

        public void PlayTrack(PlayQueueItem? playQueueItem)
        {
            _timelineController.Pause();
            _mediaPlayer.Source = null;
            if (playQueueItem == null)
            {
                _smtc.IsEnabled = false;
            }
            else
            {
                var track = playQueueItem.Track;
                var updater = _smtc.DisplayUpdater;
                _smtc.IsEnabled = true;
                _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(track.Path));
                updater.AppMediaId = Package.Current.Id.FullName;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = track.Title;
                updater.MusicProperties.Artist = track.Artist;
                updater.MusicProperties.AlbumTitle = track.Album;
                if (track.EmbeddedPictures.FirstOrDefault()?.PictureData is byte[] pictureData)
                {
                    updater.Thumbnail = ImageHelper.ByteArrayToRandomAccessStreamReference(pictureData);
                }
                else
                {
                    updater.Thumbnail = null;
                }
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

        partial void OnPlayingSongIndexChanged(int value)
        {
            DisplayedPlayingSongIndex = value + 1;
        }

        partial void OnPlaybackOrderChanged(PlaybackOrder value)
        {
            _settingsService.AppSettings.MusicGallerySettings.PlaybackOrder = value;
        }
    }
}
