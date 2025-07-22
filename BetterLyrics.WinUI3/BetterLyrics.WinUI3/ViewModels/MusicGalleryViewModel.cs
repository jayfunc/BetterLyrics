using ATL;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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

        [ObservableProperty]
        public partial ObservableCollection<Track> Tracks { get; set; } = [];

        [ObservableProperty]
        public partial bool IsDataLoading { get; set; } = false;

        public MusicGalleryViewModel(ISettingsService settingsService, ILibWatcherService libWatcherService) : base(settingsService)
        {
            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _timelineController = _mediaPlayer.TimelineController = new();
            _timelineController.PositionChanged += TimelineController_PositionChanged;
            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _mediaPlayer.CommandManager.IsEnabled = false;
            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.ButtonPressed += Smtc_ButtonPressed;

            _libWatcherService = libWatcherService;
            _libWatcherService.MusicLibraryFilesChanged += LibWatcherService_MusicLibraryFilesChanged;
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            throw new NotImplementedException();
        }

        private void TimelineController_PositionChanged(MediaTimelineController sender, object args)
        {
            _smtc.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties()
            {
                Position = sender.Position,
                EndTime = sender.Duration ?? TimeSpan.Zero
            });
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    _mediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                    _mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    //Next
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    //Previous
                    break;
            }
        }

        private void LibWatcherService_MusicLibraryFilesChanged(object? sender, Events.LibChangedEventArgs e)
        {
            RefreshSongs();
        }

        public void RefreshSongs()
        {
            IsDataLoading = true;
            Tracks.Clear();

            Task.Run(() =>
            {
                foreach (var folder in _settingsService.LocalMediaFolders)
                {
                    if (Directory.Exists(folder.Path) && folder.IsEnabled)
                    {
                        foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                        {
                            Track track = new(file);
                            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                            {
                                Tracks.Add(track);
                            });
                        }
                    }
                }

                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsDataLoading = false;
                });
            });
        }

        public void PlaySongAt(int? index)
        {
            if (index.HasValue)
            {
                var track = Tracks.ElementAtOrDefault(index.Value);
                if (track != null)
                {
                    _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(track.Path));
                    var updater = _smtc.DisplayUpdater;
                    updater.AppMediaId = Package.Current.Id.FullName;
                    updater.Type = MediaPlaybackType.Music;
                    updater.MusicProperties.Title = track.Title;
                    updater.MusicProperties.Artist = track.Artist;
                    updater.MusicProperties.AlbumTitle = track.Album;
                    if (track.EmbeddedPictures.FirstOrDefault()?.PictureData is byte[] pictureData)
                    {
                        updater.Thumbnail = ImageHelper.ByteArrayToRandomAccessStreamReference(pictureData);
                    }
                    _timelineController.Duration = TimeSpan.FromSeconds(track.Duration);
                    _timelineController.Start();
                    updater.Update();
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
            }
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
