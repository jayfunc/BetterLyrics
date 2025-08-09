// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EvtSource;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Dispatching;
using SixLabors.ImageSharp.PixelFormats;
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

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public partial class MediaSessionsService : BaseViewModel, IMediaSessionsService,
        IRecipient<PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>>>
    {
        private readonly IAlbumArtSearchService _albumArtSearchService;
        private readonly ILogger<MediaSessionsService> _logger;

        private double _lxMusicPositionSeconds = 0;
        private double _lxMusicDurationSeconds = 0;

        private bool _cachedIsPlaying = false;
        private TimeSpan _cachedPosition = TimeSpan.Zero;

        private EventSourceReader? _sse = null;

        private readonly MediaManager _mediaManager = new();

        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();
        private readonly LatestOnlyTaskRunner _onAnyMediaPropertyChangedRunner = new();

        private SongInfo? _cachedSongInfo;
        private List<MediaSourceProviderInfo> _mediaSourceProvidersInfo;
        private byte[]? _SMTCAlbumArtBytes = null;
        private int _targetAlbumArtSize = 500;

        public event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        public event EventHandler<TimelineChangedEventArgs>? TimelineChanged;
        public event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        public event EventHandler<AlbumArtChangedEventArgs>? AlbumArtChangedChanged;
        public event EventHandler<MediaSourceProvidersInfoEventArgs>? MediaSourceProvidersInfoChanged;

        public MediaSessionsService(ISettingsService settingsService, IAlbumArtSearchService albumArtSearchService) : base(settingsService)
        {
            _albumArtSearchService = albumArtSearchService;
            _logger = Ioc.Default.GetRequiredService<ILogger<MediaSessionsService>>();

            _mediaSourceProvidersInfo = _settingsService.AppSettings.MediaSourceProvidersInfo;
            InitMediaManager();
        }

        public bool IsPlaying => _cachedIsPlaying;
        public SongInfo? SongInfo => _cachedSongInfo;
        public TimeSpan Position => _cachedPosition;

        private bool IsMediaSourceEnabled(string id)
        {
            return _mediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id)?.IsEnabled ?? true;
        }

        private void InitMediaManager()
        {
            _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            _mediaManager.Start();
            Task.Run(() =>
            {
                MediaManager_OnFocusedSessionChanged(null);
            });
        }

        private void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession? mediaSession)
        {
            if (!_mediaManager.IsStarted) return;

            SendFocusedMessagesAsync().ConfigureAwait(false);
        }

        private void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            var focusedSession = _mediaManager.GetFocusedSession();

            if (mediaSession != focusedSession) return;

            if (!IsMediaSourceEnabled(mediaSession.Id))
            {
                _cachedPosition = TimeSpan.Zero;
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    TimelineChanged?.Invoke(this, new TimelineChangedEventArgs(_cachedPosition, TimeSpan.Zero));
                });
            }
            else
            {
                _cachedPosition = timelineProperties.Position;
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    TimelineChanged?.Invoke(this, new TimelineChangedEventArgs(_cachedPosition, timelineProperties.EndTime));
                });
            }
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            var focusedSession = _mediaManager.GetFocusedSession();

            RecordMediaSourceProviderInfo(mediaSession);
            if (mediaSession != focusedSession) return;

            if (!IsMediaSourceEnabled(mediaSession.Id))
            {
                _cachedIsPlaying = false;
            }
            else
            {
                _cachedIsPlaying = playbackInfo.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => true,
                    _ => false,
                };
            }

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(_cachedIsPlaying));
            });
        }

        private void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            string id = mediaSession.Id;

            var focusedSession = _mediaManager.GetFocusedSession();

            RecordMediaSourceProviderInfo(mediaSession);
            if (mediaSession != focusedSession) return;

            if (!IsMediaSourceEnabled(id))
            {
                _cachedSongInfo = null;

                _onAnyMediaPropertyChangedRunner.RunAsync(async token =>
                {
                    _logger.LogInformation("Media properties changed: Title: {Title}, Artist: {Artist}, Album: {Album}",
                        mediaProperties.Title, mediaProperties.Artist, mediaProperties.AlbumTitle);

                    if (id == Constants.PlayerID.LXMusic)
                    {
                        StopSSE();
                    }

                    _SMTCAlbumArtBytes = null;

                    await _albumArtRefreshRunner.RunAsync(async tokne =>
                    {
                        await UpdateAlbumArtRelated(tokne);
                    });

                    if (!token.IsCancellationRequested)
                    {
                        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                        });
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                _cachedSongInfo = new SongInfo
                {
                    Title = mediaProperties.Title,
                    Artist = mediaProperties.Artist,
                    Album = mediaProperties.AlbumTitle,
                    DurationMs = mediaSession.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds,
                    SourceAppUserModelId = id,
                };

                _cachedSongInfo.Duration = (int)(_cachedSongInfo.DurationMs / 1000f);

                _onAnyMediaPropertyChangedRunner.RunAsync(async token =>
                {
                    _logger.LogInformation("Media properties changed: Title: {Title}, Artist: {Artist}, Album: {Album}",
                        mediaProperties.Title, mediaProperties.Artist, mediaProperties.AlbumTitle);

                    if (id == Constants.PlayerID.LXMusic)
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
                        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                        });
                    }
                }).ConfigureAwait(false);
            }
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediaSession)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            if (_mediaManager.CurrentMediaSessions.Count == 0)
            {
                SendNullMessages();
            }
        }

        private void MediaManager_OnAnySessionOpened(MediaManager.MediaSession mediaSession)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            RecordMediaSourceProviderInfo(mediaSession);
            SendFocusedMessagesAsync().ConfigureAwait(false);
        }

        private void RecordMediaSourceProviderInfo(MediaManager.MediaSession mediaSession)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            var id = mediaSession?.Id;
            if (string.IsNullOrEmpty(id)) return;

            var found = _mediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == id);
            if (found == null)
            {
                _mediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id));
                // 在这里就写进设置
                // 因为 SettingsPageViewModel 可能还没有初始化
                _settingsService.AppSettings.MediaSourceProvidersInfo = _mediaSourceProvidersInfo;
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    MediaSourceProvidersInfoChanged?.Invoke(this, new MediaSourceProvidersInfoEventArgs(_mediaSourceProvidersInfo));
                });
            }
        }

        private void SendNullMessages()
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _cachedSongInfo = null;
                _cachedIsPlaying = false;
                SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(_cachedSongInfo));
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(_cachedIsPlaying));
                TimelineChanged?.Invoke(this, new TimelineChangedEventArgs(TimeSpan.Zero, TimeSpan.Zero));
            });
        }

        private async Task SendFocusedMessagesAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession == null || focusedSession.ControlSession == null) return;

            var mediaProps = await focusedSession.ControlSession.TryGetMediaPropertiesAsync();
            MediaManager_OnAnyTimelinePropertyChanged(focusedSession, focusedSession.ControlSession.GetTimelineProperties());
            MediaManager_OnAnyMediaPropertyChanged(focusedSession, mediaProps);
            MediaManager_OnAnyPlaybackStateChanged(focusedSession, focusedSession.ControlSession.GetPlaybackInfo());
        }

        private async Task UpdateAlbumArtRelated(CancellationToken token)
        {
            if (_cachedSongInfo == null)
            {
                _logger.LogWarning("Cached song info is null, cannot update album art.");
                return;
            }

            byte[]? bytes = await _albumArtSearchService.SearchAsync(
                SongInfo?.SourceAppUserModelId ?? "",
                _cachedSongInfo.Title,
                _cachedSongInfo.Artist,
                _cachedSongInfo?.Album ?? string.Empty,
                _SMTCAlbumArtBytes
            );
            token.ThrowIfCancellationRequested();

            if (bytes == null)
            {
                bytes = await ImageHelper.CreateTextPlaceholderBytesAsync(_targetAlbumArtSize, _targetAlbumArtSize);
                token.ThrowIfCancellationRequested();
            }

            bytes = ImageHelper.Resize(bytes, _targetAlbumArtSize);
            bytes = ImageHelper.MakeSquareWithThemeColor(bytes);

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            token.ThrowIfCancellationRequested();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();

            var albumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            token.ThrowIfCancellationRequested();

            var albumArtLightAccentColor = ImageHelper.GetAccentColorsFromByte(bytes, 1, false).FirstOrDefault();
            var albumArtDarkAccentColor = ImageHelper.GetAccentColorsFromByte(bytes, 1, true).FirstOrDefault();

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                AlbumArtChangedChanged?.Invoke(this, new AlbumArtChangedEventArgs(null, albumArtSwBitmap, albumArtLightAccentColor, albumArtDarkAccentColor));
            });
        }

        private void StartSSE()
        {
            try
            {
                _sse = new EventSourceReader(new Uri($"{_settingsService.AppSettings.LXMusicServer}{Constants.LXMusic.QuerySuffix}")).Start();
                _sse.MessageReceived += Sse_MessageReceived;
                _sse.Disconnected += Sse_Disconnected;
            }
            catch (Exception)
            {
                _logger.LogError("Failed to start SSE connection for LX Music.");
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
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
            if (_cachedSongInfo?.SourceAppUserModelId == Constants.PlayerID.LXMusic)
            {
                var data = JsonSerializer.Deserialize(e.Message, Serialization.SourceGenerationContext.Default.JsonElement);
                if (data.ValueKind == JsonValueKind.Number)
                {
                    if (e.Event == "progress")
                    {
                        _lxMusicPositionSeconds = data.GetDouble();
                    }
                    else if (e.Event == "duration")
                    {
                        _lxMusicDurationSeconds = data.GetDouble();
                    }
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        TimelineChanged?.Invoke(this, new TimelineChangedEventArgs(TimeSpan.FromSeconds(_lxMusicPositionSeconds), TimeSpan.FromSeconds(_lxMusicDurationSeconds)));
                    });
                }
            }
        }

        public async Task PlayAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession?.TryPlayAsync();
            }
        }

        public async Task PauseAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession?.TryPauseAsync();
            }
        }

        public async Task PreviousAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession?.TrySkipPreviousAsync();
            }
        }

        public async Task NextAsync()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession?.TrySkipNextAsync();
            }
        }

        public async Task ChangePosition(double seconds)
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                await focusedSession.ControlSession?.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds).Ticks);
            }
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.MediaSourceProvidersInfo))
                {
                    _mediaSourceProvidersInfo = [.. message.NewValue];
                    MediaManager_OnFocusedSessionChanged(null);
                }
            }
        }
    }
}
