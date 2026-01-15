// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.DiscordService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.PlayHistoryService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslationService;
using BetterLyrics.WinUI3.Services.TransliterationService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using EvtSource;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : BaseViewModel, IGSMTCService,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<ChineseRomanization>>,
        IRecipient<PropertyChangedMessage<DateTime?>>
    {
        private EventSourceReader? _sse = null;
        private readonly MediaManager _mediaManager = new();
        private IBuffer? _SMTCAlbumArtBuffer = null;

        private MediaManager.MediaSession? _currentDesiredSession = null;

        private readonly IAlbumArtSearchService _albumArtSearchService;
        private readonly ILyricsSearchService _lyrcsSearchService;
        private readonly ITranslationService _translationService;
        private readonly ITransliterationService _transliterationService;
        private readonly ISettingsService _settingsService;
        private readonly IDiscordService _discordService;
        private readonly IPlayHistoryService _playHistoryService;
        private readonly ILastFMService _lastFMService;
        private readonly ILogger<GSMTCService> _logger;

        private double _lxMusicPositionSeconds = 0;
        private byte[]? _lxMusicAlbumArtBytes = null;

        private readonly DispatcherQueueTimer? _onMediaPropsChangedTimer;
        private readonly DispatcherTimer _scrobbleTimer;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsScrobbled { get; set; } = false;
        [ObservableProperty] public partial TimeSpan ScrobbledDuration { get; set; } = TimeSpan.Zero;
        [ObservableProperty] public partial TimeSpan TargetScrobbledDuration { get; set; } = TimeSpan.Zero;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool CurrentIsPlaying { get; private set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TimeSpan CurrentPosition { get; private set; } = TimeSpan.Zero;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SongInfo CurrentSongInfo { get; private set; } = SongInfoExtensions.Placeholder;

        [ObservableProperty] public partial MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; set; }

        public GSMTCService(
            ISettingsService settingsService,
            IAlbumArtSearchService albumArtSearchService,
            ILyricsSearchService lyricsSearchService,
            IDiscordService discordService,
            ITranslationService libreTranslateService,
            ITransliterationService transliterationService,
            IPlayHistoryService playHistoryService,
            ILastFMService lastFMService,
            ILogger<GSMTCService> logger)
        {
            _settingsService = settingsService;
            _albumArtSearchService = albumArtSearchService;
            _lyrcsSearchService = lyricsSearchService;
            _translationService = libreTranslateService;
            _transliterationService = transliterationService;
            _discordService = discordService;
            _playHistoryService = playHistoryService;
            _lastFMService = lastFMService;
            _logger = logger;

            _scrobbleTimer = new();
            _scrobbleTimer.Interval = TimeSpan.FromSeconds(1);
            _scrobbleTimer.Tick += ScrobbleTimer_Tick;

            _onMediaPropsChangedTimer = _dispatcherQueue.CreateTimer();

            _settingsService.AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged += MediaSourceProvidersInfo_ItemPropertyChanged;

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;

            InitMediaManager();
        }

        private void ScrobbleTimer_Tick(object? sender, object e)
        {
            if (!IsScrobbled)
            {
                if (!string.IsNullOrWhiteSpace(CurrentSongInfo.Title) && CurrentSongInfo.Title != "N/A")
                {
                    ScrobbledDuration += _scrobbleTimer.Interval;
                    if (ScrobbledDuration >= TargetScrobbledDuration)
                    {
                        // 写入本地播放记录
                        var playHistoryItem = CurrentSongInfo.ToPlayHistoryItem(ScrobbledDuration.TotalMilliseconds);
                        if (playHistoryItem != null)
                        {
                            // 后台
                            _ = Task.Run(async () =>
                            {
                                await _playHistoryService.AddLogAsync(playHistoryItem);
                            });
                            _logger.LogInformation("ScrobbleTimer_Tick: {} scrobbled", CurrentSongInfo.Title);
                        }
                        // 写入 Last.fm 播放记录
                        var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
                        if (isLastFMEnabled)
                        {
                            // 后台
                            _ = Task.Run(() => _lastFMService.TrackAsync(CurrentSongInfo));
                        }

                        IsScrobbled = true;
                        ScrobbledDuration = TimeSpan.Zero;
                    }
                }
            }
        }

        private void LocalMediaFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAlbumArt();
            UpdateLyrics();
        }

        private void MediaSourceProvidersInfo_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MediaSourceProviderInfo.AlbumArtSearchProvidersInfo):
                    UpdateAlbumArt();
                    break;
                case nameof(MediaSourceProviderInfo.LyricsSearchProvidersInfo):
                    UpdateLyrics();
                    break;
                case nameof(MediaSourceProviderInfo.LyricsSearchType):
                    UpdateLyrics();
                    break;
                case nameof(MediaSourceProviderInfo.MatchingThreshold):
                    UpdateLyrics();
                    break;
                default:
                    break;
            }
        }

        private MediaSourceProviderInfo? GetCurrentDesiredMediaSourceProviderInfo()
        {
            return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == _currentDesiredSession?.Id);
        }

        private bool IsMediaSourceEnabled(string id)
        {
            var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id);
            if (_settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus.IsOpened)
            {
                if (PlayerIdHelper.IsBetterLyrics(found?.Provider))
                {
                    return found?.IsEnabled ?? true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return found?.IsEnabled ?? true;
            }
        }

        private bool IsMediaSourceTimelineSyncEnabled(string? id)
        {
            return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id)?.IsTimelineSyncEnabled ?? true;
        }

        private void InitMediaManager()
        {
            _mediaManager.Start();
            _mediaManager.CurrentMediaSessions.ToList().ForEach(x => RecordMediaSession(x.Value.Id));

            _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            OnDesiredSessionChanged(true);
        }

        private void OnDesiredSessionChanged(bool firstTime = false)
        {
            var desiredSession = GetCurrentDesiredSession();
            if (firstTime || desiredSession != _currentDesiredSession)
            {
                _currentDesiredSession = desiredSession;
                if (_currentDesiredSession == null)
                {
                    SendNullMessages();
                }
                else
                {
                    _ = SendFocusedMessagesAsync();
                }
            }
        }

        private void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession? mediaSession)
        {
            OnDesiredSessionChanged();
        }

        private void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (mediaSession != _currentDesiredSession) return;

                CurrentPosition = timelineProperties.Position;
                CurrentSongInfo.DurationMs = timelineProperties.EndTime.TotalMilliseconds;
                UpdateTargetScrobbledDuration();
                if (CurrentPosition.TotalSeconds == 0)
                {
                    IsScrobbled = false;
                    ScrobbledDuration = TimeSpan.Zero;
                }
            });
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (mediaSession != _currentDesiredSession) return;

                CurrentIsPlaying = playbackInfo.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => true,
                    _ => false,
                };

                if (CurrentIsPlaying)
                {
                    _scrobbleTimer.Start();
                }
                else
                {
                    _scrobbleTimer.Stop();
                }
            });
        }

        private void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            _onMediaPropsChangedTimer?.Debounce(() =>
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    if (mediaSession != _currentDesiredSession) return;

                    string sessionId = mediaSession.Id;

                    var currentMediaSourceProviderInfo = GetCurrentDesiredMediaSourceProviderInfo();
                    if (currentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
                    {
                        currentMediaSourceProviderInfo?.PositionOffset = 0;
                    }

                    string fixedTitle = mediaProperties.Title;
                    string fixedArtist = mediaProperties.Artist;
                    string fixedAlbum = mediaProperties.AlbumTitle;
                    string? songId = null;

                    if (PlayerIdHelper.IsAppleMusic(sessionId))
                    {
                        fixedArtist = mediaProperties.Artist.Split(" — ").First();
                        fixedAlbum = mediaProperties.Artist.Split(" — ").Last();
                        fixedAlbum = fixedAlbum.Replace(" - Single", "");
                        fixedAlbum = fixedAlbum.Replace(" - EP", "");
                    }
                    else if (PlayerIdHelper.IsNeteaseFamily(sessionId))
                    {
                        songId = mediaProperties.Genres
                            .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.NetEaseCloudMusicTrackID))?
                            .Replace(ExtendedGenreFiled.NetEaseCloudMusicTrackID, "");
                    }
                    else if (sessionId == PlayerId.QQMusic)
                    {
                        songId = mediaProperties.Genres
                            .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.QQMusicTrackID))?
                            .Replace(ExtendedGenreFiled.QQMusicTrackID, "");
                    }

                    var linkedFileName = mediaProperties.Genres
                        .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.FileName))?
                        .Replace(ExtendedGenreFiled.FileName, "");

                    CurrentSongInfo = new()
                    {
                        Title = fixedTitle,
                        Artist = fixedArtist,
                        Album = fixedAlbum,
                        DurationMs = mediaSession.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds,
                        PlayerId = sessionId,
                        SongId = songId,
                        LinkedFileName = linkedFileName,
                        StartedAt = DateTime.Now.ToBinary(),
                    };

                    UpdateTargetScrobbledDuration();
                    IsScrobbled = false;
                    ScrobbledDuration = TimeSpan.Zero;

                    if (PlayerIdHelper.IsLXMusic(sessionId))
                    {
                        StartSSE();
                    }
                    else
                    {
                        StopSSE();
                    }

                    if (PlayerIdHelper.IsLXMusic(sessionId) && _lxMusicAlbumArtBytes != null)
                    {
                        _SMTCAlbumArtBuffer = _lxMusicAlbumArtBytes.AsBuffer();
                    }
                    else if (mediaProperties.Thumbnail is IRandomAccessStreamReference streamReference)
                    {
                        _SMTCAlbumArtBuffer = await ImageHelper.ToBufferAsync(streamReference);
                    }
                    else
                    {
                        _SMTCAlbumArtBuffer = null;
                    }

                    _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged {SongInfo}", CurrentSongInfo);

                    CurrentMediaSourceProviderInfo = GetCurrentDesiredMediaSourceProviderInfo();

                    UpdateAlbumArt();
                    UpdateLyrics();

                    UpdateDiscordPresence();
                    UpdateCurrentMediaSourceProviderInfoPositionOffset();
                });
            }, Time.DebounceTimeout);
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediaSession)
        {
            if (mediaSession == null) return;

            OnDesiredSessionChanged();
        }

        private void MediaManager_OnAnySessionOpened(MediaManager.MediaSession mediaSession)
        {
            if (mediaSession == null) return;

            var id = mediaSession.Id;

            _dispatcherQueue.TryEnqueue(() =>
            {
                RecordMediaSession(id);
                OnDesiredSessionChanged();
            });
        }

        private void RecordMediaSession(string id)
        {
            var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == id);
            if (found == null)
            {
                _settingsService.AppSettings.MediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id, _settingsService.AppSettings.GeneralSettings.ListenOnNewPlaybackSource));
            }
        }

        private MediaManager.MediaSession? GetCurrentDesiredSession()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null && IsMediaSourceEnabled(focusedSession.Id))
            {
                return focusedSession;
            }

            foreach (var session in _mediaManager.CurrentMediaSessions.Values)
            {
                if (IsMediaSourceEnabled(session.Id))
                {
                    return session;
                }
            }
            return null;
        }

        private void SendNullMessages()
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CurrentSongInfo = SongInfoExtensions.Placeholder;
                CurrentIsPlaying = false;
                CurrentPosition = TimeSpan.Zero;

                UpdateAlbumArt();
                UpdateLyrics();

                _scrobbleTimer.Stop();
                _discordService.Disable();
            });
        }

        private void UpdateCurrentMediaSourceProviderInfoPositionOffset()
        {
            if (CurrentPosition.TotalSeconds <= 1 && CurrentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
            {
                CurrentMediaSourceProviderInfo?.PositionOffset = 0;
            }
        }

        private void UpdateDiscordPresence()
        {
            if (CurrentMediaSourceProviderInfo?.IsDiscordPresenceEnabled == true && CurrentSongInfo != null)
            {
                _discordService.Enable();
                _discordService.UpdateRichPresence(CurrentSongInfo);
            }
            else
            {
                _discordService.Disable();
            }
        }

        private void UpdateTargetScrobbledDuration()
        {
            TargetScrobbledDuration = TimeSpan.FromSeconds(CurrentSongInfo.Duration == 0 ? 30 : CurrentSongInfo.Duration / 2);
        }

        private async Task SendFocusedMessagesAsync()
        {
            if (_currentDesiredSession == null)
            {
                SendNullMessages();
                return;
            }

            try
            {
                var mediaProps = await _currentDesiredSession.ControlSession?.TryGetMediaPropertiesAsync();
                var timelineProps = _currentDesiredSession.ControlSession?.GetTimelineProperties();
                var playbackInfo = _currentDesiredSession.ControlSession?.GetPlaybackInfo();

                if (mediaProps == null || timelineProps == null || playbackInfo == null)
                {
                    SendNullMessages();
                    return;
                }

                MediaManager_OnAnyTimelinePropertyChanged(_currentDesiredSession, timelineProps);
                MediaManager_OnAnyMediaPropertyChanged(_currentDesiredSession, mediaProps);
                MediaManager_OnAnyPlaybackStateChanged(_currentDesiredSession, playbackInfo);
            }
            catch (Exception)
            {
                SendNullMessages();
            }
        }

        private void StartSSE()
        {
            if (_sse != null)
            {
                return;
            }

            try
            {
                _sse = new EventSourceReader(new Uri($"{_settingsService.AppSettings.GeneralSettings.LXMusicServer}{Constants.LXMusic.QuerySuffix}")).Start();
                _sse.MessageReceived += Sse_MessageReceived;
                _sse.Disconnected += Sse_Disconnected;
            }
            catch (Exception)
            {
                _logger.LogError("Failed to start SSE connection for LX Music.");
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    ToastHelper.ShowToast("FailToStartLXMusicServer", null, InfoBarSeverity.Error);
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
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                await Task.Delay(e.ReconnectDelay);
                if (_sse != null && !_sse.IsDisposed) _sse.Start();
            });
        }

        private void Sse_MessageReceived(object sender, EventSourceMessageEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                if (PlayerIdHelper.IsLXMusic(CurrentSongInfo.PlayerId))
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
                            CurrentSongInfo.DurationMs = data.GetDouble() * 1000;
                            UpdateDiscordPresence();
                        }

                        if (IsMediaSourceTimelineSyncEnabled(CurrentSongInfo.PlayerId))
                        {
                            CurrentPosition = TimeSpan.FromSeconds(_lxMusicPositionSeconds);
                        }
                    }
                    else if (data.ValueKind == JsonValueKind.String)
                    {
                        if (e.Event == "picUrl")
                        {
                            string? picUrl = data.GetString();
                            if (picUrl != null)
                            {
                                _logger.LogInformation("LX Music Album Art URL: {url}", picUrl);
                                _lxMusicAlbumArtBytes = await ImageHelper.GetImageByteArrayFromUrlAsync(picUrl);
                                if (_lxMusicAlbumArtBytes != null)
                                {
                                    _SMTCAlbumArtBuffer = _lxMusicAlbumArtBytes.AsBuffer();
                                }
                                else
                                {
                                    _SMTCAlbumArtBuffer = null;
                                }
                                UpdateAlbumArt();
                            }
                        }
                    }
                }
            });
        }

        public async Task PlayAsync()
        {
            await _currentDesiredSession?.ControlSession?.TryPlayAsync();
        }

        public async Task PauseAsync()
        {
            await _currentDesiredSession?.ControlSession?.TryPauseAsync();
        }

        public async Task PreviousAsync()
        {
            await _currentDesiredSession?.ControlSession?.TrySkipPreviousAsync();
        }

        public async Task NextAsync()
        {
            await _currentDesiredSession?.ControlSession?.TrySkipNextAsync();
        }

        public async Task ChangePosition(double seconds)
        {
            await _currentDesiredSession?.ControlSession?.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds).Ticks);
        }

        public async Task ChangeLyricsLine(int index)
        {
            if (CurrentLyricsData?.LyricsLines?.ElementAtOrDefault(index)?.StartMs is int startMs)
            {
                await ChangePosition(startMs / 1000.0);
            }
        }

        partial void OnCurrentIsPlayingChanged(bool value)
        {
            if (WindowHook.GetWindowHandle<NowPlayingWindow>() is IntPtr hwnd)
            {
                TaskbarList.SetProgressState(hwnd, value ? TaskbarButtonProgressState.Normal : TaskbarButtonProgressState.Paused);
            }
        }

        partial void OnCurrentPositionChanged(TimeSpan value)
        {
            if (WindowHook.GetWindowHandle<NowPlayingWindow>() is IntPtr hwnd)
            {
                TaskbarList.SetProgressValue(hwnd, (ulong)value.TotalSeconds, (ulong)(CurrentSongInfo.Duration));
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaSourceProviderInfo)
            {
                if (message.PropertyName == nameof(MediaSourceProviderInfo.IsEnabled))
                {
                    OnDesiredSessionChanged();
                }
            }
            else if (message.Sender is TranslationSettings)
            {
                if (message.PropertyName == nameof(TranslationSettings.IsLibreTranslateEnabled))
                {
                    UpdateLyrics();
                }
                else if (message.PropertyName == nameof(TranslationSettings.IsTranslationEnabled))
                {
                    UpdateLyrics();
                }
                else if (message.PropertyName == nameof(TranslationSettings.IsChineseRomanizationEnabled))
                {
                    UpdateLyrics();
                }
                else if (message.PropertyName == nameof(TranslationSettings.IsJapaneseRomanizationEnabled))
                {
                    UpdateLyrics();
                }
                else if (message.PropertyName == nameof(TranslationSettings.IsTraditionalChineseEnabled))
                {
                    UpdateLyrics();
                }
            }
            else if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(MusicGallerySettings.LyricsWindowStatus.IsOpened))
                {
                    OnDesiredSessionChanged();
                }
            }
            else if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                {
                    UpdateAlbumArt();
                    UpdateLyrics();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is TranslationSettings)
            {
                if (message.PropertyName == nameof(TranslationSettings.SelectedTargetLanguageCode))
                {
                    _logger.LogInformation("Target LibreTranslate language code changed: {code}", _settingsService.AppSettings.TranslationSettings.SelectedTargetLanguageCode);
                    UpdateLyrics();
                }
                else if (message.PropertyName == nameof(TranslationSettings.LibreTranslateServer))
                {
                    UpdateLyrics();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ChineseRomanization> message)
        {
            if (message.Sender is TranslationSettings)
            {
                if (message.PropertyName == nameof(TranslationSettings.ChineseRomanization))
                {
                    UpdateLyrics();
                }
            }
        }

        public void Receive(PropertyChangedMessage<DateTime?> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.LastSyncTime))
                {
                    UpdateAlbumArt();
                    UpdateLyrics();
                }
            }
        }
    }
}
