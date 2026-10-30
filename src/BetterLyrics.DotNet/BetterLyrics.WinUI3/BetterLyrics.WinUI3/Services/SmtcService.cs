using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using MimeMapping;
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
using Timer = System.Timers.Timer;

namespace BetterLyrics.WinUI3.Services;

/// <summary>
/// Singleton Service
/// </summary>
public partial class SmtcService : ObservableObject, ISmtcService
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ISettingsService _settingsService;
    private readonly IAudioMixerService _audioMixerService;
    private readonly IPlaybackStateService _playbackStateService;
    private readonly ILogger<SmtcService> _logger;

    private MediaPlayer _mediaPlayer;
    private readonly Timer _positionTimer;

    private int _currentIndex = -1;
    private readonly List<int> _shuffleOrder = [];
    private int _shufflePointer = -1;
    private MediaPlaybackItem? _currentPlaybackItem;
    private TimeSpan? _positionToRestoreOnMediaOpened;

    public bool IsVolumeRestored { get; set; }
    public bool IsPlaying => _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
    private bool _isApplyingVolume;
    private int _positionSaveTicks;

    [ObservableProperty] public partial ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; } = [];
    [ObservableProperty] public partial ExtendedTrack? PlayingTrack { get; set; }
    [ObservableProperty] public partial PlaybackState PlaybackState { get; set; }

    public SmtcService(ISettingsService settingsService, IFileSystemService fileSystemService,
        IAppUIThreadProvider appUiThreadProvider, IGlobalToastProvider globalToastProvider,
        IAudioMixerService audioMixerService, IPlaybackStateService playbackStateService,
        ILogger<SmtcService> logger)
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;
        _audioMixerService = audioMixerService;
        _playbackStateService = playbackStateService;
        _logger = logger;

        PlaybackState = _playbackStateService.CurrentState;

        _mediaPlayer = CreateMediaPlayer();

        _positionTimer = new Timer(1000);
        _positionTimer.Elapsed += PositionTimer_Elapsed;
    }

    private MediaPlayer CreateMediaPlayer()
    {
        var mediaPlayer = new MediaPlayer();

        mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

        mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        mediaPlayer.SystemMediaTransportControls.IsStopEnabled = true;
        mediaPlayer.SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;

        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        return mediaPlayer;
    }

    private void ReleaseMediaPlayer()
    {
        var mediaPlayer = _mediaPlayer;

        // 先换上新实例，避免计时器、事件回调读到已销毁的对象
        _mediaPlayer = CreateMediaPlayer();

        try
        {
            mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
            mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            mediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
            mediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;
            mediaPlayer.SystemMediaTransportControls.ButtonPressed -= SystemMediaTransportControls_ButtonPressed;
            mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

            mediaPlayer.Pause();
            mediaPlayer.Source = null;
            mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
            mediaPlayer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release the media player.");
        }
    }

    private void ResetPlayback()
    {
        _positionTimer.Stop();
        _positionToRestoreOnMediaOpened = null;

        // 空闲的 MediaPlayer 不持有音轨，也就不会注册 SMTC 会话，此时无需重建实例
        if (_currentPlaybackItem == null && _mediaPlayer.Source == null) return;

        try
        {
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to reset the media player.");
        }

        CleanupCurrentPlaybackItem();
        ReleaseMediaPlayer();
    }

    private void LoadAndPlayTrack(int index, bool recoverPlaybackPosition)
    {
        var queueItem = TrackPlayingQueue[index];
        PlayingTrack = queueItem.Track;

        var previousItem = _currentPlaybackItem;
        _currentPlaybackItem = CreatePlaybackItem(queueItem);
        UpdateItemDisplayProperties(_currentPlaybackItem, queueItem.Track);

        _positionToRestoreOnMediaOpened = recoverPlaybackPosition ? PlaybackState.PlaybackPosition : null;

        _mediaPlayer.SystemMediaTransportControls.IsEnabled = true;
        _mediaPlayer.Source = _currentPlaybackItem;

        if (previousItem != null)
        {
            // 直接替换 Source 可以避免出现"Source 为空"的中间态，让 SMTC 会话一直挂着最新音轨
            DisposePlaybackItem(previousItem);
            GC.Collect(1, GCCollectionMode.Optimized);
        }

        _mediaPlayer.Play();
        EnsureVolumeApplied();
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        if (_positionToRestoreOnMediaOpened.HasValue)
        {
            var targetPos = _positionToRestoreOnMediaOpened.Value;
            _positionToRestoreOnMediaOpened = null;
            try
            {
                if (targetPos > TimeSpan.Zero)
                {
                    sender.Position = targetPos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore the saved playback position.");
            }
        }
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _logger.LogError("Media playback failed. Error: {Error}; message: {ErrorMessage}; extended code: {ExtendedErrorCode}.",
            args.Error, args.ErrorMessage, args.ExtendedErrorCode);
        _positionToRestoreOnMediaOpened = null;
    }

    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender,
        MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        args.Handled = true;
        _appUIThreadProvider.Execute(() =>
        {
            PlayNextTrack();
        });
    }

    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender,
        MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        args.Handled = true;
        _appUIThreadProvider.Execute(() =>
        {
            PlayPreviousTrack();
        });
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

    public async Task UpdatePlaybackListAsync(IEnumerable<PlayQueueItem> playQueue, bool recoverPlaybackPosition = false, bool allowAutoPlay = false)
    {
        var musicGallerySettings = _settingsService.AppSettings.MusicGallerySettings;

        var savedPlaybackState = _playbackStateService.CurrentState;
        var savedPlaybackIndex = savedPlaybackState.PlayQueueIndex;
        var savedPlaybackPosition = savedPlaybackState.PlaybackPosition;

        var playQueueList = playQueue.ToList();

        _logger.LogDebug("Updating playback queue. Tracks: {TrackCount}; recover position: {RecoverPosition}; auto play: {AutoPlay}.",
            playQueueList.Count, recoverPlaybackPosition, allowAutoPlay);

        TrackPlayingQueue.CollectionChanged -= TrackPlayingQueue_CollectionChanged;
        TrackPlayingQueue = [.. playQueueList];
        TrackPlayingQueue.CollectionChanged += TrackPlayingQueue_CollectionChanged;

        PlaybackState.PlayQueuePaths = [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];

        ApplyPlaybackOrder(musicGallerySettings.PlaybackOrder);

        if (_currentPlaybackItem != null && PlayingTrack != null)
        {
            var existingItem = TrackPlayingQueue.FirstOrDefault(x => x.Track.Uri == PlayingTrack.Uri);
            if (existingItem != null)
            {
                var existingIndex = TrackPlayingQueue.IndexOf(existingItem);
                _currentIndex = existingIndex;
                PlaybackState.PlayQueueIndex = existingIndex;

                // 队列重建后会生成新的 ExtendedTrack 实例（元数据、封面路径可能已被重新解析），
                // 这里同步给 PlayingTrack，否则界面会一直绑定在旧数据上（例如封面一直是占位图）。
                PlayingTrack = existingItem.Track;

                if (allowAutoPlay && musicGallerySettings.AutoPlay && !IsPlaying)
                {
                    _mediaPlayer.Play();
                    EnsureVolumeApplied();
                }

                await Task.CompletedTask;
                return;
            }
        }

        ResetPlayback();

        if (savedPlaybackIndex >= 0 && savedPlaybackIndex < TrackPlayingQueue.Count)
        {
            _currentIndex = savedPlaybackIndex;
            PlaybackState.PlayQueueIndex = savedPlaybackIndex;
            PlaybackState.PlaybackPosition = savedPlaybackPosition;

            PlayingTrack = TrackPlayingQueue[savedPlaybackIndex].Track;

            if (allowAutoPlay && musicGallerySettings.AutoPlay)
            {
                LoadAndPlayTrack(savedPlaybackIndex, recoverPlaybackPosition);
            }
        }
        else
        {
            _currentIndex = -1;
            PlaybackState.PlayQueueIndex = -1;
            PlayingTrack = null;
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

    public void Play()
    {
        // 音轨是懒加载的（见 UpdatePlaybackListAsync），恢复队列后第一次播放时在这里挂上音轨
        if (_mediaPlayer.Source == null && _currentIndex >= 0 && _currentIndex < TrackPlayingQueue.Count)
        {
            LoadAndPlayTrack(_currentIndex, recoverPlaybackPosition: true);
            return;
        }

        _mediaPlayer.Play();
        EnsureVolumeApplied();
    }

    public void Pause()
    {
        _mediaPlayer.Pause();
        SaveCurrentPlaybackPosition();
    }

    public void Stop()
    {
        PlayTrackAt(-1);
    }

    public TimeSpan CurrentPosition
    {
        get
        {
            try
            {
                return _mediaPlayer.Source == null ? PlaybackState.PlaybackPosition : _mediaPlayer.PlaybackSession.Position;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read the current playback position.");
                return TimeSpan.Zero;
            }
        }
    }

    public void Seek(TimeSpan position)
    {
        try
        {
            if (_mediaPlayer.Source == null) return;
            _mediaPlayer.PlaybackSession.Position = position;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seek to {Position}.", position);
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to reset the playback position before switching tracks.");
        }

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
            PlaybackState.PlayQueueIndex = index;

            var queueItem = TrackPlayingQueue[index];

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

            LoadAndPlayTrack(index, recoverPlaybackPosition);

            _logger.LogInformation("Playing track: {Title} - {Artist} (queue index {Index}).", queueItem.Track.Title,
                queueItem.Track.Artist, index);
        }
        else if (index == -1) // 停止播放
        {
            ResetPlayback();

            _currentIndex = -1;
            PlaybackState.PlayQueueIndex = -1;
            PlaybackState.PlaybackPosition = TimeSpan.Zero;

            PlayingTrack = null;

            _logger.LogInformation("Playback stopped.");
        }
    }

    private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (args.Button == SystemMediaTransportControlsButton.Stop)
        {
            _appUIThreadProvider.Execute(() =>
            {
                PlayTrackAt(-1);
            });
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply the configured application volume.");
            }
            finally
            {
                _isApplyingVolume = false;
            }
        });
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        _appUIThreadProvider.Execute(() => OnPropertyChanged(nameof(IsPlaying)));

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
            if (_currentPlaybackItem == null || PlayingTrack == null || _mediaPlayer.Source == null) return;

            var session = _mediaPlayer.PlaybackSession;
            if (session != null)
            {
                var pos = session.Position;
                _appUIThreadProvider.Execute(() =>
                {
                    PlaybackState.PlaybackPosition = pos;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read the current playback position.");
        }
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to update the media timeline properties.");
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
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to apply album art to media display properties.");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to load album art for media display properties.");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update media display properties for {Title}.", track.Title);
        }
    }

    private void TrackPlayingQueue_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PlaybackState.PlayQueuePaths = [.. TrackPlayingQueue.Select(x => x.Track.Uri.ToDecodedAbsoluteUri())];

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewStartingIndex <= _currentIndex)
                {
                    _currentIndex += e.NewItems?.Count ?? 0;
                    PlaybackState.PlayQueueIndex = _currentIndex;
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
                    PlaybackState.PlayQueueIndex = _currentIndex;
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
            _logger.LogError(ex, "Failed to open the media stream for {Title} ({Uri}).", queueItem.Track.Title,
                queueItem.Track.Uri);
            if (source.CustomProperties.ContainsKey("Stream") == false)
            {
                sourceStream?.Dispose();
            }
            if (source.CustomProperties.ContainsKey("FileSystem") == false)
            {
                provider?.Dispose();
            }

            _appUIThreadProvider.Execute(() =>
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            });
        }
        finally
        {
            deferral.Complete();
        }
    }
}