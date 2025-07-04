// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;
using static Lyricify.Lyrics.Providers.Web.Musixmatch.GetTokenResponse;

namespace BetterLyrics.WinUI3.Services
{
    public partial class PlaybackService : BaseViewModel, IPlaybackService, IRecipient<PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>>>
    {
        private readonly IMusicSearchService _musicSearchService;

        private readonly MediaManager _mediaManager = new();

        private CancellationTokenSource? _mediaPropsCts;

        private List<MediaSourceProviderInfo> _mediaSourceProvidersInfo;

        public event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;
        public event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        public event EventHandler<MediaSourceProvidersInfoEventArgs>? MediaSourceProvidersInfoChanged;

        public PlaybackService(ISettingsService settingsService, IMusicSearchService musicSearchService) : base(settingsService)
        {
            _musicSearchService = musicSearchService;
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

        private async void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession mediaSession)
        {
            if (mediaSession == null || !IsMediaSourceEnabled(mediaSession.ControlSession.SourceAppUserModelId))
            {
                SendNullMessages();
            }
            else
            {
                MediaManager_OnAnyMediaPropertyChanged(mediaSession, await mediaSession.ControlSession.TryGetMediaPropertiesAsync());
                MediaManager_OnAnyPlaybackStateChanged(mediaSession, mediaSession.ControlSession.GetPlaybackInfo());
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

        private async void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            string id = mediaSession.ControlSession.SourceAppUserModelId;
            if (!IsMediaSourceEnabled(id) || mediaSession != _mediaManager.GetFocusedSession()) return;

            _mediaPropsCts?.Cancel();
            var cts = new CancellationTokenSource();
            _mediaPropsCts = cts;
            var token = cts.Token;

            try
            {
                SongInfo? songInfo;

                token.ThrowIfCancellationRequested();

                songInfo = new SongInfo
                {
                    Title = mediaProperties.Title,
                    Artist = mediaProperties.Artist,
                    Album = mediaProperties.AlbumTitle,
                    DurationMs = mediaSession.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds,
                    SourceAppUserModelId = id,
                };

                byte[] bytes;

                if (mediaProperties.Thumbnail is IRandomAccessStreamReference streamReference)
                {
                    bytes = await ImageHelper.ToByteArrayAsync(
                        streamReference
                    );
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    bytes = await _musicSearchService.SearchAlbumArtAsync(
                        songInfo.Title,
                        songInfo.Artist,
                        songInfo.Album
                    );
                    token.ThrowIfCancellationRequested();
                }

                var decoder = await ImageHelper.GetDecoderFromByte(bytes);
                token.ThrowIfCancellationRequested();
                songInfo.AlbumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                token.ThrowIfCancellationRequested();
                songInfo.AlbumArtAccentColor = ImageHelper.GetAccentColorsFromByte(bytes).FirstOrDefault();
                if (!token.IsCancellationRequested)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
                        () =>
                        {
                            SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(songInfo));
                        });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
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
            string id = mediaSession.ControlSession.SourceAppUserModelId;
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
                SongInfoChanged?.Invoke(this, new SongInfoChangedEventArgs(null));
                IsPlayingChanged?.Invoke(this, new IsPlayingChangedEventArgs(false));
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(TimeSpan.Zero));
            });
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
    }
}
