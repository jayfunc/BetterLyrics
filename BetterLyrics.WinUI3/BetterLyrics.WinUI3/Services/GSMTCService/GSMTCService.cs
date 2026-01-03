// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
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
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
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

        private readonly Stopwatch _scrobbleStopwatch = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool CurrentIsPlaying { get; private set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TimeSpan CurrentPosition { get; private set; } = TimeSpan.Zero;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SongInfo? CurrentSongInfo { get; private set; } = SongInfoExtensions.Placeholder;

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

            _onMediaPropsChangedTimer = _dispatcherQueue.CreateTimer();

            _settingsService.AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged += MediaSourceProvidersInfo_ItemPropertyChanged;

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;

            _settingsService.AppSettings.MappedSongSearchQueries.CollectionChanged += MappedSongSearchQueries_CollectionChanged;
            _settingsService.AppSettings.MappedSongSearchQueries.ItemPropertyChanged += MappedSongSearchQueries_ItemPropertyChanged;

            InitMediaManager();
        }

        private void MappedSongSearchQueries_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            UpdateLyrics();
        }

        private void MappedSongSearchQueries_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateLyrics();
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

        private MediaSourceProviderInfo? GetCurrentMediaSourceProviderInfo()
        {
            var desiredSession = GetCurrentSession();
            return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == desiredSession?.Id);
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
            _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            _mediaManager.Start();

            MediaManager_OnFocusedSessionChanged(null);
            _mediaManager.CurrentMediaSessions.ToList().ForEach(x => RecordMediaSourceProviderInfo(x.Value));
        }

        private async void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession? mediaSession)
        {
            if (!_mediaManager.IsStarted) return;

            await SendFocusedMessagesAsync();
        }

        private void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!_mediaManager.IsStarted) return;
                if (mediaSession == null)
                {
                    _scrobbleStopwatch.Reset();
                    CurrentPosition = TimeSpan.Zero;
                    return;
                }

                var desiredSession = GetCurrentSession();

                if (mediaSession != desiredSession) return;

                if (!IsMediaSourceEnabled(mediaSession.Id))
                {
                    _scrobbleStopwatch.Reset();
                    CurrentPosition = TimeSpan.Zero;
                }
                else
                {
                    if (IsMediaSourceTimelineSyncEnabled(mediaSession.Id))
                    {
                        CurrentPosition = timelineProperties?.Position ?? TimeSpan.Zero;
                        CurrentSongInfo?.DurationMs = timelineProperties?.EndTime.TotalMilliseconds ?? 0;
                    }
                }
            });
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!_mediaManager.IsStarted) return;
                if (mediaSession == null)
                {
                    CurrentIsPlaying = false;
                    return;
                }

                var desiredSession = GetCurrentSession();

                if (mediaSession != desiredSession) return;

                if (!IsMediaSourceEnabled(mediaSession.Id))
                {
                    CurrentIsPlaying = false;
                }
                else
                {
                    CurrentIsPlaying = playbackInfo?.PlaybackStatus switch
                    {
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => true,
                        _ => false,
                    };

                    if (CurrentIsPlaying)
                    {
                        _scrobbleStopwatch.Start();
                    }
                    else
                    {
                        _scrobbleStopwatch.Stop();
                    }
                }
            });
        }

        private void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            _onMediaPropsChangedTimer?.Debounce(() =>
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    if (!_mediaManager.IsStarted) return;
                    if (mediaSession == null)
                    {
                        CurrentSongInfo = SongInfoExtensions.Placeholder;
                    }

                    string? sessionId = mediaSession?.Id;

                    var desiredSession = GetCurrentSession();

                    if (mediaSession != desiredSession) return;

                    if (sessionId != null && !IsMediaSourceEnabled(sessionId))
                    {
                        CurrentSongInfo = SongInfoExtensions.Placeholder;

                        if (PlayerIdHelper.IsLXMusic(sessionId))
                        {
                            StopSSE();
                        }

                        _SMTCAlbumArtBuffer = null;
                    }
                    else
                    {
                        var currentMediaSourceProviderInfo = GetCurrentMediaSourceProviderInfo();
                        if (currentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
                        {
                            currentMediaSourceProviderInfo?.PositionOffset = 0;
                        }

                        string? fixedArtist = mediaProperties?.Artist;
                        string? fixedAlbum = mediaProperties?.AlbumTitle;
                        string? songId = null;

                        if (PlayerIdHelper.IsAppleMusic(sessionId))
                        {
                            fixedArtist = mediaProperties?.Artist.Split(" — ").FirstOrDefault();
                            fixedAlbum = mediaProperties?.Artist.Split(" — ").LastOrDefault();
                            fixedAlbum = fixedAlbum?.Replace(" - Single", "");
                            fixedAlbum = fixedAlbum?.Replace(" - EP", "");
                        }
                        else if (PlayerIdHelper.IsNeteaseFamily(sessionId))
                        {
                            songId = mediaProperties?.Genres
                                .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.NetEaseCloudMusicTrackID))?
                                .Replace(ExtendedGenreFiled.NetEaseCloudMusicTrackID, "");
                        }
                        else if (sessionId == PlayerId.QQMusic)
                        {
                            songId = mediaProperties?.Genres
                                .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.QQMusicTrackID))?
                                .Replace(ExtendedGenreFiled.QQMusicTrackID, "");
                        }

                        var linkedFileName = mediaProperties?.Genres
                            .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.FileName))?
                            .Replace(ExtendedGenreFiled.FileName, "");

                        // 写入播放记录
                        if (CurrentSongInfo != null && !string.IsNullOrWhiteSpace(CurrentSongInfo.Title) && CurrentSongInfo.Title != "N/A")
                        {
                            // 必须捕获一个副本给异步任务，因为 CurrentSongInfo 马上就要变了
                            var lastSong = CurrentSongInfo;

                            // 当前秒表时间 >= 上一首总时长 / 2
                            if (lastSong.DurationMs > 0 &&
                                _scrobbleStopwatch.Elapsed.TotalMilliseconds >= (lastSong.DurationMs / 2))
                            {
                                // 写入本地播放记录
                                var playHistoryItem = lastSong.ToPlayHistoryItem(_scrobbleStopwatch.Elapsed.TotalMilliseconds);
                                if (playHistoryItem != null)
                                {
                                    // 后台
                                    _ = Task.Run(() => _playHistoryService.AddLogAsync(playHistoryItem));
                                    _logger.LogInformation($"[Scrobble] 结算成功: {lastSong.Title}");
                                }
                                // 写入 Last.fm 播放记录
                                var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
                                if (isLastFMEnabled)
                                {
                                    // 后台
                                    _ = Task.Run(() => _lastFMService.TrackAsync(lastSong));
                                }
                            }
                        }
                        _scrobbleStopwatch.Restart();

                        CurrentSongInfo = new SongInfo
                        {
                            Title = mediaProperties?.Title ?? "N/A",
                            Artists = fixedArtist?.SplitByCommonSplitter() ?? ["N/A"],
                            Album = fixedAlbum ?? "N/A",
                            DurationMs = mediaSession?.ControlSession?.GetTimelineProperties().EndTime.TotalMilliseconds ?? 0,
                            PlayerId = sessionId,
                            SongId = songId,
                            LinkedFileName = linkedFileName
                        };

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
                        else if (mediaProperties?.Thumbnail is IRandomAccessStreamReference streamReference)
                        {
                            _SMTCAlbumArtBuffer = await ImageHelper.ToBufferAsync(streamReference);
                        }
                        else
                        {
                            _SMTCAlbumArtBuffer = null;
                        }
                    }

                    _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged {SongInfo}", CurrentSongInfo);

                    CurrentMediaSourceProviderInfo = GetCurrentMediaSourceProviderInfo();

                    UpdateAlbumArt();
                    UpdateLyrics();

                    UpdateDiscordPresence();
                    UpdateCurrentMediaSourceProviderInfoPositionOffset();
                });
            }, Time.DebounceTimeout);
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

        private async void MediaManager_OnAnySessionOpened(MediaManager.MediaSession mediaSession)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            RecordMediaSourceProviderInfo(mediaSession);
            await SendFocusedMessagesAsync();
        }

        private MediaManager.MediaSession? GetCurrentSession()
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession == null)
            {
                return null;
            }
            if (IsMediaSourceEnabled(focusedSession.Id))
            {
                return focusedSession;
            }
            else
            {
                foreach (var session in _mediaManager.CurrentMediaSessions.Values)
                {
                    if (IsMediaSourceEnabled(session.Id))
                    {
                        return session;
                    }
                }
            }
            return null;
        }

        private void RecordMediaSourceProviderInfo(MediaManager.MediaSession mediaSession)
        {
            if (!_mediaManager.IsStarted) return;
            if (mediaSession == null) return;

            var id = mediaSession?.Id;
            if (string.IsNullOrEmpty(id)) return;

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == id);
                if (found == null)
                {
                    _settingsService.AppSettings.MediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id, _settingsService.AppSettings.GeneralSettings.ListenOnNewPlaybackSource));
                }
            });
        }

        private void SendNullMessages()
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
            {
                CurrentSongInfo = SongInfoExtensions.Placeholder;
                CurrentIsPlaying = false;

                CurrentMediaSourceProviderInfo = GetCurrentMediaSourceProviderInfo();

                _scrobbleStopwatch.Reset();
                CurrentPosition = TimeSpan.Zero;

                _discordService.Disable();
                UpdateCurrentMediaSourceProviderInfoPositionOffset();
            }));
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

        private async Task SendFocusedMessagesAsync()
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;

            var desiredSession = GetCurrentSession();

            try
            {
                mediaProps = await desiredSession?.ControlSession?.TryGetMediaPropertiesAsync();
            }
            catch (Exception) { }

            MediaManager_OnAnyTimelinePropertyChanged(desiredSession, desiredSession?.ControlSession?.GetTimelineProperties());
            MediaManager_OnAnyMediaPropertyChanged(desiredSession, mediaProps);
            MediaManager_OnAnyPlaybackStateChanged(desiredSession, desiredSession?.ControlSession?.GetPlaybackInfo());
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
                if (PlayerIdHelper.IsLXMusic(CurrentSongInfo?.PlayerId))
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
                            CurrentSongInfo?.DurationMs = data.GetDouble() * 1000;
                            UpdateDiscordPresence();
                        }

                        if (IsMediaSourceTimelineSyncEnabled(CurrentSongInfo?.PlayerId))
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
            var desiredSession = GetCurrentSession();
            if (desiredSession != null)
            {
                await desiredSession.ControlSession?.TryPlayAsync();
            }
        }

        public async Task PauseAsync()
        {
            var desiredSession = GetCurrentSession();
            if (desiredSession != null)
            {
                await desiredSession.ControlSession?.TryPauseAsync();
            }
        }

        public async Task PreviousAsync()
        {
            var desiredSession = GetCurrentSession();
            if (desiredSession != null)
            {
                await desiredSession.ControlSession?.TrySkipPreviousAsync();
            }
        }

        public async Task NextAsync()
        {
            var desiredSession = GetCurrentSession();
            if (desiredSession != null)
            {
                await desiredSession.ControlSession?.TrySkipNextAsync();
            }
        }

        public async Task ChangePosition(double seconds)
        {
            var desiredSession = GetCurrentSession();
            if (desiredSession != null)
            {
                await desiredSession.ControlSession?.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds).Ticks);
            }
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
                TaskbarList.SetProgressValue(hwnd, (ulong)value.TotalSeconds, (ulong)(CurrentSongInfo?.Duration ?? value.TotalSeconds));
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaSourceProviderInfo)
            {
                if (message.PropertyName == nameof(MediaSourceProviderInfo.IsEnabled))
                {
                    MediaManager_OnFocusedSessionChanged(null);
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
                    MediaManager_OnFocusedSessionChanged(null);
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
                else if (message.PropertyName == nameof(TranslationSettings.CutletDockerServer))
                {
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
