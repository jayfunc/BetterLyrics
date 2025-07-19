// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EvtSource;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
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

        private readonly string _lxMusicId = "cn.toside.music.desktop";

        private bool _cachedIsPlaying = false;

        private EventSourceReader? _sse = null;

        private readonly MediaManager _mediaManager = new();

        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();
        private readonly LatestOnlyTaskRunner _onAnyMediaPropertyChangedRunner = new();

        private SongInfo? _cachedSongInfo;
        private List<MediaSourceProviderInfo> _mediaSourceProvidersInfo;
        private byte[]? _SMTCAlbumArtBytes = null;

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

        public bool IsPlaying => _cachedIsPlaying;
        public SongInfo? SongInfo => _cachedSongInfo;

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
                Task.Run(async () =>
                {
                    try
                    {
                        var props = await mediaSession.ControlSession.TryGetMediaPropertiesAsync();
                        MediaManager_OnAnyMediaPropertyChanged(mediaSession, props);
                        MediaManager_OnAnyPlaybackStateChanged(mediaSession, mediaSession.ControlSession.GetPlaybackInfo());
                    }
                    catch (Exception) { }
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

            _cachedIsPlaying = playbackInfo.PlaybackStatus switch
            {
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => true,
                _ => false,
            };

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                () =>
                {
                    IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(_cachedIsPlaying));
                }
            );
        }

        private async void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            string id = mediaSession.ControlSession.SourceAppUserModelId;

            RecordMediaSourceProviderInfo(mediaSession);
            if (!IsMediaSourceEnabled(id) || mediaSession != _mediaManager.GetFocusedSession()) return;

            _cachedSongInfo = new SongInfo
            {
                Title = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                Album = mediaProperties.AlbumTitle,
                DurationMs = mediaSession.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds,
                SourceAppUserModelId = id,
            };

            await _onAnyMediaPropertyChangedRunner.RunAsync(async token =>
            {
                _logger.LogInformation("Media properties changed: Title: {Title}, Artist: {Artist}, Album: {Album}",
                    mediaProperties.Title, mediaProperties.Artist, mediaProperties.AlbumTitle);

                if (id == _lxMusicId)
                {
                    StartSSE();
                }
                else
                {
                    StopSSE();
                }

                if (mediaProperties.Thumbnail is IRandomAccessStreamReference streamReference)
                {
                    _SMTCAlbumArtBytes = await ImageHelper.ToByteArrayAsync(streamReference);
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    _SMTCAlbumArtBytes = null;
                }

                await _albumArtRefreshRunner.RunAsync(async tokne =>
                {
                    await UpdateAlbumArtRelated(tokne);
                });

                if (!token.IsCancellationRequested)
                {
                    _dispatcherQueue.TryEnqueue(() =>
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
                _cachedIsPlaying = false;
                SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(_cachedIsPlaying));
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
                bytes = await ImageHelper.CreateTextPlaceholderBytesAsync(400, 400);
                token.ThrowIfCancellationRequested();
            }

            bytes = ImageHelper.MakeSquareWithThemeColor(bytes);

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            token.ThrowIfCancellationRequested();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();

            var _albumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            token.ThrowIfCancellationRequested();

            var _albumArtAccentColor = ImageHelper.GetAccentColorsFromByte(bytes).FirstOrDefault();

            _dispatcherQueue.TryEnqueue(() =>
            {
                AlbumArtChangedChanged?.Invoke(this, new AlbumArtChangedEventArgs(_albumArtSwBitmap, _albumArtAccentColor));
            });
        }

        private void StartSSE()
        {
            try
            {
                _sse = new EventSourceReader(new Uri($"{_settingsService.LXMusicServer}/subscribe-player-status?filter=progress")).Start();
                _sse.MessageReceived += Sse_MessageReceived;
                _sse.Disconnected += Sse_Disconnected;
            }
            catch (Exception)
            {
                _logger.LogError("Failed to start SSE connection for LX Music.");
                _dispatcherQueue.TryEnqueue(() =>
                {
                    App.Current.LyricsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("FailToStartLXMusicServer"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                });
                StopSSE();
            }
        }

        private void StopSSE()
        {
            if (_sse != null)
            {
                _sse.MessageReceived -= Sse_MessageReceived;
                _sse.Disconnected -= Sse_Disconnected;
                _sse.Dispose();
                _sse = null;
            }
        }

        private void Sse_Disconnected(object sender, DisconnectEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(e.ReconnectDelay);
                if (_sse != null && !_sse.IsDisposed) _sse.Start();
            });
        }

        private void Sse_MessageReceived(object sender, EventSourceMessageEventArgs e)
        {
            var data = JsonSerializer.Deserialize(e.Message, Serialization.SourceGenerationContext.Default.JsonElement);

            if (data.TryGetDouble(out double positionSeconds))
            {
                if (_cachedSongInfo?.SourceAppUserModelId == _lxMusicId)
                {
                    PositionChanged?.Invoke(this, new PositionChangedEventArgs(TimeSpan.FromSeconds(positionSeconds)));
                }
            }
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

        public async void Receive(PropertyChangedMessage<ObservableCollection<AlbumArtSearchProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.AlbumArtSearchProvidersInfo))
                {
                    // Album art search providers info changed, re-fetch album art
                    _logger.LogInformation("Album art search providers info changed, refreshing album art.");
                    await _albumArtRefreshRunner.RunAsync(async tokne =>
                    {
                        await UpdateAlbumArtRelated(tokne);
                    });
                }
            }
        }
    }
}
