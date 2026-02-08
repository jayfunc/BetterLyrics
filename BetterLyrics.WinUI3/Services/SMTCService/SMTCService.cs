using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Entities;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.SMTCService
{
    public partial class SMTCService : BaseViewModel, ISMTCService
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly MediaTimelineController _timelineController;
        private readonly SystemMediaTransportControls _smtc;

        private IRandomAccessStream? _currentStream;
        private Stream? _currentNetStream;
        private IUnifiedFileSystem? _currentProvider;

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;

        [ObservableProperty] public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; } = [];
        [ObservableProperty] public partial ExtendedTrack? PlayingTrack { get; set; }

        public SMTCService(ISettingsService settingsService, IFileSystemService fileSystemService)
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;

            _mediaPlayer = new MediaPlayer();
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

            _ = Task.Run(async () =>
            {
                var parsedFiles = await _fileSystemService.GetParsedFilesAsync();
                var playQueue = _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                    {
                        var encodedUri = new Uri(x).AbsoluteUri;
                        return new PlayQueueItem(new ExtendedTrack(parsedFiles.FirstOrDefault(y => y.Uri == encodedUri)));
                    });
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TrackPlayingQueue = [.. playQueue];
                    TrackPlayingQueue.CollectionChanged += TrackPlayingQueue_CollectionChanged;
                });
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

        private void Smtc_PlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            _timelineController.Position = args.RequestedPlaybackPosition;
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            _timelineController.Start();
            _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            PlayNextTrack();
        }

        private void TimelineController_PositionChanged(MediaTimelineController sender, object args)
        {
            _smtc.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties()
            {
                Position = sender.Position,
                EndTime = _mediaPlayer.PlaybackSession.NaturalDuration
            });
        }

        private void TrackPlayingQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths = [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];
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

        private void PlayNextTrack()
        {
            var musicGallerySettings = _settingsService.AppSettings.MusicGallerySettings;
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                switch (musicGallerySettings.PlaybackOrder)
                {
                    case PlaybackOrder.RepeatAll:
                        if (musicGallerySettings.PlayQueueIndex < TrackPlayingQueue.Count - 1)
                        {
                            musicGallerySettings.PlayQueueIndex++;
                        }
                        else
                        {
                            musicGallerySettings.PlayQueueIndex = 0;
                        }
                        break;
                    case PlaybackOrder.RepeatOne:
                        //_timelineController.Position = TimeSpan.Zero;
                        break;
                    case PlaybackOrder.Shuffle:
                        if (TrackPlayingQueue.Count > 0)
                        {
                            musicGallerySettings.PlayQueueIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        break;
                    default:
                        break;
                }
                await PlayTrackAtAsync(musicGallerySettings.PlayQueueIndex);
            });
        }

        private void PlayPreviousTrack()
        {
            var musicGallerySettings = _settingsService.AppSettings.MusicGallerySettings;
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                switch (musicGallerySettings.PlaybackOrder)
                {
                    case PlaybackOrder.RepeatAll:
                        if (musicGallerySettings.PlayQueueIndex > 0)
                        {
                            musicGallerySettings.PlayQueueIndex--;
                        }
                        else
                        {
                            musicGallerySettings.PlayQueueIndex = TrackPlayingQueue.Count - 1;
                        }
                        break;
                    case PlaybackOrder.RepeatOne:
                        //_timelineController.Position = TimeSpan.Zero;
                        break;
                    case PlaybackOrder.Shuffle:
                        if (TrackPlayingQueue.Count > 0)
                        {
                            musicGallerySettings.PlayQueueIndex = new Random().Next(0, TrackPlayingQueue.Count);
                        }
                        break;
                    default:
                        break;
                }
                await PlayTrackAtAsync(musicGallerySettings.PlayQueueIndex);
            });
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
                    GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
                    _timelineController.Pause();
                }
            }
        }

        public async Task PlayTrackAtAsync(int index)
        {
            await PlayTrackAsync(TrackPlayingQueue.ElementAtOrDefault(index));
        }

    }
}
