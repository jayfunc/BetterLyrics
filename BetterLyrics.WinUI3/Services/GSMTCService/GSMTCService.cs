// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Memory;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EvtSource;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Windows.Media.Control;
using Windows.Storage.Streams;
using BetterLyrics.Core.ViewModels;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : BaseViewModel, IGSMTCService,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<ChineseRomanization>>,
        IRecipient<PropertyChangedMessage<DateTime?>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<WindowStatus>>,
        IRecipient<PropertyChangedMessage<ChineseConversion>>
    {
        private EventSourceReader? _lxMusicSse = null;
        private UniversalMemoryReader? _memoryReader = null;

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

        private readonly Debouncer _onMediaPropsChangedDebouncer = new();
        private readonly DispatcherTimer _scrobbleTimer;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsScrobbled { get; set; } = false;
        [ObservableProperty] public partial TimeSpan ScrobbledDuration { get; set; } = TimeSpan.Zero;
        [ObservableProperty] public partial TimeSpan TargetScrobbledDuration { get; set; } = TimeSpan.Zero;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool CurrentIsPlaying { get; private set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TimeSpan CurrentPosition { get; private set; } = TimeSpan.Zero;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SongInfo CurrentSongInfo { get; private set; } = SongInfoExtensions.Placeholder;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; set; }

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

            // For dev only
            //var memoryReaderConfig = new MemoryReaderConfig
            //{
            //    ProcessName = "",
            //    Is64Bit = true,
            //    CurrentTime = new MemoryAddressDefinition
            //    {
            //        ModuleName = "",
            //        BaseOffset = 0x,
            //        PointerOffsets = [0x],
            //        ValueType = MemoryValueType.Int32,
            //        UnitScale = 0.001
            //    },
            //    TotalDuration = new MemoryAddressDefinition
            //    {
            //        ModuleName = "",
            //        BaseOffset = 0x,
            //        PointerOffsets = [0x],
            //        ValueType = MemoryValueType.Int32,
            //        UnitScale = 0.001
            //    }
            //};
            //var test = JsonSerializer.Serialize(memoryReaderConfig, Serialization.SourceGenerationContext.Default.MemoryReaderConfig);

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
                            _logger.LogInformation("ScrobbleTimer_Tick: {Title} scrobbled to local stat", CurrentSongInfo.Title);
                        }
                        // 写入 Last.fm 播放记录
                        var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
                        if (isLastFMEnabled)
                        {
                            // 后台
                            _ = Task.Run(() => _lastFMService.TrackAsync(CurrentSongInfo));
                            _logger.LogInformation("ScrobbleTimer_Tick: {Title} scrobbled to last.fm", CurrentSongInfo.Title);
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
            return found?.IsEnabled ?? true;
        }

        private bool IsMediaSourceTimelineSyncEnabled(string? id)
        {
            return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id)?.IsTimelineSyncEnabled ?? true;
        }

        private void InitMediaManager()
        {
            // 经反馈，某些用户环境下 MediaManager.Start() 会抛出异常，暂时捕获并提示，避免程序崩溃
            try
            {
                _mediaManager.Start();
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, MessageSeverity.Error);
                return;
            }

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
                _ = SendFocusedMessagesAsync();
            }
        }

        private void OnAnyTimelineChangedCore(MediaSession? mediaSession, TimeSpan? currentPosition, TimeSpan? duration)
        {
            AppUIThread.Execute(() =>
            {
                if (mediaSession != _currentDesiredSession) return;

                CurrentPosition = currentPosition ?? TimeSpan.Zero;
                CurrentSongInfo.DurationMs = duration?.TotalMilliseconds ?? 0;
                UpdateTargetScrobbledDuration();
                if (CurrentPosition.TotalSeconds == 0)
                {
                    IsScrobbled = false;
                    ScrobbledDuration = TimeSpan.Zero;
                }
            });
        }

        private void MediaManager_OnFocusedSessionChanged(MediaSession? mediaSession)
        {
            OnDesiredSessionChanged();
        }

        private void MediaManager_OnAnyTimelinePropertyChanged(MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties)
        {
            OnAnyTimelineChangedCore(mediaSession, timelineProperties?.Position, timelineProperties?.EndTime);
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
        {
            AppUIThread.Execute(() =>
            {
                if (mediaSession != _currentDesiredSession) return;

                CurrentIsPlaying = playbackInfo?.PlaybackStatus switch
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

        private void MediaManager_OnAnyMediaPropertyChanged(MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            _ = _onMediaPropsChangedDebouncer.RunAsync(() =>
            {
                _ = OnAnyMediaPropertyChangedCoreAsync(mediaSession, mediaProperties);
            }, 1000);
        }

        private void MediaManager_OnAnySessionClosed(MediaSession mediaSession)
        {
            if (mediaSession == null) return;

            OnDesiredSessionChanged();
        }

        private void MediaManager_OnAnySessionOpened(MediaSession mediaSession)
        {
            if (mediaSession == null) return;

            var id = mediaSession.Id;

            AppUIThread.Execute(() =>
            {
                RecordMediaSession(id);
                OnDesiredSessionChanged();
            });
        }

        private async Task OnAnyMediaPropertyChangedCoreAsync(MediaSession? mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaSession != _currentDesiredSession) return;

            string? sessionId = mediaSession?.Id;

            var currentMediaSourceProviderInfo = GetCurrentDesiredMediaSourceProviderInfo();
            if (currentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
            {
                currentMediaSourceProviderInfo?.PositionOffset = 0;
            }

            // 处理歌曲信息
            string? fixedTitle = mediaProperties?.Title;
            string? fixedArtist = mediaProperties?.Artist;
            string? fixedAlbum = mediaProperties?.AlbumTitle;
            string? songId = null;

            if (PlayerIdHelper.IsAppleMusic(sessionId))
            {
                fixedArtist = mediaProperties?.Artist.Split(" — ").First();
                fixedAlbum = mediaProperties?.Artist.Split(" — ").Last();
                fixedAlbum = fixedAlbum?.Replace(" - Single", "");
                fixedAlbum = fixedAlbum?.Replace(" - EP", "");
            }
            else if (PlayerIdHelper.IsNeteaseFamily(sessionId))
            {
                songId = mediaProperties?.Genres
                    .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.NetEaseCloudMusicTrackID))?
                    .Replace(ExtendedGenreFiled.NetEaseCloudMusicTrackID, "");
            }
            else if (PlayerIdHelper.IsQQFamily(sessionId))
            {
                songId = mediaProperties?.Genres
                    .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.QQMusicTrackID))?
                    .Replace(ExtendedGenreFiled.QQMusicTrackID, "");
            }

            var linkedFileName = mediaProperties?.Genres
                .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.FileName))?
                .Replace(ExtendedGenreFiled.FileName, "");

            HandleLXMusicIfDetected(sessionId);

            // 总是先回收 _memoryReader
            _memoryReader?.OnProgressChanged -= UniversalMemoryReader_OnProgressChanged;
            _memoryReader?.Dispose();
            _memoryReader = null;

            // 注册
            if (currentMediaSourceProviderInfo?.IsMemoryReaderEnabled == true)
            {
                if (currentMediaSourceProviderInfo.MemoryReaderConfig is MemoryReaderConfig config)
                {
                    _memoryReader = new(config);
                    _memoryReader.Start();
                    _memoryReader.OnProgressChanged += UniversalMemoryReader_OnProgressChanged;
                }
            }

            // 处理专辑图片
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

            AppUIThread.Execute(() =>
            {
                CurrentSongInfo = new()
                {
                    Title = fixedTitle ?? "N/A",
                    Artist = fixedArtist ?? "N/A",
                    Album = fixedAlbum ?? "N/A",
                    DurationMs = mediaSession?.ControlSession.GetTimelineProperties().EndTime.TotalMilliseconds ?? 0,
                    PlayerId = sessionId,
                    SongId = songId,
                    LinkedFileName = linkedFileName,
                    StartedAt = DateTime.Now.ToBinary(),
                };

                UpdateTargetScrobbledDuration();
                IsScrobbled = false;
                ScrobbledDuration = TimeSpan.Zero;

                CurrentMediaSourceProviderInfo = currentMediaSourceProviderInfo;
                UpdateCurrentMediaSourceProviderInfoPositionOffset();
                UpdateDiscordPresence();

                UpdateLyrics();
                UpdateAlbumArt();

                _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged {SongInfo}", CurrentSongInfo);
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
            // 检查内置播放器会话是否存在
            var selfSession = _mediaManager.CurrentMediaSessions.FirstOrDefault(x => PlayerIdHelper.IsBetterLyrics(x.Key));
            var selfSessionKey = selfSession.Key;
            // 合法且设置中处于启用状态则
            if (!string.IsNullOrEmpty(selfSessionKey) && IsMediaSourceEnabled(selfSessionKey))
            {
                // 直接返回，即使当前聚焦的会话非内置播放器
                return selfSession.Value;
            }

            // 若音乐库处于开启状态且未开启内置播放源会话
            if (_settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus == WindowStatus.Opened)
            {
                return null;
            }

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
            try
            {
                var mediaProps = await _currentDesiredSession?.ControlSession?.TryGetMediaPropertiesAsync();
                var timelineProps = _currentDesiredSession?.ControlSession?.GetTimelineProperties();
                var playbackInfo = _currentDesiredSession?.ControlSession?.GetPlaybackInfo();

                MediaManager_OnAnyTimelinePropertyChanged(_currentDesiredSession, timelineProps);
                MediaManager_OnAnyMediaPropertyChanged(_currentDesiredSession, mediaProps);
                MediaManager_OnAnyPlaybackStateChanged(_currentDesiredSession, playbackInfo);
            }
            catch (Exception)
            {
                MediaManager_OnAnyTimelinePropertyChanged(_currentDesiredSession, null);
                MediaManager_OnAnyMediaPropertyChanged(_currentDesiredSession, null);
                MediaManager_OnAnyPlaybackStateChanged(_currentDesiredSession, null);
            }
        }

        // LX Music
        private void HandleLXMusicIfDetected(string? sessionId)
        {
            if (PlayerIdHelper.IsLXMusic(sessionId))
            {
                StartLXMusicSSE();
            }
            else
            {
                StopLXMusicSSE();
            }
        }

        private void StartLXMusicSSE()
        {
            if (_lxMusicSse != null)
            {
                return;
            }

            try
            {
                _lxMusicSse = new EventSourceReader(new Uri($"{_settingsService.AppSettings.GeneralSettings.LXMusicServer}{LXMusic.QuerySuffix}")).Start();
                _lxMusicSse.MessageReceived += LXMusicSse_MessageReceived;
                _lxMusicSse.Disconnected += LXMusicSse_Disconnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartLXMusicSSE");
                AppUIThread.Execute(() =>
                {
                    GlobalToastManager.Show("FailToStartLXMusicServer", null, MessageSeverity.Error);
                });
                StopLXMusicSSE();
            }
        }

        private void StopLXMusicSSE()
        {
            if (_lxMusicSse != null)
            {
                _lxMusicSse.MessageReceived -= LXMusicSse_MessageReceived;
                _lxMusicSse.Disconnected -= LXMusicSse_Disconnected;
                _lxMusicSse.Dispose();
                _lxMusicSse = null;
            }
        }

        private void LXMusicSse_Disconnected(object sender, DisconnectEventArgs e)
        {
            AppUIThread.RunAsync(async () =>
            {
                await Task.Delay(e.ReconnectDelay);
                if (_lxMusicSse != null && !_lxMusicSse.IsDisposed) _lxMusicSse.Start();
            });
        }

        private void LXMusicSse_MessageReceived(object sender, EventSourceMessageEventArgs e)
        {
            AppUIThread.RunAsync(async () =>
            {
                if (PlayerIdHelper.IsLXMusic(CurrentSongInfo.PlayerId))
                {
                    var data = JsonSerializer.Deserialize(e.Message, Core.Serialization.SourceGenerationContext.Default.JsonElement);
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

        private void UniversalMemoryReader_OnProgressChanged(double time, double total)
        {
            OnAnyTimelineChangedCore(_currentDesiredSession, TimeSpan.FromSeconds(time), TimeSpan.FromSeconds(total));
        }

        public async Task PlayAsync()
        {
            await _currentDesiredSession?.ControlSession?.TryPlayAsync();
        }

        public async Task PauseAsync()
        {
            await _currentDesiredSession?.ControlSession?.TryPauseAsync();
        }

        public async Task StopAsync()
        {
            try
            {
                await _currentDesiredSession?.ControlSession?.TryStopAsync();
            }
            catch (Exception) { }
        }

        public async Task PreviousAsync()
        {
            await _currentDesiredSession?.ControlSession?.TrySkipPreviousAsync();
        }

        public async Task NextAsync()
        {
            await _currentDesiredSession?.ControlSession?.TrySkipNextAsync();
        }

        public async Task ChangePositionAsync(double seconds)
        {
            await _currentDesiredSession?.ControlSession?.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds).Ticks);
        }

        public async Task ChangeLyricsLineAsync(int index)
        {
            if (CurrentLyricsData?.LyricsLines?.ElementAtOrDefault(index)?.StartMs is int startMs)
            {
                await ChangePositionAsync(startMs / 1000.0);
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

        partial void OnCurrentMediaSourceProviderInfoChanged(MediaSourceProviderInfo? value)
        {
            foreach (var item in _settingsService.AppSettings.MediaSourceProvidersInfo)
            {
                item.IsFocused = item.Provider == value?.Provider;
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
                else if (message.PropertyName == nameof(TranslationSettings.IsFilterEnabled))
                {
                    UpdateLyrics();
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

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is MediaSourceProviderInfo)
            {
                if (message.PropertyName == nameof(MediaSourceProviderInfo.TargetAlbumArtSize))
                {
                    UpdateAlbumArt(true);
                }
            }
        }

        public void Receive(PropertyChangedMessage<WindowStatus> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(MusicGallerySettings.LyricsWindowStatus.WindowStatus))
                {
                    OnDesiredSessionChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ChineseConversion> message)
        {
            if (message.Sender is TranslationSettings)
            {
                if (message.PropertyName == nameof(TranslationSettings.ChineseConversion))
                {
                    UpdateLyrics();
                }
            }
        }
    }
}
