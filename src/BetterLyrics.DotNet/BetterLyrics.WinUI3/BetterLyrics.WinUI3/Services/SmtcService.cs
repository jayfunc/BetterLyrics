using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using MimeMapping;

namespace BetterLyrics.WinUI3.Services;

public partial class SmtcService : BaseViewModel, ISmtcService
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly MediaPlayer _mediaPlayer;
    private readonly MediaPlaybackList _playbackList;
    private readonly Timer _positionTimer;
    private readonly ISettingsService _settingsService;

    public SmtcService(ISettingsService settingsService, IFileSystemService fileSystemService,
        IAppUIThreadProvider appUiThreadProvider, IGlobalToastProvider globalToastProvider)
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;

        _mediaPlayer = new MediaPlayer();
        _playbackList = new MediaPlaybackList();
        _playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;

        _mediaPlayer.Source = _playbackList;
        _mediaPlayer.SystemMediaTransportControls.IsStopEnabled = true;
        _mediaPlayer.SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;

        _positionTimer = new Timer(1000);
        _positionTimer.Elapsed += PositionTimer_Elapsed;

        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    [ObservableProperty] public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; } = [];
    [ObservableProperty] public partial ExtendedTrack? PlayingTrack { get; set; }

    public async Task UpdatePlaybackListAsync(IEnumerable<PlayQueueItem> playQueue,
        bool recoverPlaybackPosition = false, bool allowAutoPlay = false)
    {
        var musicGallerySettings = _settingsService.AppSettings.MusicGallerySettings;
        var savedIndex = musicGallerySettings.PlayQueueIndex;

        var playQueueList = playQueue.ToList();

        TrackPlayingQueue.CollectionChanged -= TrackPlayingQueue_CollectionChanged;
        TrackPlayingQueue = [.. playQueueList];
        TrackPlayingQueue.CollectionChanged += TrackPlayingQueue_CollectionChanged;

        _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths =
            [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];

        _playbackList.Items.Clear();

        var playbackItems = await Task.Run(() =>
        {
            var items = new List<MediaPlaybackItem>(playQueueList.Count);
            foreach (var item in playQueueList) items.Add(CreatePlaybackItem(item));

            return items;
        });

        foreach (var item in playbackItems) _playbackList.Items.Add(item);

        if (savedIndex > 0 && savedIndex < _playbackList.Items.Count)
        {
            _playbackList.MoveTo((uint)savedIndex);
            if (recoverPlaybackPosition) _mediaPlayer.Position = musicGallerySettings.PlaybackPosition;

            if (allowAutoPlay && _settingsService.AppSettings.MusicGallerySettings.AutoPlay) _mediaPlayer.Play();
        }

        ApplyPlaybackOrder(musicGallerySettings.PlaybackOrder);
    }

    public void ApplyPlaybackOrder(PlaybackOrder order)
    {
        switch (order)
        {
            case PlaybackOrder.Shuffle:
                _mediaPlayer.IsLoopingEnabled = false;
                _playbackList.AutoRepeatEnabled = true;
                _playbackList.ShuffleEnabled = true;
                break;
            case PlaybackOrder.RepeatAll:
                _mediaPlayer.IsLoopingEnabled = false;
                _playbackList.AutoRepeatEnabled = true;
                _playbackList.ShuffleEnabled = false;
                break;
            case PlaybackOrder.RepeatOne:
                _mediaPlayer.IsLoopingEnabled = true;
                _playbackList.AutoRepeatEnabled = false;
                _playbackList.ShuffleEnabled = false;
                break;
        }
    }

    public void PlayNextTrack()
    {
        _playbackList.MoveNext();
    }

    public void PlayTrack(PlayQueueItem? playQueueItem)
    {
        if (playQueueItem == null) return;
        var index = TrackPlayingQueue.IndexOf(playQueueItem);
        if (index >= 0) PlayTrackAt(index);
    }

    public void PlayTrackAt(int index, bool recoverPlaybackPosition = false)
    {
        if (index >= 0 && index < _playbackList.Items.Count)
        {
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
            _playbackList.MoveTo((uint)index);
            if (recoverPlaybackPosition)
                _mediaPlayer.Position = _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition;

            _mediaPlayer.Play();
        }
        else if (index == -1) // 停止播放
        {
            _mediaPlayer.Pause();
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
            _mediaPlayer.Pause();
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        }
    }

    private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (args.Button == SystemMediaTransportControlsButton.Stop) PlayTrackAt(-1);
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        if (sender.PlaybackState == MediaPlaybackState.Playing)
            _positionTimer.Start();
        else
            _positionTimer.Stop();
    }

    private void PositionTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var session = _mediaPlayer.PlaybackSession;

            _mediaPlayer.SystemMediaTransportControls.UpdateTimelineProperties(
                new SystemMediaTransportControlsTimelineProperties
                {
                    Position = session.Position,
                    EndTime = session.NaturalDuration
                });

            _appUIThreadProvider.Execute(() =>
            {
                _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition = session.Position;
            });
        }
        catch
        {
        }
    }

    private void PlaybackList_CurrentItemChanged(MediaPlaybackList sender,
        CurrentMediaPlaybackItemChangedEventArgs args)
    {
        var newItem = args.NewItem;
        _appUIThreadProvider.Execute(() =>
        {
            if (newItem != null && newItem.Source.CustomProperties.TryGetValue("QueueItem", out var obj) &&
                obj is PlayQueueItem queueItem)
            {
                PlayingTrack = queueItem.Track;
                _settingsService.AppSettings.MusicGallerySettings.PlayQueueIndex = (int)sender.CurrentItemIndex;
            }
            else
            {
                PlayingTrack = null;
            }
        });
    }

    private void TrackPlayingQueue_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths =
            [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                    for (var i = 0; i < e.NewItems.Count; i++)
                    {
                        var item = (PlayQueueItem?)e.NewItems[i];
                        if (item != null) _playbackList.Items.Insert(e.NewStartingIndex + i, CreatePlaybackItem(item));
                    }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                    foreach (var _ in e.OldItems)
                        _playbackList.Items.RemoveAt(e.OldStartingIndex);

                break;
            case NotifyCollectionChangedAction.Reset:
                _playbackList.Items.Clear();
                foreach (var item in TrackPlayingQueue) _playbackList.Items.Add(CreatePlaybackItem(item));

                break;
        }
    }

    private MediaPlaybackItem CreatePlaybackItem(PlayQueueItem queueItem)
    {
        var track = queueItem.Track;
        var binder = new MediaBinder { Token = track.Uri };
        binder.Binding += Binder_Binding;

        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["QueueItem"] = queueItem;

        var item = new MediaPlaybackItem(source);
        var props = item.GetDisplayProperties();

        props.Type = MediaPlaybackType.Music;
        props.MusicProperties.Title = track.Title ?? track.FileName;
        props.MusicProperties.Artist = track.Artist ?? "";
        props.MusicProperties.AlbumTitle = track.Album ?? "";
        props.MusicProperties.Genres.Add(
            $"{ExtendedGenreFiled.FileName}{Path.GetFileNameWithoutExtension(track.FileName)}");

        if (!string.IsNullOrEmpty(track.LocalAlbumArtPath))
            _ = Task.Run(async () =>
            {
                if (File.Exists(track.LocalAlbumArtPath))
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(track.LocalAlbumArtPath);
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);

                    _appUIThreadProvider.Execute(() => { item.ApplyDisplayProperties(props); });
                }
            });

        item.ApplyDisplayProperties(props);
        return item;
    }

    private async void Binder_Binding(MediaBinder sender, MediaBindingEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var uri = sender.Token;
            var track = TrackPlayingQueue.FirstOrDefault(x => x.Track.Uri == uri)?.Track;
            if (track == null) throw new FileNotFoundException("Track not found in queue", uri);

            var targetFolder = _settingsService.AppSettings.LocalMediaFolders.FirstOrDefault(f =>
                track.Uri.StartsWith(f.GetStandardUri().AbsoluteUri, StringComparison.OrdinalIgnoreCase));
            if (targetFolder == null) throw new FileNotFoundException(null, track.Uri.ToDecodedAbsoluteUri());

            var provider = targetFolder.CreateFileSystem();
            if (provider == null) return;
            await provider.ConnectAsync();

            var fileCacheStub = new FilesIndexItem { Uri = uri };
            var sourceStream = await _fileSystemService.OpenFileAsync(provider, fileCacheStub);
            if (sourceStream == null) throw new FileNotFoundException(null, uri);

            if (!sourceStream.CanSeek)
            {
                var memStream = new MemoryStream();
                await sourceStream.CopyToAsync(memStream);
                memStream.Position = 0;
                sourceStream.Dispose();
                sourceStream = memStream;
            }

            args.SetStream(sourceStream.AsRandomAccessStream(),
                MimeUtility.GetMimeMapping(track.FileName));
        }
        catch (Exception ex)
        {
            _appUIThreadProvider.Execute(() =>
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error));
        }
        finally
        {
            deferral.Complete();
        }
    }

    public void PlayPreviousTrack()
    {
        _playbackList.MovePrevious();
    }
}