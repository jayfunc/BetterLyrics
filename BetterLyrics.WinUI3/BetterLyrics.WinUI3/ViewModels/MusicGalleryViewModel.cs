using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
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
    public partial class MusicGalleryViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<ObservableCollection<LocalMediaFolder>>>
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly MediaPlayer _mediaPlayer = new();
        private readonly MediaTimelineController _timelineController = new();
        private readonly SystemMediaTransportControls _smtc;
        private List<Track> _tracks = [];
        private List<Track> _filteredTracks = [];

        [ObservableProperty]
        public partial bool IsLocalMediaNotFound { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<GroupInfoList> GroupedTracks { get; set; } = [];

        [ObservableProperty]
        public partial List<Track> SelectedTracks { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<Track> TrackPlayingQueue { get; set; } = [];

        public Track? PlayingTrack => TrackPlayingQueue.ElementAtOrDefault(PlayingSongIndex);

        [ObservableProperty]
        public partial PlaybackOrder PlaybackOrder { get; set; }

        [ObservableProperty]
        public partial SongOrderType SongOrderType { get; set; } = SongOrderType.Title;

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

        public MusicGalleryViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService) : base(settingsService)
        {
            RefreshSongs();

            PlaybackOrder = _settingsService.PlaybackOrder;

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
                        PlayTrack(PlayingTrack);
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
                        PlayTrack(PlayingTrack);
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
                        PlayTrack(PlayingTrack);
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
                        PlayTrack(PlayingTrack);
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

        public void RefreshSongs()
        {
            _dispatcherQueueTimer.Debounce(() =>
            {
                IsDataLoading = true;
                _tracks.Clear();

                Task.Run(() =>
                {
                    foreach (var folder in _settingsService.LocalMediaFolders)
                    {
                        if (Directory.Exists(folder.Path) && folder.IsEnabled)
                        {
                            foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                            {
                                Track track = new(file);
                                if (track.Duration <= 0) continue;
                                _tracks.Add(track);
                            }
                        }
                    }

                    ApplySongSearchQuery();

                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        IsLocalMediaNotFound = !_filteredTracks.Any();
                        ApplySongOrderType();
                        IsDataLoading = false;
                    });
                });
            }, TimeSpan.FromMilliseconds(100));
        }

        public void ApplySongSearchQuery()
        {
            if (string.IsNullOrWhiteSpace(SongSearchQuery))
            {
                _filteredTracks = _tracks;
                return;
            }
            _filteredTracks = new List<Track>(
                _tracks.Where(t => t.Title.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   t.Artist.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   t.Album.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase))
            );
        }

        private void ApplySongOrderType()
        {
            switch (SongOrderType)
            {
                case SongOrderType.Title:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Title),
                        o => ((Track)o).Title
                    );
                    break;
                case SongOrderType.Artist:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Artist),
                        o => ((Track)o).Artist
                    );
                    break;
                case SongOrderType.Album:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Album),
                        o => ((Track)o).Album
                    );
                    break;
            }
        }

        public void PlayTrackAt(int index)
        {
            PlayTrack(TrackPlayingQueue.ElementAtOrDefault(index));
        }

        public void PlayTrack(Track? track)
        {
            _timelineController.Pause();
            _mediaPlayer.Source = null;
            if (track == null)
            {
                _smtc.IsEnabled = false;
            }
            else
            {
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

        partial void OnSongOrderTypeChanged(SongOrderType value)
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
            _settingsService.PlaybackOrder = value;
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<LocalMediaFolder>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LocalMediaFolders))
                {
                    RefreshSongs();
                }
            }
        }
    }
}
