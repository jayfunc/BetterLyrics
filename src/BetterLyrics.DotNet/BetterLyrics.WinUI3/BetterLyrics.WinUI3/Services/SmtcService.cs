using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
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
    private readonly IAudioMixerService _audioMixerService;

    public bool IsVolumeRestored { get; set; }
    private bool _isApplyingVolume;

    [ObservableProperty] public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; } = [];
    [ObservableProperty] public partial ExtendedTrack? PlayingTrack { get; set; }

    public SmtcService(ISettingsService settingsService, IFileSystemService fileSystemService,
        IAppUIThreadProvider appUiThreadProvider, IGlobalToastProvider globalToastProvider,
        IAudioMixerService audioMixerService)
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;
        _audioMixerService = audioMixerService;

        _mediaPlayer = new MediaPlayer();
        _playbackList = new MediaPlaybackList();
        _playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;

        _mediaPlayer.Source = _playbackList;
        _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
        _mediaPlayer.SystemMediaTransportControls.IsStopEnabled = true;
        _mediaPlayer.SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;

        _positionTimer = new Timer(1000);
        _positionTimer.Elapsed += PositionTimer_Elapsed;

        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    private void ResetPlaybackList()
    {
        _appUIThreadProvider.Execute(() =>
        {
            try
            {
                var oldItems = _playbackList.Items.ToList();
                _playbackList.Items.Clear();
                _ = Task.Run(() => DisposePlaybackItems(oldItems));

                foreach (var item in TrackPlayingQueue.ToList())
                {
                    _playbackList.Items.Add(CreatePlaybackItem(item));
                }
            }
            catch { }
        });
    }

    private static void DisposePlaybackItems(IEnumerable<MediaPlaybackItem> items)
    {
        foreach (var item in items.ToList())
        {
            try
            {
                if (item.Source?.CustomProperties.TryGetValue("Stream", out var s) == true && s is IDisposable stream)
                {
                    stream.Dispose();
                }
                if (item.Source?.CustomProperties.TryGetValue("FileSystem", out var fs) == true && fs is IDisposable fileSystem)
                {
                    fileSystem.Dispose();
                }
                item.Source?.Dispose();
            }
            catch { }
        }
    }

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

        _mediaPlayer.Pause();
        _positionTimer.Stop();

        _playbackList.ShuffleEnabled = false;
        _playbackList.AutoRepeatEnabled = false;

        var oldItems = _playbackList.Items.ToList();
        _playbackList.Items.Clear();
        _ = Task.Run(() => DisposePlaybackItems(oldItems));

        var playbackItems = await Task.Run(() =>
        {
            var items = new List<MediaPlaybackItem>(playQueueList.Count);
            foreach (var item in playQueueList) items.Add(CreatePlaybackItem(item));

            return items;
        });

        foreach (var item in playbackItems) _playbackList.Items.Add(item);

        if (savedIndex >= 0 && savedIndex < _playbackList.Items.Count)
        {
            _playbackList.MoveTo((uint)savedIndex);
            if (recoverPlaybackPosition) _mediaPlayer.Position = musicGallerySettings.PlaybackPosition;

            if (allowAutoPlay && _settingsService.AppSettings.MusicGallerySettings.AutoPlay)
            {
                _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
                _mediaPlayer.Play();
                EnsureVolumeApplied();
            }
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
            EnsureVolumeApplied();
        }
        else if (index == -1) // 停止播放
        {
            _mediaPlayer.Pause();
            _positionTimer.Stop();
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
            _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition = TimeSpan.Zero;
            PlayingTrack = null;
            ResetPlaybackList();
        }
    }

    private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (args.Button == SystemMediaTransportControlsButton.Stop) PlayTrackAt(-1);
    }

    private void EnsureVolumeApplied()
    {
        if (_isApplyingVolume) return;
        _isApplyingVolume = true;

        var targetVolume = _settingsService.AppSettings.MusicGallerySettings.Volume;
        _ = Task.Run(async () =>
        {
            try
            {
                for (var i = 0; i < 20; i++)
                {
                    var currentVol = _audioMixerService.GetApplicationVolume(Environment.ProcessId);
                    if (currentVol != -1)
                    {
                        if (!IsVolumeRestored || currentVol != targetVolume)
                        {
                            _audioMixerService.SetApplicationVolume(Environment.ProcessId, targetVolume);
                            IsVolumeRestored = true;
                        }
                        break;
                    }
                    await Task.Delay(100);
                }
            }
            finally
            {
                _isApplyingVolume = false;
            }
        });
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        if (sender.PlaybackState == MediaPlaybackState.Playing)
        {
            _positionTimer.Start();
            EnsureVolumeApplied();
        }
        else
        {
            _positionTimer.Stop();
            SaveCurrentPlaybackPosition();
        }
    }

    private void SaveCurrentPlaybackPosition()
    {
        try
        {
            var session = _mediaPlayer.PlaybackSession;
            if (session != null)
            {
                var pos = session.Position;
                _appUIThreadProvider.Execute(() =>
                {
                    _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition = pos;
                });
            }
        }
        catch { }
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
                UpdateItemDisplayProperties(newItem, queueItem.Track);
            }
            else
            {
                PlayingTrack = null;
            }
        });
    }

    private void UpdateItemDisplayProperties(MediaPlaybackItem item, ExtendedTrack track)
    {
        try
        {
            var props = item.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = track.Title ?? track.FileName;
            props.MusicProperties.Artist = track.Artist ?? "";
            props.MusicProperties.AlbumTitle = track.Album ?? "";
            props.MusicProperties.Genres.Clear();
            props.MusicProperties.Genres.Add(
                $"{ExtendedGenreFiled.FileName}{Path.GetFileNameWithoutExtension(track.FileName)}");

            item.ApplyDisplayProperties(props);

            if (!string.IsNullOrEmpty(track.LocalAlbumArtPath))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (File.Exists(track.LocalAlbumArtPath))
                        {
                            var storageFile = await StorageFile.GetFileFromPathAsync(track.LocalAlbumArtPath);
                            var thumbProps = item.GetDisplayProperties();
                            thumbProps.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);

                            _appUIThreadProvider.Execute(() =>
                            {
                                try
                                {
                                    item.ApplyDisplayProperties(thumbProps);
                                }
                                catch { }
                            });
                        }
                    }
                    catch { }
                });
            }
        }
        catch { }
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
                {
                    var itemsToDispose = new List<MediaPlaybackItem>();
                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        var index = e.OldStartingIndex;
                        if (index >= 0 && index < _playbackList.Items.Count)
                        {
                            var item = _playbackList.Items[index];
                            _playbackList.Items.RemoveAt(index);
                            itemsToDispose.Add(item);
                        }
                    }

                    if (itemsToDispose.Count > 0)
                    {
                        _ = Task.Run(() => DisposePlaybackItems(itemsToDispose));
                    }
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                var oldResetItems = _playbackList.Items.ToList();
                _playbackList.Items.Clear();
                _ = Task.Run(() => DisposePlaybackItems(oldResetItems));
                foreach (var item in TrackPlayingQueue.ToList()) _playbackList.Items.Add(CreatePlaybackItem(item));

                break;
        }
    }

    private MediaPlaybackItem CreatePlaybackItem(PlayQueueItem queueItem)
    {
        var track = queueItem.Track;
        var binder = new MediaBinder { Token = track.Uri };
        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["QueueItem"] = queueItem;

        binder.Binding += (s, e) => Binder_Binding(s, e, source, queueItem);

        return new MediaPlaybackItem(source);
    }

    private async void Binder_Binding(MediaBinder sender, MediaBindingEventArgs args, MediaSource source, PlayQueueItem queueItem)
    {
        var deferral = args.GetDeferral();
        IUnifiedFileSystem? provider = null;
        Stream? sourceStream = null;
        try
        {
            var track = queueItem.Track;
            var uri = track.Uri;

            var targetFolder = _settingsService.AppSettings.LocalMediaFolders.FirstOrDefault(f =>
                track.Uri.StartsWith(f.GetStandardUri().AbsoluteUri, StringComparison.OrdinalIgnoreCase));
            if (targetFolder == null) throw new FileNotFoundException(null, track.Uri.ToDecodedAbsoluteUri());

            provider = targetFolder.CreateFileSystem();
            if (provider == null) throw new IOException($"Failed to create file system for '{targetFolder.Name ?? targetFolder.UriPath}'.");
            await provider.ConnectAsync();

            var fileCacheStub = new FilesIndexItem { Uri = uri };
            sourceStream = await _fileSystemService.OpenFileAsync(provider, fileCacheStub);
            if (sourceStream == null)
            {
                throw new FileNotFoundException(null, uri);
            }

            if (!sourceStream.CanSeek)
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"BetterLyrics_stream_{Guid.NewGuid():N}.tmp");
                FileStream? tempStream = null;
                try
                {
                    tempStream = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
                    await sourceStream.CopyToAsync(tempStream);
                    tempStream.Position = 0;
                    sourceStream.Dispose();
                    sourceStream = tempStream;
                }
                catch
                {
                    tempStream?.Dispose();
                    throw;
                }
            }

            source.CustomProperties["Stream"] = sourceStream;
            source.CustomProperties["FileSystem"] = provider;

            args.SetStream(sourceStream.AsRandomAccessStream(),
                MimeUtility.GetMimeMapping(track.FileName));
        }
        catch (Exception ex)
        {
            if (source.CustomProperties.ContainsKey("Stream") == false)
            {
                sourceStream?.Dispose();
            }
            if (source.CustomProperties.ContainsKey("FileSystem") == false)
            {
                provider?.Dispose();
            }

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