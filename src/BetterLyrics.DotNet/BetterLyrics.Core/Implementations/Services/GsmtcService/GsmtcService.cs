// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Memory;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EvtSource;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

/// <summary>
/// Singleton Service
/// </summary>
public partial class GsmtcService : ObservableObject, IGsmtcService,
    IRecipient<MediaSourceProviderEnabledChangedMessage>,
    IRecipient<MediaSourceDiscordPresenceChangedMessage>,
    IRecipient<TranslationSettingsChangedMessage>,
    IRecipient<MediaFolderEnabledChangedMessage>,
    IRecipient<MediaFolderLastSyncTimeChangedMessage>,
    IRecipient<MediaSourceTargetAlbumArtSizeChangedMessage>,
    IRecipient<LyricsWindowStatusStateChangedMessage>,
    IRecipient<DiscordAlbumArtSourceChangedMessage>
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

    [ObservableProperty]
    public partial bool IsScrobbled { get; set; } = false;

    [ObservableProperty] public partial TimeSpan ScrobbledDuration { get; set; } = TimeSpan.Zero;
    [ObservableProperty] public partial TimeSpan TargetScrobbledDuration { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial bool CurrentIsPlaying { get; private set; } = false;

    [ObservableProperty]
    public partial TimeSpan CurrentPosition { get; private set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial SongInfo CurrentSongInfo { get; private set; } = SongInfoExtensions.Placeholder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExternalSourceActive))]
    public partial MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; set; }

    public bool IsExternalSourceActive => CurrentMediaSourceProviderInfo != null && !CurrentMediaSourceProviderInfo.IsBetterLyrics;

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

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    partial void OnIsScrobbledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new SongScrobbledMessage(value));

    partial void OnCurrentSongInfoChanged(SongInfo value) =>
        WeakReferenceMessenger.Default.Send(new CurrentSongInfoChangedMessage(value));

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

    public void Receive(MediaSourceProviderEnabledChangedMessage message)
    {
        OnDesiredSessionChanged();
    }

    public void Receive(MediaSourceDiscordPresenceChangedMessage message)
    {
        _ = UpdateDiscordPresenceAsync();
    }

    public void Receive(TranslationSettingsChangedMessage message)
    {
        if (message.PropertyName == nameof(TranslationSettings.SelectedTargetLanguageCode))
        {
            _logger.LogInformation("Target LibreTranslate language code changed: {code}",
                _settingsService.AppSettings.TranslationSettings.SelectedTargetLanguageCode);
        }

        if (IsPresentationTranslationProperty(message.PropertyName))
        {
            UpdateLyricsPresentation();
        }
        else
        {
            // Only do a full refresh when the lyrics content changed
            UpdateLyrics();
        }
    }

    private static bool IsPresentationTranslationProperty(string propertyName) =>
        propertyName is
            nameof(TranslationSettings.IsTranslationEnabled) or
            nameof(TranslationSettings.SelectedTargetLanguageCode) or
            nameof(TranslationSettings.IsMandarinRomanizationEnabled) or
            nameof(TranslationSettings.IsCantoneseRomanizationEnabled) or
            nameof(TranslationSettings.IsJapaneseRomanizationEnabled) or
            nameof(TranslationSettings.IsKoreanRomanizationEnabled) or
            nameof(TranslationSettings.IsLibreTranslateEnabled) or
            nameof(TranslationSettings.LibreTranslateServer);

    public void Receive(MediaFolderEnabledChangedMessage message)
    {
        UpdateAlbumArt();
        UpdateLyrics();
    }

    public void Receive(MediaFolderLastSyncTimeChangedMessage message)
    {
        UpdateAlbumArt();
        UpdateLyrics();
    }

    public void Receive(MediaSourceTargetAlbumArtSizeChangedMessage message)
    {
        UpdateAlbumArt(true);
    }

    public void Receive(LyricsWindowStatusStateChangedMessage message)
    {
        OnDesiredSessionChanged();
    }

    public void Receive(DiscordAlbumArtSourceChangedMessage message)
    {
        CurrentSongInfo.AlbumArtUrl = null;
        _ = UpdateDiscordPresenceAsync();
    }

    private void ScrobbleTimerCallback(object? state)
    {
        if (!IsScrobbled)
        {
            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
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
    }

    private void LocalMediaFolders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

            _logger.LogInformation("Media manager initialized, sessions: {sessions}",
                string.Join(", ", _mediaManagerProvider.CurrentMediaSessions.Select(x => $"{x.SessionId} ({x.PlaybackStatus})")));

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
        //Debug.WriteLine("OnDesiredSessionChanged: " + desiredSession?.SessionId);
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
            if (!IsCurrentDesiredSession(mediaSession)) return;

            mediaSession?.TryRefreshTimelinePropsAsync();

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

    private async void MediaManager_OnAnyPlaybackStateChanged(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession != null)
        {
            await mediaSession.TryRefreshPlaybackStateAsync();
        }

        OnDesiredSessionChanged();

        if (!IsCurrentDesiredSession(mediaSession)) return;

        var isPlaying = mediaSession?.PlaybackStatus switch
        {
            SessionPlaybackStatus.Playing => true,
            _ => false
        };

        if (isPlaying)
        {
            _scrobbleTimer.Change(0, 1000);
        }
        else
        {
            _scrobbleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        _appUIThreadProvider.Execute(async () =>
        {
            CurrentIsPlaying = isPlaying;
            _ = UpdateDiscordPresenceAsync();
        });
    }

    private void MediaManager_OnAnyMediaPropertyChanged(IMediaSessionProvider? mediaSession)
    {
        // 避免其他播放源的媒体信息变化应影响当前播放源
        if (!IsCurrentDesiredSession(mediaSession)) return;

        _ = _onMediaPropsChangedDebouncer.RunAsync(
            () =>
            {
                _ = OnAnyMediaPropertyChangedCoreAsync(mediaSession);
            }, 1000);
    }

    private void MediaManager_OnAnySessionClosed(IMediaSessionProvider? mediaSession)
    {
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
        if (!IsCurrentDesiredSession(mediaSession)) return;

        var sessionId = mediaSession?.SessionId;

        var currentMediaSourceProviderInfo = GetCurrentDesiredMediaSourceProviderInfo();
        if (currentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
        {
            currentMediaSourceProviderInfo?.PositionOffset = 0;
        }

        if (mediaSession != null)
        {
            await mediaSession.TryRefreshMediaPropsAsync();
            // 暂停状态下切歌通常不会产生时间轴变化事件，EndTime 会停留在上一首；
            // 时长取自 EndTime，这里主动刷新一次，避免时长为上一首的数值。
            await mediaSession.TryRefreshTimelinePropsAsync();
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
        {
            if (currentMediaSourceProviderInfo.MemoryReaderConfig is MemoryReaderConfig config)
            {
                _memoryReader.Config = config;
                _memoryReader.Start();
                _memoryReader.OnProgressChanged += UniversalMemoryReader_OnProgressChanged;
            }
        }

        // 处理专辑图片
        if (PlayerIdHelper.IsLXMusic(sessionId) && _lxMusicAlbumArtBytes != null)
        {
            _smtcAlbumArtBuffer = _lxMusicAlbumArtBytes;
        }
        else if (mediaSession?.Thumbnail is byte[] imageData)
        {
            _smtcAlbumArtBuffer = imageData;
        }
        else
        {
            _smtcAlbumArtBuffer = null;
        }

        _appUIThreadProvider.Execute(() =>
        {
            if (string.IsNullOrEmpty(fixedTitle) && string.IsNullOrEmpty(fixedArtist) && string.IsNullOrEmpty(fixedAlbum))
            {
                CurrentSongInfo = SongInfoExtensions.Placeholder;
            }
            else
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
            }

            UpdateTargetScrobbledDuration();
            IsScrobbled = false;
            ScrobbledDuration = TimeSpan.Zero;

            CurrentMediaSourceProviderInfo = currentMediaSourceProviderInfo;
            UpdateCurrentMediaSourceProviderInfoPositionOffset();
            _ = UpdateDiscordPresenceAsync();

            var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
            if (isLastFMEnabled)
            {
                _ = Task.Run(() => _lastFmService.UpdateNowPlayingAsync(CurrentSongInfo));
                _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged: {Title} update now playing to last.fm", CurrentSongInfo.Title);
            }

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
            _settingsService.AppSettings.MediaSourceProvidersInfo.Add(new MediaSourceProviderInfo(id,
                _settingsService.AppSettings.GeneralSettings.ListenOnNewPlaybackSource));
        }
    }

    private IMediaSessionProvider? GetValidFocusedSession()
    {
        var focusedSession = _mediaManagerProvider.FocusedSession;
        if (focusedSession == null || !IsMediaSourceEnabled(focusedSession.SessionId))
        {
            return null;
        }

        if (PlayerIdHelper.IsBetterLyrics(focusedSession.SessionId) && !_windowManagerProvider.IsWindowFocused(WindowType.MusicGalleryWindow))
        {
            return null;
        }

        return focusedSession;
    }

    private IMediaSessionProvider? GetCurrentDesiredSession()
    {
        var enabledSessions = _mediaManagerProvider.CurrentMediaSessions
            .Where(x => IsMediaSourceEnabled(x.SessionId))
            .ToList();
        var validFocusedSession = GetValidFocusedSession();
        // 当前会话的 provider 实例可能因会话重建而失效，这里始终取媒体管理器中的实时实例，
        // 避免 _currentDesiredSession 指向已释放的旧对象。
        var currentDesiredSession = _currentDesiredSession is null
            ? null
            : enabledSessions.FirstOrDefault(x => x.SessionId == _currentDesiredSession.SessionId);

        // Priority 1: Playing sessions
        // 1.1 Valid focused session that is playing
        if (validFocusedSession?.PlaybackStatus == SessionPlaybackStatus.Playing)
        {
            return validFocusedSession;
        }
        // 1.2 Valid current desired session that is playing
        if (currentDesiredSession?.PlaybackStatus == SessionPlaybackStatus.Playing)
        {
            return currentDesiredSession;
        }
        // 1.3 Any other session that is playing
        var playingSession = enabledSessions.FirstOrDefault(x => x.PlaybackStatus == SessionPlaybackStatus.Playing);
        if (playingSession != null)
        {
            return playingSession;
        }

        // Priority 2: Paused sessions
        // 2.1 Valid focused session that is paused
        if (validFocusedSession != null)
        {
            return validFocusedSession;
        }
        // 2.2 Valid current desired session that is paused
        if (currentDesiredSession != null)
        {
            return currentDesiredSession;
        }

        // Priority 3: Fallback strategy
        // Return the first available session
        return enabledSessions.FirstOrDefault();
    }

    /// <summary>
    /// 判断给定会话是否就是当前播放源。
    /// 按 <see cref="IMediaSessionProvider.SessionId"/> 比较，避免 provider 实例被重建（同一会话产生新实例）后
    /// 引用比较静默失效、导致事件被丢弃且歌曲信息/歌词不再更新；null 与 null 视为一致（用于清空歌曲信息的场景）。
    /// </summary>
    private bool IsCurrentDesiredSession(IMediaSessionProvider? mediaSession)
    {
        if (mediaSession is null || _currentDesiredSession is null)
        {
            return mediaSession is null && _currentDesiredSession is null;
        }

        return mediaSession.SessionId == _currentDesiredSession.SessionId;
    }

    private void UpdateCurrentMediaSourceProviderInfoPositionOffset()
    {
        if (CurrentPosition.TotalSeconds <= 1 &&
            CurrentMediaSourceProviderInfo?.ResetPositionOffsetOnSongChanged == true)
        {
            CurrentMediaSourceProviderInfo?.PositionOffset = 0;
        }
    }

    private async Task UpdateDiscordPresenceAsync()
    {
        if (CurrentMediaSourceProviderInfo?.IsDiscordPresenceEnabled == true && CurrentSongInfo != SongInfoExtensions.Placeholder)
        {
            var discordSource = _settingsService.AppSettings.DiscordSettings.AlbumArtSource;
            if (discordSource != OnlineAlbumArtProvider.None && string.IsNullOrEmpty(CurrentSongInfo.AlbumArtUrl))
            {
                CurrentSongInfo.AlbumArtUrl = await _albumArtSearchService.GetAlbumArtUrlAsync(
                    CurrentSongInfo, discordSource, 500, CancellationToken.None);
            }

            await _discordService.UpdateRichPresenceAsync(CurrentSongInfo, CurrentIsPlaying, CurrentPosition, CurrentSongInfo.AlbumArtUrl);
        }
        else
        {
            _discordService.ClearRichPresence();
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
                _globalToastProvider.Show("FailToStartLXMusicServer", ex.Message, MessageSeverity.Error);
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
            if (_lxMusicSse != null && !_lxMusicSse.IsDisposed)
            {
                _lxMusicSse.Start();
            }
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
                    {
                        CurrentPosition = TimeSpan.FromSeconds(_lxMusicPositionSeconds);
                    }
                }
                else if (data.ValueKind == JsonValueKind.String)
                {
                    if (e.Event == "picUrl")
                    {
                        var picUrl = data.GetString();
                        if (picUrl != null)
                        {
                            //_logger.LogInformation("LX Music Album Art URL: {url}", picUrl);
                            _lxMusicAlbumArtBytes = await ImageHelper.GetImageByteArrayFromUrlAsync(picUrl);
                            if (_lxMusicAlbumArtBytes != null)
                            {
                                _smtcAlbumArtBuffer = _lxMusicAlbumArtBytes;
                            }
                            else
                            {
                                _smtcAlbumArtBuffer = null;
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
        WeakReferenceMessenger.Default.Send(new CurrentIsPlayingChangedMessage(value));
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        _windowManagerProvider.SetTaskbarProgressValue(WindowType.NowPlayingWindow, value.TotalSeconds / CurrentSongInfo.Duration);
        WeakReferenceMessenger.Default.Send(new CurrentPositionChangedMessage(value));
    }

    partial void OnCurrentMediaSourceProviderInfoChanged(MediaSourceProviderInfo? value)
    {
        foreach (var item in _settingsService.AppSettings.MediaSourceProvidersInfo)
        {
            item.IsFocused = item.Provider == value?.Provider;
        }
        WeakReferenceMessenger.Default.Send(new CurrentMediaSourceProviderChangedMessage(value));
    }
}