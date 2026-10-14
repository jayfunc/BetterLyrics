using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
    private readonly Timer _positionTimer;
    private readonly ISettingsService _settingsService;
    private readonly IAudioMixerService _audioMixerService;

    private int _currentIndex = -1;
    private readonly List<int> _shuffleOrder = [];
    private int _shufflePointer = -1;
    private MediaPlaybackItem? _currentPlaybackItem;

    public bool IsVolumeRestored { get; set; }
    private bool _isApplyingVolume;
    private int _positionSaveTicks;

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

        _mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        _mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

        _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
        _mediaPlayer.SystemMediaTransportControls.IsStopEnabled = true;
        _mediaPlayer.SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;

        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        _positionTimer = new Timer(1000);
        _positionTimer.Elapsed += PositionTimer_Elapsed;

        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender,
        MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        args.Handled = true;
        _appUIThreadProvider.Execute(PlayNextTrack);
    }

    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender,
        MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        args.Handled = true;
        _appUIThreadProvider.Execute(PlayPreviousTrack);
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        _appUIThreadProvider.Execute(() =>
        {
            var order = _settingsService.AppSettings.MusicGallerySettings.PlaybackOrder;
            if (order == PlaybackOrder.RepeatOne)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
                return;
            }

            PlayNextTrack();
        });
    }

    private void CleanupCurrentPlaybackItem()
    {
        if (_currentPlaybackItem == null) return;

        var item = _currentPlaybackItem;
        _currentPlaybackItem = null;

        DisposePlaybackItem(item);
        GC.Collect(1, GCCollectionMode.Optimized);
    }

    private static void DisposePlaybackItem(MediaPlaybackItem item)
    {
        try
        {
            var source = item.Source;
            if (source != null)
            {
                source.CustomProperties["IsDisposed"] = true;
                if (source.CustomProperties.TryGetValue("RandomAccessStream", out var r) && r is IDisposable ras)
                {
                    try { ras.Dispose(); } catch { }
                }
                if (source.CustomProperties.TryGetValue("Stream", out var s) && s is IDisposable stream)
                {
                    try { stream.Dispose(); } catch { }
                }
                if (source.CustomProperties.TryGetValue("FileSystem", out var fs) && fs is IDisposable fileSystem)
                {
                    try { fileSystem.Dispose(); } catch { }
                }
                try { source.Dispose(); } catch { }
            }
        }
        catch { }
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
        _mediaPlayer.Source = null;
        CleanupCurrentPlaybackItem();

        ApplyPlaybackOrder(musicGallerySettings.PlaybackOrder);

        if (savedIndex >= 0 && savedIndex < TrackPlayingQueue.Count)
        {
            _currentIndex = savedIndex;
            var queueItem = TrackPlayingQueue[savedIndex];
            PlayingTrack = queueItem.Track;

            _currentPlaybackItem = CreatePlaybackItem(queueItem);
            UpdateItemDisplayProperties(_currentPlaybackItem, queueItem.Track);

            _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
            _mediaPlayer.Source = _currentPlaybackItem;

            if (recoverPlaybackPosition)
            {
                _mediaPlayer.Position = musicGallerySettings.PlaybackPosition;
            }

            if (allowAutoPlay && musicGallerySettings.AutoPlay)
            {
                _mediaPlayer.Play();
                EnsureVolumeApplied();
            }
        }
        else
        {
            _currentIndex = -1;
            PlayingTrack = null;
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        }

        await Task.CompletedTask;
    }

    public void ApplyPlaybackOrder(PlaybackOrder order)
    {
        switch (order)
        {
            case PlaybackOrder.Shuffle:
                _mediaPlayer.IsLoopingEnabled = false;
                GenerateShuffleOrder();
                break;
            case PlaybackOrder.RepeatAll:
                _mediaPlayer.IsLoopingEnabled = false;
                break;
            case PlaybackOrder.RepeatOne:
                _mediaPlayer.IsLoopingEnabled = true;
                break;
        }
    }

    private void GenerateShuffleOrder()
    {
        _shuffleOrder.Clear();
        var count = TrackPlayingQueue.Count;
        if (count == 0)
        {
            _shufflePointer = -1;
            return;
        }

        var indices = Enumerable.Range(0, count).ToList();
        if (_currentIndex >= 0 && _currentIndex < count)
        {
            indices.Remove(_currentIndex);
            ShuffleList(indices);
            _shuffleOrder.Add(_currentIndex);
            _shuffleOrder.AddRange(indices);
            _shufflePointer = 0;
        }
        else
        {
            ShuffleList(indices);
            _shuffleOrder.AddRange(indices);
            _shufflePointer = -1;
        }
    }

    private static void ShuffleList(IList<int> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void PlayNextTrack()
    {
        if (TrackPlayingQueue.Count == 0) return;

        var order = _settingsService.AppSettings.MusicGallerySettings.PlaybackOrder;
        if (order == PlaybackOrder.Shuffle)
        {
            if (_shuffleOrder.Count != TrackPlayingQueue.Count)
            {
                GenerateShuffleOrder();
            }

            _shufflePointer++;
            if (_shufflePointer >= _shuffleOrder.Count)
            {
                GenerateShuffleOrder();
                _shufflePointer = 0;
            }

            if (_shufflePointer >= 0 && _shufflePointer < _shuffleOrder.Count)
            {
                PlayTrackAt(_shuffleOrder[_shufflePointer]);
            }
        }
        else
        {
            var nextIndex = (_currentIndex + 1) % TrackPlayingQueue.Count;
            PlayTrackAt(nextIndex);
        }
    }

    public void PlayPreviousTrack()
    {
        if (TrackPlayingQueue.Count == 0) return;

        try
        {
            if (_mediaPlayer.PlaybackSession.Position > TimeSpan.FromSeconds(3))
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                return;
            }
        }
        catch { }

        var order = _settingsService.AppSettings.MusicGallerySettings.PlaybackOrder;
        if (order == PlaybackOrder.Shuffle)
        {
            if (_shuffleOrder.Count != TrackPlayingQueue.Count)
            {
                GenerateShuffleOrder();
            }

            _shufflePointer--;
            if (_shufflePointer < 0)
            {
                _shufflePointer = Math.Max(0, _shuffleOrder.Count - 1);
            }

            if (_shufflePointer >= 0 && _shufflePointer < _shuffleOrder.Count)
            {
                PlayTrackAt(_shuffleOrder[_shufflePointer]);
            }
        }
        else
        {
            var prevIndex = (_currentIndex - 1 + TrackPlayingQueue.Count) % TrackPlayingQueue.Count;
            PlayTrackAt(prevIndex);
        }
    }

    public void PlayTrack(PlayQueueItem? playQueueItem)
    {
        if (playQueueItem == null) return;
        var index = TrackPlayingQueue.IndexOf(playQueueItem);
        if (index >= 0) PlayTrackAt(index);
    }

    public void PlayTrackAt(int index, bool recoverPlaybackPosition = false)
    {
        if (index >= 0 && index < TrackPlayingQueue.Count)
        {
            _currentIndex = index;
            _settingsService.AppSettings.MusicGallerySettings.PlayQueueIndex = index;

            var queueItem = TrackPlayingQueue[index];
            PlayingTrack = queueItem.Track;

            if (_settingsService.AppSettings.MusicGallerySettings.PlaybackOrder == PlaybackOrder.Shuffle)
            {
                if (_shuffleOrder.Count != TrackPlayingQueue.Count)
                {
                    GenerateShuffleOrder();
                }
                else
                {
                    var pos = _shuffleOrder.IndexOf(index);
                    if (pos != -1) _shufflePointer = pos;
                }
            }

            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            CleanupCurrentPlaybackItem();

            _currentPlaybackItem = CreatePlaybackItem(queueItem);
            UpdateItemDisplayProperties(_currentPlaybackItem, queueItem.Track);

            _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
            _mediaPlayer.Source = _currentPlaybackItem;

            if (recoverPlaybackPosition)
            {
                _mediaPlayer.Position = _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition;
            }

            _mediaPlayer.Play();
            EnsureVolumeApplied();
        }
        else if (index == -1) // 停止播放
        {
            _mediaPlayer.Pause();
            _positionTimer.Stop();
            _mediaPlayer.Source = null;
            CleanupCurrentPlaybackItem();

            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
            _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition = TimeSpan.Zero;
            _settingsService.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;
            _currentIndex = -1;
            PlayingTrack = null;
        }
    }

    private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (args.Button == SystemMediaTransportControlsButton.Stop)
        {
            _appUIThreadProvider.Execute(() => PlayTrackAt(-1));
        }
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

    public void SaveCurrentPlaybackPosition()
    {
        try
        {
            var session = _mediaPlayer.PlaybackSession;
            if (session != null)
            {
                _appUIThreadProvider.Execute(() =>
                {
                    _settingsService.AppSettings.MusicGallerySettings.PlaybackPosition = session.Position;
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

            if (++_positionSaveTicks >= 5)
            {
                _positionSaveTicks = 0;
                SaveCurrentPlaybackPosition();
            }
        }
        catch
        {
        }
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
                if (e.NewStartingIndex <= _currentIndex)
                {
                    _currentIndex += e.NewItems?.Count ?? 0;
                    _settingsService.AppSettings.MusicGallerySettings.PlayQueueIndex = _currentIndex;
                }
                if (_settingsService.AppSettings.MusicGallerySettings.PlaybackOrder == PlaybackOrder.Shuffle)
                {
                    GenerateShuffleOrder();
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (TrackPlayingQueue.Count == 0)
                {
                    PlayTrackAt(-1);
                    _shuffleOrder.Clear();
                    _shufflePointer = -1;
                    return;
                }

                var removedCount = e.OldItems?.Count ?? 0;
                var removedStart = e.OldStartingIndex;
                var removedEnd = removedStart + removedCount - 1;

                if (_currentIndex >= removedStart && _currentIndex <= removedEnd)
                {
                    var newIndex = Math.Clamp(removedStart, 0, TrackPlayingQueue.Count - 1);
                    PlayTrackAt(newIndex);
                }
                else if (_currentIndex > removedEnd)
                {
                    _currentIndex -= removedCount;
                    _settingsService.AppSettings.MusicGallerySettings.PlayQueueIndex = _currentIndex;
                }

                if (_settingsService.AppSettings.MusicGallerySettings.PlaybackOrder == PlaybackOrder.Shuffle)
                {
                    GenerateShuffleOrder();
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                if (TrackPlayingQueue.Count == 0)
                {
                    PlayTrackAt(-1);
                    _shuffleOrder.Clear();
                    _shufflePointer = -1;
                }
                else
                {
                    if (_settingsService.AppSettings.MusicGallerySettings.PlaybackOrder == PlaybackOrder.Shuffle)
                    {
                        GenerateShuffleOrder();
                    }
                }
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
            if (source.CustomProperties.TryGetValue("IsDisposed", out var d) && d is true)
            {
                return;
            }

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

            if (source.CustomProperties.TryGetValue("IsDisposed", out var d2) && d2 is true)
            {
                sourceStream.Dispose();
                provider.Dispose();
                return;
            }

            var ras = sourceStream.AsRandomAccessStream();
            source.CustomProperties["Stream"] = sourceStream;
            source.CustomProperties["FileSystem"] = provider;
            source.CustomProperties["RandomAccessStream"] = ras;

            args.SetStream(ras,
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
}