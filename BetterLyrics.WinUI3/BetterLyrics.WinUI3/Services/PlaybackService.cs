// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Services
{
    public partial class PlaybackService : BaseViewModel, IPlaybackService,
        IRecipient<PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>>>,
        IRecipient<PropertyChangedMessage<ObservableCollection<AlbumArtSearchProviderInfo>>>
    {
        private readonly IAlbumArtSearchService _albumArtSearchService;
        private readonly ILogger<PlaybackService> _logger;
        private readonly MediaManager _mediaManager = new();
        private readonly LatestOnlyTaskRunner _AlbumArtRefreshRunner = new();
        private readonly LatestOnlyTaskRunner _OnAnyMediaPropertyChangedRunner = new();

        private SongInfo? _cachedSongInfo;
        private List<MediaSourceProviderInfo> _mediaSourceProvidersInfo;
        private byte[]? _SMTCAlbumArtBytes = null;
        private AlbumArtChangedEventArgs _albumArtChangedEventArgs = new AlbumArtChangedEventArgs();

        public event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;
        public event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        public event EventHandler<AlbumArtChangedEventArgs>? AlbumArtChangedChanged;
        public event EventHandler<MediaSourceProvidersInfoEventArgs>? MediaSourceProvidersInfoChanged;

        public PlaybackService(ISettingsService settingsService, IAlbumArtSearchService albumArtSearchService) : base(settingsService)
        {
            _albumArtSearchService = albumArtSearchService;
            _logger = Ioc.Default.GetRequiredService<ILogger<PlaybackService>>();

            _mediaSourceProvidersInfo = _settingsService.MediaSourceProvidersInfo;
            InitMediaManager();
        }

        private bool IsMediaSourceEnabled(string id)
        {
            return _mediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id)?.IsEnabled ?? true;
        }

        private void InitMediaManager()
        {
            _mediaManager.Start();

            _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            MediaManager_OnFocusedSessionChanged(_mediaManager.GetFocusedSession());
        }

        private void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession mediaSession)
        {
            if (mediaSession == null || !IsMediaSourceEnabled(mediaSession.ControlSession.SourceAppUserModelId))
            {
                SendNullMessages();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var props = await mediaSession.ControlSession.TryGetMediaPropertiesAsync();
                        MediaManager_OnAnyMediaPropertyChanged(mediaSession, props);
                        MediaManager_OnAnyPlaybackStateChanged(mediaSession, mediaSession.ControlSession.GetPlaybackInfo());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "TryGetMediaPropertiesAsync failed");
                        SendNullMessages();
                    }
                });
            }
        }

        private void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties)
        {
            if (!IsMediaSourceEnabled(mediaSession.ControlSession.SourceAppUserModelId) || mediaSession != _mediaManager.GetFocusedSession()) return;

            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                () =>
                {
                    PositionChanged?.Invoke(this, new PositionChangedEventArgs(timelineProperties.Position));
                }
            );
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
        {
            RecordMediaSourceProviderInfo(mediaSession);
            if (!IsMediaSourceEnabled(mediaSession.ControlSession.SourceAppUserModelId) || mediaSession != _mediaManager.GetFocusedSession()) return;

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                () =>
                {
                    IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(playbackInfo.PlaybackStatus switch
                    {
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => true,
                        _ => false,
                    }));
                }
            );
        }

        private void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            _ = _OnAnyMediaPropertyChangedRunner.RunAsync(async token =>
            {
                _logger.LogInformation("Media properties changed: Title: {Title}, Artist: {Artist}, Album: {Album}",
                    mediaProperties.Title, mediaProperties.Artist, mediaProperties.AlbumTitle);

                RecordMediaSourceProviderInfo(mediaSession);
                string id = mediaSession.ControlSession.SourceAppUserModelId;
                if (!IsMediaSourceEnabled(id) || mediaSession != _mediaManager.GetFocusedSession()) return;

                token.ThrowIfCancellationRequested();

                _cachedSongInfo = new SongInfo
                {
                    Title = mediaProperties.Title,
                    Artist = mediaProperties.Artist,
                    Album = mediaProperties.AlbumTitle,
                    DurationMs = mediaSession.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds,
                    SourceAppUserModelId = id,
                };

                if (mediaProperties.Thumbnail is IRandomAccessStreamReference streamReference)
                {
                    _SMTCAlbumArtBytes = await ImageHelper.ToByteArrayAsync(streamReference);
                    token.ThrowIfCancellationRequested();
                }

                _ = _AlbumArtRefreshRunner.RunAsync(async tokne =>
                {
                    await UpdateAlbumArtRelated(tokne);
                });

                if (!token.IsCancellationRequested)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                    () =>
                    {
                        SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                    });
                }
            });
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediaSession)
        {
            if (_mediaManager.CurrentMediaSessions.Count == 0)
            {
                SendNullMessages();
            }
        }

        private void MediaManager_OnAnySessionOpened(MediaManager.MediaSession mediaSession)
        {
            RecordMediaSourceProviderInfo(mediaSession);
        }

        private void RecordMediaSourceProviderInfo(MediaManager.MediaSession mediaSession)
        {
            var id = mediaSession?.ControlSession?.SourceAppUserModelId;
            if (string.IsNullOrEmpty(id)) return;

            var found = _mediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == id);
            if (found == null)
            {
                _mediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id, true));
                _settingsService.MediaSourceProvidersInfo = _mediaSourceProvidersInfo;
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                () =>
                {
                    MediaSourceProvidersInfoChanged?.Invoke(this, new MediaSourceProvidersInfoEventArgs(_mediaSourceProvidersInfo));
                });
            }
        }

        private void SendNullMessages()
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
            () =>
            {
                _cachedSongInfo = null;
                SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(false));
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(TimeSpan.Zero));
            });
        }

        private async Task UpdateAlbumArtRelated(CancellationToken token)
        {
            if (_cachedSongInfo == null)
            {
                _logger.LogWarning("Cached song info is null, cannot update album art.");
                return;
            }

            byte[]? bytes = await _albumArtSearchService.SearchAsync(
                _cachedSongInfo.Title,
                _cachedSongInfo.Artist,
                _cachedSongInfo?.Album ?? string.Empty,
                _SMTCAlbumArtBytes
            );
            token.ThrowIfCancellationRequested();

            if (bytes == null)
            {
                bytes = await ImageHelper.CreateTextPlaceholderBytesAsync($"{_cachedSongInfo!.Artist} - {_cachedSongInfo.Title}", 400, 400);
                token.ThrowIfCancellationRequested();
            }

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            token.ThrowIfCancellationRequested();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();

            _albumArtChangedEventArgs.AlbumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            token.ThrowIfCancellationRequested();

            _albumArtChangedEventArgs.AlbumArtAccentColor = ImageHelper.GetAccentColorsFromByte(bytes).FirstOrDefault();

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
            () =>
            {
                AlbumArtChangedChanged?.Invoke(this, _albumArtChangedEventArgs);
            });
        }

        public async Task PlayAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession.TryPlayAsync();
            }
        }

        public async Task PauseAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession.TryPauseAsync();
            }
        }

        public async Task PreviousAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession.TrySkipPreviousAsync();
            }
        }

        public async Task NextAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession.TrySkipNextAsync();
            }
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.MediaSourceProvidersInfo))
                {
                    _mediaSourceProvidersInfo = [.. message.NewValue];
                    _settingsService.MediaSourceProvidersInfo = _mediaSourceProvidersInfo;
                    MediaManager_OnFocusedSessionChanged(_mediaManager.GetFocusedSession());
                }
            }
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<AlbumArtSearchProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.AlbumArtSearchProvidersInfo))
                {
                    // Album art search providers info changed, re-fetch album art
                    _logger.LogInformation("Album art search providers info changed, refreshing album art.");
                    _ = _AlbumArtRefreshRunner.RunAsync(async tokne =>
                    {
                        await UpdateAlbumArtRelated(tokne);
                    });
                }
            }
        }
    }
}
