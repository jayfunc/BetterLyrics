// 2025/6/23 by Zhe Fang

using System.Collections.Specialized;
using System.Text.Json;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Memory;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EvtSource;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

public partial class GsmtcService : BaseViewModel, IGsmtcService,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<DateTime?>>,
    IRecipient<PropertyChangedMessage<int>>,
    IRecipient<PropertyChangedMessage<WindowStatus>>,
    IRecipient<PropertyChangedMessage<ChineseConversion>>,
    IRecipient<PropertyChangedMessage<DiscordAlbumArtSource>>
{
    private readonly IAlbumArtSearchService _albumArtSearchService;
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IDiscordService _discordService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILastFmService _lastFmService;
    private readonly ILogger<GsmtcService> _logger;
    private readonly ILyricsSearchService _lyrcsSearchService;

    private readonly IMediaManagerProvider _mediaManagerProvider =
        Ioc.Default.GetRequiredService<IMediaManagerProvider>();

    private readonly IUniversalMemoryReaderProvider _memoryReader =
        Ioc.Default.GetRequiredService<IUniversalMemoryReaderProvider>();

    private readonly Debouncer _onMediaPropsChangedDebouncer = new();
    private readonly IPlayHistoryService _playHistoryService;
    private readonly Timer _scrobbleTimer;
    private readonly ISettingsService _settingsService;
    private readonly ITranslationService _translationService;
    private readonly ITransliterationService _transliterationService;
    private readonly IWindowManagerProvider _windowManagerProvider;

    private IMediaSessionProvider? _currentDesiredSession;
    private byte[]? _lxMusicAlbumArtBytes;

    private double _lxMusicPositionSeconds;
    private EventSourceReader? _lxMusicSse;
    private byte[]? _smtcAlbumArtBuffer;

    public GsmtcService(
        ISettingsService settingsService,
        IAlbumArtSearchService albumArtSearchService,
        ILyricsSearchService lyricsSearchService,
        IDiscordService discordService,
        ITranslationService libreTranslateService,
        ITransliterationService transliterationService,
        IPlayHistoryService playHistoryService,
        ILastFmService lastFmService,
        IAppUIThreadProvider appUiThreadProvider,
        ILogger<GsmtcService> logger, IGlobalToastProvider globalToastProvider,
        IWindowManagerProvider windowManagerProvider)
    {
        _settingsService = settingsService;
        _albumArtSearchService = albumArtSearchService;
        _lyrcsSearchService = lyricsSearchService;
        _translationService = libreTranslateService;
        _transliterationService = transliterationService;
        _discordService = discordService;
        _playHistoryService = playHistoryService;
        _lastFmService = lastFmService;
        _appUIThreadProvider = appUiThreadProvider;
        _logger = logger;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;

        _scrobbleTimer = new Timer(ScrobbleTimerCallback, null, Timeout.Infinite, 1000);

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

        _settingsService.AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged +=
            MediaSourceProvidersInfo_ItemPropertyChanged;

        _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;

        InitMediaManager();
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsScrobbled { get; set; } = false;

    [ObservableProperty] public partial TimeSpan ScrobbledDuration { get; set; } = TimeSpan.Zero;
    [ObservableProperty] public partial TimeSpan TargetScrobbledDuration { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool CurrentIsPlaying { get; private set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TimeSpan CurrentPosition { get; private set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial SongInfo CurrentSongInfo { get; private set; } = SongInfoExtensions.Placeholder;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; set; }

    public async Task PlayAsync()
    {
        await (_currentDesiredSession?.TryPlayAsync() ?? Task.CompletedTask);
    }

    public async Task PauseAsync()
    {
        await (_currentDesiredSession?.TryPauseAsync() ?? Task.CompletedTask);
    }

    public async Task StopAsync()
    {
        try
        {
            await (_currentDesiredSession?.TryStopAsync() ?? Task.CompletedTask);
        }
        catch (Exception)
        {
        }
    }

    public async Task PreviousAsync()
    {
        await (_currentDesiredSession?.TrySkipPreviousAsync() ?? Task.CompletedTask);
    }

    public async Task NextAsync()
    {
        await (_currentDesiredSession?.TrySkipNextAsync() ?? Task.CompletedTask);
    }

    public async Task ChangePositionAsync(double seconds)
    {
        await (_currentDesiredSession?.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds)) ?? Task.CompletedTask);
    }

    public async Task ChangeLyricsLineAsync(int index)
    {
        if (CurrentLyricsData?.LyricsLines?.ElementAtOrDefault(index)?.StartMs is int startMs)
            await ChangePositionAsync(startMs / 1000.0);
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is MediaSourceProviderInfo)
        {
            if (message.PropertyName == nameof(MediaSourceProviderInfo.IsEnabled)) OnDesiredSessionChanged();
        }
        else if (message.Sender is TranslationSettings)
        {
            if (message.PropertyName == nameof(TranslationSettings.IsLibreTranslateEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsTranslationEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsMandarinRomanizationEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsCantoneseRomanizationEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsJapaneseRomanizationEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsKoreanRomanizationEnabled))
                UpdateLyrics();
            else if (message.PropertyName == nameof(TranslationSettings.IsFilterEnabled))
                UpdateLyrics();
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

    public void Receive(PropertyChangedMessage<ChineseConversion> message)
    {
        if (message.Sender is TranslationSettings)
            if (message.PropertyName == nameof(TranslationSettings.ChineseConversion))
                UpdateLyrics();
    }



    public void Receive(PropertyChangedMessage<DateTime?> message)
    {
        if (message.Sender is MediaFolder)
            if (message.PropertyName == nameof(MediaFolder.LastSyncTime))
            {
                UpdateAlbumArt();
                UpdateLyrics();
            }
    }

    public void Receive(PropertyChangedMessage<int> message)
    {
        if (message.Sender is MediaSourceProviderInfo)
            if (message.PropertyName == nameof(MediaSourceProviderInfo.TargetAlbumArtSize))
                UpdateAlbumArt(true);
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender is TranslationSettings)
        {
            if (message.PropertyName == nameof(TranslationSettings.SelectedTargetLanguageCode))
            {
                _logger.LogInformation("Target LibreTranslate language code changed: {code}",
                    _settingsService.AppSettings.TranslationSettings.SelectedTargetLanguageCode);
                UpdateLyrics();
            }
            else if (message.PropertyName == nameof(TranslationSettings.LibreTranslateServer))
            {
                UpdateLyrics();
            }
        }
    }

    public void Receive(PropertyChangedMessage<WindowStatus> message)
    {
        if (message.Sender is LyricsWindowStatus)
            if (message.PropertyName == nameof(MusicGallerySettings.LyricsWindowStatus.WindowStatus))
                OnDesiredSessionChanged();
    }

    public void Receive(PropertyChangedMessage<DiscordAlbumArtSource> message)
    {
        if (message.Sender is DiscordSettings)
        {
            if (message.PropertyName == nameof(DiscordSettings.AlbumArtSource))
            {
                if (CurrentSongInfo != null)
                {
                    CurrentSongInfo.AlbumArtUrl = null;
                }
                _ = UpdateDiscordPresenceAsync();
            }
        }
    }

    private void ScrobbleTimerCallback(object? state)
    {
        if (!IsScrobbled)
            if (!string.IsNullOrWhiteSpace(CurrentSongInfo.Title) && CurrentSongInfo.Title != "N/A")
            {
                _appUIThreadProvider.Execute(() =>
                {
                    ScrobbledDuration += TimeSpan.FromSeconds(1);
                    if (ScrobbledDuration >= TargetScrobbledDuration)
                    {
                        // 写入本地播放记录
                        var playHistoryItem = CurrentSongInfo.ToPlayHistoryItem(ScrobbledDuration.TotalMilliseconds);
                        if (playHistoryItem != null)
                        {
                            // 后台
                            _ = Task.Run(async () => { await _playHistoryService.AddLogAsync(playHistoryItem); });
                            _logger.LogInformation("ScrobbleTimer_Tick: {Title} scrobbled to local stat",
                                CurrentSongInfo.Title);
                        }

                        // 写入 Last.fm 播放记录
                        var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
                        if (isLastFMEnabled)
                        {
                            // 后台
                            _ = Task.Run(() => _lastFmService.TrackAsync(CurrentSongInfo));
                            _logger.LogInformation("ScrobbleTimer_Tick: {Title} scrobbled to last.fm",
                                CurrentSongInfo.Title);
                        }

                        IsScrobbled = true;
                        ScrobbledDuration = TimeSpan.Zero;
                    }
                });
            }
    }

    private void LocalMediaFolders_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
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
        }
    }

    private MediaSourceProviderInfo? GetCurrentDesiredMediaSourceProviderInfo()
    {
        return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x =>
            x.Provider == _currentDesiredSession?.SessionId);
    }

    private bool IsMediaSourceEnabled(string id)
    {
        var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id);
        return found?.IsEnabled ?? true;
    }

    private bool IsMediaSourceTimelineSyncEnabled(string? id)
    {
        return _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id)
            ?.IsTimelineSyncEnabled ?? true;
    }

    private void InitMediaManager()
    {
        // 经反馈，某些用户环境下 MediaManager.Start() 会抛出异常，暂时捕获并提示，避免程序崩溃
        try
        {
            _mediaManagerProvider.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            _mediaManagerProvider.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManagerProvider.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            _mediaManagerProvider.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManagerProvider.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManagerProvider.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            _mediaManagerProvider.Init();

            _mediaManagerProvider.CurrentMediaSessions.ToList().ForEach(x => RecordMediaSession(x.SessionId));

            OnDesiredSessionChanged(true);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            return;
        }
    }

    private void OnDesiredSessionChanged(bool firstTime = false)
    {
        var desiredSession = GetCurrentDesiredSession();
        if (firstTime || desiredSession != _currentDesiredSession)
        {
            _currentDesiredSession = desiredSession;
            SendFocusedMessages();
        }
    }

    private void OnAnyTimelineChangedCore(IMediaSessionProvider? mediaSession)
    {
        _appUIThreadProvider.Execute(() =>
        {
            if (mediaSession != _currentDesiredSession) return;

            if (mediaSession != null)
            {
                mediaSession.TryRefreshTimelinePropsAsync();
            }

            CurrentPosition = mediaSession?.CurrentTime ?? TimeSpan.Zero;
            CurrentSongInfo.DurationMs = mediaSession?.EndTime.TotalMilliseconds ?? 0;
            UpdateTargetScrobbledDuration();
            if (CurrentPosition.TotalSeconds == 0)
            {
                IsScrobbled = false;
                ScrobbledDuration = TimeSpan.Zero;
            }
        });
    }

    private void MediaManager_OnFocusedSessionChanged(IMediaSessionProvider? mediaSession)
    {
        OnDesiredSessionChanged();
    }

    private void MediaManager_OnAnyTimelinePropertyChanged(IMediaSessionProvider? mediaSession)
    {
        OnAnyTimelineChangedCore(mediaSession);
    }

    private void MediaManager_OnAnyPlaybackStateChanged(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession != _currentDesiredSession) return;

        if (mediaSession != null)
        {
            mediaSession.TryRefreshPlaybackStateAsync();
        }

        var isPlaying = mediaSession?.PlaybackStatus switch
        {
            SessionPlaybackStatus.Playing => true,
            _ => false
        };

        if (isPlaying)
            _scrobbleTimer.Change(0, 1000);
        else
            _scrobbleTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _appUIThreadProvider.Execute(async () =>
        {
            CurrentIsPlaying = isPlaying;
            _ = UpdateDiscordPresenceAsync();
        });
    }

    private void MediaManager_OnAnyMediaPropertyChanged(IMediaSessionProvider? mediaSession)
    {
        _ = _onMediaPropsChangedDebouncer.RunAsync(
            () => { _ = OnAnyMediaPropertyChangedCoreAsync(mediaSession); }, 1000);
    }

    private void MediaManager_OnAnySessionClosed(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession == null) return;

        OnDesiredSessionChanged();
    }

    private void MediaManager_OnAnySessionOpened(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession == null) return;

        var id = mediaSession.SessionId;

        _appUIThreadProvider.Execute(() =>
        {
            RecordMediaSession(id);
            OnDesiredSessionChanged();
        });
    }

    private async Task OnAnyMediaPropertyChangedCoreAsync(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession != _currentDesiredSession) return;

        var sessionId = mediaSession?.SessionId;

        var currentMediaSourceProviderInfo = GetCurrentDesiredMediaSourceProviderInfo();
        if (currentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
            currentMediaSourceProviderInfo?.PositionOffset = 0;

        if (mediaSession != null)
        {
            await mediaSession.TryRefreshMediaPropsAsync();
        }

        // 处理歌曲信息
        var fixedTitle = mediaSession?.Title;
        var fixedArtist = mediaSession?.Artist;
        var fixedAlbum = mediaSession?.Album;
        string? songId = null;

        if (PlayerIdHelper.IsAppleMusic(sessionId))
        {
            fixedArtist = mediaSession?.Artist?.Split(" — ").First();
            fixedAlbum = mediaSession?.Artist?.Split(" — ").Last();
            fixedAlbum = fixedAlbum?.Replace(" - Single", "");
            fixedAlbum = fixedAlbum?.Replace(" - EP", "");
        }
        else if (PlayerIdHelper.IsNeteaseFamily(sessionId))
        {
            songId = mediaSession?.Genres?
                .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.NetEaseCloudMusicTrackID))?
                .Replace(ExtendedGenreFiled.NetEaseCloudMusicTrackID, "");
        }
        else if (PlayerIdHelper.IsQQFamily(sessionId))
        {
            songId = mediaSession?.Genres?
                .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.QQMusicTrackID))?
                .Replace(ExtendedGenreFiled.QQMusicTrackID, "");
        }

        var linkedFileName = mediaSession?.Genres?
            .FirstOrDefault(x => x.StartsWith(ExtendedGenreFiled.FileName))?
            .Replace(ExtendedGenreFiled.FileName, "");

        HandleLXMusicIfDetected(sessionId);

        // 总是先停止 _memoryReader
        _memoryReader.Stop();

        // 注册
        if (currentMediaSourceProviderInfo?.IsMemoryReaderEnabled == true)
            if (currentMediaSourceProviderInfo.MemoryReaderConfig is MemoryReaderConfig config)
            {
                _memoryReader.Start();
                _memoryReader.OnProgressChanged += UniversalMemoryReader_OnProgressChanged;
            }

        // 处理专辑图片
        if (PlayerIdHelper.IsLXMusic(sessionId) && _lxMusicAlbumArtBytes != null)
            _smtcAlbumArtBuffer = _lxMusicAlbumArtBytes;
        else if (mediaSession?.Thumbnail is byte[] imageData)
            _smtcAlbumArtBuffer = imageData;
        else
            _smtcAlbumArtBuffer = null;

        _appUIThreadProvider.Execute(() =>
        {
            CurrentSongInfo = new SongInfo
            {
                Title = fixedTitle ?? "N/A",
                Artist = fixedArtist ?? "N/A",
                Album = fixedAlbum ?? "N/A",
                DurationMs = mediaSession?.EndTime.TotalMilliseconds ?? 0,
                PlayerId = sessionId,
                SongId = songId,
                LinkedFileName = linkedFileName,
                StartedAt = DateTime.Now.ToBinary()
            };

            UpdateTargetScrobbledDuration();
            IsScrobbled = false;
            ScrobbledDuration = TimeSpan.Zero;

            CurrentMediaSourceProviderInfo = currentMediaSourceProviderInfo;
            UpdateCurrentMediaSourceProviderInfoPositionOffset();
            _ = UpdateDiscordPresenceAsync();

            UpdateLyrics();
            UpdateAlbumArt();

            _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged {SongInfo}", CurrentSongInfo);
        });
    }

    private void RecordMediaSession(string id)
    {
        var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == id);
        if (found == null)
            _settingsService.AppSettings.MediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id,
                _settingsService.AppSettings.GeneralSettings.ListenOnNewPlaybackSource));
    }

    private IMediaSessionProvider? GetCurrentDesiredSession()
    {
        // 检查内置播放器会话是否存在
        var selfSession =
            _mediaManagerProvider.CurrentMediaSessions.FirstOrDefault(x => PlayerIdHelper.IsBetterLyrics(x.SessionId));
        var selfSessionKey = selfSession?.SessionId;
        // 合法且设置中处于启用状态则
        if (!string.IsNullOrEmpty(selfSessionKey) && IsMediaSourceEnabled(selfSessionKey))
            // 直接返回，即使当前聚焦的会话非内置播放器
            return selfSession;

        // 若音乐库处于开启状态且未开启内置播放源会话
        if (_settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus ==
            WindowStatus.Opened)
            return null;

        var focusedSession = _mediaManagerProvider.FocusedSession;
        if (focusedSession != null && IsMediaSourceEnabled(focusedSession.SessionId)) return focusedSession;

        foreach (var session in _mediaManagerProvider.CurrentMediaSessions)
            if (IsMediaSourceEnabled(session.SessionId))
                return session;

        return null;
    }

    private void UpdateCurrentMediaSourceProviderInfoPositionOffset()
    {
        if (CurrentPosition.TotalSeconds <= 1 &&
            CurrentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
            CurrentMediaSourceProviderInfo?.PositionOffset = 0;
    }

    private async Task UpdateDiscordPresenceAsync()
    {
        if (CurrentMediaSourceProviderInfo?.IsDiscordPresenceEnabled == true && CurrentSongInfo != null)
        {
            var discordSource = _settingsService.AppSettings.DiscordSettings.AlbumArtSource;
            if (discordSource != DiscordAlbumArtSource.None && string.IsNullOrEmpty(CurrentSongInfo.AlbumArtUrl))
            {
                CurrentSongInfo.AlbumArtUrl = await _albumArtSearchService.GetAlbumArtUrlAsync(
                    CurrentSongInfo, discordSource, 500, CancellationToken.None);
            }

            await _discordService.UpdateRichPresenceAsync(CurrentSongInfo, CurrentIsPlaying, CurrentPosition, CurrentSongInfo.AlbumArtUrl);
        }
    }

    private void UpdateTargetScrobbledDuration()
    {
        TargetScrobbledDuration =
            TimeSpan.FromSeconds(CurrentSongInfo.Duration == 0 ? 30 : CurrentSongInfo.Duration / 2);
    }

    private void SendFocusedMessages()
    {
        try
        {
            MediaManager_OnAnyTimelinePropertyChanged(_currentDesiredSession);
            MediaManager_OnAnyMediaPropertyChanged(_currentDesiredSession);
            MediaManager_OnAnyPlaybackStateChanged(_currentDesiredSession);
        }
        catch (Exception)
        {
            MediaManager_OnAnyTimelinePropertyChanged(null);
            MediaManager_OnAnyMediaPropertyChanged(null);
            MediaManager_OnAnyPlaybackStateChanged(null);
        }
    }

    // LX Music
    private void HandleLXMusicIfDetected(string? sessionId)
    {
        if (PlayerIdHelper.IsLXMusic(sessionId))
            StartLXMusicSSE();
        else
            StopLXMusicSSE();
    }

    private void StartLXMusicSSE()
    {
        if (_lxMusicSse != null) return;

        try
        {
            _lxMusicSse =
                new EventSourceReader(new Uri(
                    $"{_settingsService.AppSettings.GeneralSettings.LXMusicServer}{LXMusic.QuerySuffix}")).Start();
            _lxMusicSse.MessageReceived += LXMusicSse_MessageReceived;
            _lxMusicSse.Disconnected += LXMusicSse_Disconnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartLXMusicSSE");
            _appUIThreadProvider.Execute(() =>
            {
                _globalToastProvider.Show("FailToStartLXMusicServer", null, MessageSeverity.Error);
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
        _appUIThreadProvider.RunAsync(async () =>
        {
            await Task.Delay(e.ReconnectDelay);
            if (_lxMusicSse != null && !_lxMusicSse.IsDisposed) _lxMusicSse.Start();
        });
    }

    private void LXMusicSse_MessageReceived(object sender, EventSourceMessageEventArgs e)
    {
        _appUIThreadProvider.RunAsync(async () =>
        {
            if (PlayerIdHelper.IsLXMusic(CurrentSongInfo.PlayerId))
            {
                var data = JsonSerializer.Deserialize(e.Message,
                    SourceGenerationContext.Default.JsonElement);
                if (data.ValueKind == JsonValueKind.Number)
                {
                    if (e.Event == "progress")
                    {
                        _lxMusicPositionSeconds = data.GetDouble();
                    }
                    else if (e.Event == "duration")
                    {
                        CurrentSongInfo.DurationMs = data.GetDouble() * 1000;
                        _ = UpdateDiscordPresenceAsync();
                    }

                    if (IsMediaSourceTimelineSyncEnabled(CurrentSongInfo.PlayerId))
                        CurrentPosition = TimeSpan.FromSeconds(_lxMusicPositionSeconds);
                }
                else if (data.ValueKind == JsonValueKind.String)
                {
                    if (e.Event == "picUrl")
                    {
                        var picUrl = data.GetString();
                        if (picUrl != null)
                        {
                            _logger.LogInformation("LX Music Album Art URL: {url}", picUrl);
                            _lxMusicAlbumArtBytes = await ImageHelper.GetImageByteArrayFromUrlAsync(picUrl);
                            if (_lxMusicAlbumArtBytes != null)
                                _smtcAlbumArtBuffer = _lxMusicAlbumArtBytes;
                            else
                                _smtcAlbumArtBuffer = null;

                            UpdateAlbumArt();
                        }
                    }
                }
            }
        });
    }

    private void UniversalMemoryReader_OnProgressChanged(double time, double total)
    {
        _appUIThreadProvider.Execute(() =>
        {
            if (total > 0)
            {
                CurrentSongInfo.DurationMs = total * 1000;
                UpdateTargetScrobbledDuration();
            }

            if (IsMediaSourceTimelineSyncEnabled(CurrentSongInfo.PlayerId))
            {
                CurrentPosition = TimeSpan.FromSeconds(time);
            }

            if (CurrentPosition.TotalSeconds == 0)
            {
                IsScrobbled = false;
                ScrobbledDuration = TimeSpan.Zero;
            }
        });
    }

    partial void OnCurrentIsPlayingChanged(bool value)
    {
        _windowManagerProvider.SetTaskbarProgressState(WindowType.NowPlayingWindow, value);
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        _windowManagerProvider.SetTaskbarProgressValue(WindowType.NowPlayingWindow, value.TotalSeconds / CurrentSongInfo.Duration);
    }

    partial void OnCurrentMediaSourceProviderInfoChanged(MediaSourceProviderInfo? value)
    {
        foreach (var item in _settingsService.AppSettings.MediaSourceProvidersInfo)
            item.IsFocused = item.Provider == value?.Provider;
    }
}