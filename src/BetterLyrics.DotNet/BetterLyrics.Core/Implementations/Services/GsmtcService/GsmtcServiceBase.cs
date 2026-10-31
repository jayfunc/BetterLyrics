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
using System.Text.Json;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

public abstract partial class GsmtcServiceBase : ObservableObject, IGsmtcService,
    IRecipient<TranslationSettingsChangedMessage>,
    IRecipient<MediaFolderEnabledChangedMessage>,
    IRecipient<MediaFolderLastSyncTimeChangedMessage>,
    IRecipient<MediaSourceTargetAlbumArtSizeChangedMessage>
{
    protected readonly IAlbumArtSearchService _albumArtSearchService;
    protected readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IGlobalToastProvider _globalToastProvider;
    protected readonly ILogger _logger;
    private readonly ILyricsSearchService _lyrcsSearchService;

    protected readonly IMediaManagerProvider _mediaManagerProvider =
        Ioc.Default.GetRequiredService<IMediaManagerProvider>();

    protected readonly IMonitorProvider _monitorProvider =
        Ioc.Default.GetRequiredService<IMonitorProvider>();

    private readonly Debouncer _onMediaPropsChangedDebouncer = new();
    protected readonly ISettingsService _settingsService;
    private readonly ITranslationService _translationService;
    private readonly ITransliterationService _transliterationService;
    protected readonly IWindowManagerProvider _windowManagerProvider;

    protected IMediaSessionProvider? _currentDesiredSession;
    private byte[]? _lxMusicAlbumArtBytes;

    private double _lxMusicPositionSeconds;
    private EventSourceReader? _lxMusicSse;
    private byte[]? _smtcAlbumArtBuffer;

    /// <summary>
    /// 上一次成功应用到 UI 的 SMTC 媒体信息快照；系统息屏/挂起恢复后用于判断歌曲是否已经变化。
    /// </summary>
    private (string? Title, string? Artist, string? Album)? _appliedMediaPropsSnapshot;

    private bool _isRefreshingAfterDisplayResume;

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

    /// <summary>
    /// 消息通道标识；为 null 时发送到默认通道。
    /// </summary>
    protected virtual string? MessageToken => null;

    protected GsmtcServiceBase(
    ISettingsService settingsService,
    IAlbumArtSearchService albumArtSearchService,
    ILyricsSearchService lyricsSearchService,
    ITranslationService libreTranslateService,
    ITransliterationService transliterationService,
    IAppUIThreadProvider appUiThreadProvider,
    ILogger logger, IGlobalToastProvider globalToastProvider,
    IWindowManagerProvider windowManagerProvider)
    {
        _settingsService = settingsService;
        _albumArtSearchService = albumArtSearchService;
        _lyrcsSearchService = lyricsSearchService;
        _translationService = libreTranslateService;
        _transliterationService = transliterationService;
        _appUIThreadProvider = appUiThreadProvider;
        _logger = logger;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;

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
    }

    private void SendMessage<TMessage>(TMessage message) where TMessage : class
    {
        var token = MessageToken;
        if (token == null)
        {
            WeakReferenceMessenger.Default.Send(message);
        }
        else
        {
            WeakReferenceMessenger.Default.Send(message, token);
        }
    }

    protected virtual void OnPlaybackStateApplied(bool isPlaying) { }
    protected virtual void OnPresenceUpdateRequested() { }
    protected virtual void OnSongInfoApplied() { }
    protected virtual void OnMediaPropertiesRefreshing(MediaSourceProviderInfo? providerInfo) { }
    protected virtual void OnCurrentIsPlayingChangedCore(bool value) { }
    protected virtual void OnCurrentPositionChangedCore(TimeSpan value) { }
    protected virtual void OnCurrentMediaSourceProviderInfoChangedCore(MediaSourceProviderInfo? value) { }

    protected abstract IMediaSessionProvider? GetCurrentDesiredSession();

    /// <summary>
    /// 必须在子类构造函数末尾调用，确保子类字段初始化完成后才开始触发钩子。
    /// </summary>
    protected void Initialize()
    {
        InitMediaManager();

        // 系统自动息屏后（Modern Standby 下桌面应用会被挂起），期间的部分 SMTC 事件会丢失；
        // 亮屏后必须主动补齐当前会话状态，否则歌曲信息与封面会一直停留在旧值。
        _monitorProvider.DisplayStatusChanged += MonitorProvider_DisplayStatusChanged;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void MonitorProvider_DisplayStatusChanged(object? sender, bool isDisplayOn)
    {
        if (!isDisplayOn) return;

        _ = RefreshCurrentSessionAfterDisplayResumeAsync();
    }

    private async Task RefreshCurrentSessionAfterDisplayResumeAsync()
    {
        if (_isRefreshingAfterDisplayResume) return;
        _isRefreshingAfterDisplayResume = true;

        try
        {
            var previousSessionId = _currentDesiredSession?.SessionId;

            // 息屏期间可能发生了会话切换（事件在系统挂起时丢失），先重新选取一次当前会话
            OnDesiredSessionChanged();

            var mediaSession = _currentDesiredSession;
            if (mediaSession == null) return;

            await mediaSession.TryRefreshPlaybackStateAsync();
            await mediaSession.TryRefreshMediaPropsAsync();
            await mediaSession.TryRefreshTimelinePropsAsync();

            var snapshot = GetMediaPropsSnapshot(mediaSession);
            var isSongChanged = mediaSession.SessionId != previousSessionId || snapshot != _appliedMediaPropsSnapshot;

            if (isSongChanged)
            {
                // 息屏期间切歌但 SMTC 事件丢失：走一次完整的切歌流程
                _logger.LogDebug("Media properties changed while the display was off; refreshing the current song.");
                await OnAnyMediaPropertyChangedCoreAsync(mediaSession);
            }
            else
            {
                // 歌曲未变化，只是 UI 在息屏/挂起期间没有及时刷新（例如 Win2D 画布被暂停）
                RefreshCurrentMediaPresentation(mediaSession);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh the media session after the display was turned on.");
        }
        finally
        {
            _isRefreshingAfterDisplayResume = false;
        }
    }

    private static (string? Title, string? Artist, string? Album) GetMediaPropsSnapshot(
        IMediaSessionProvider mediaSession) =>
        (mediaSession.Title, mediaSession.Artist, mediaSession.Album);

    private void RefreshCurrentMediaPresentation(IMediaSessionProvider mediaSession)
    {
        // 同一首歌的封面也可能在息屏期间被更新，此时需要重新检索
        var thumbnail = mediaSession.Thumbnail;
        var isThumbnailChanged = thumbnail is { Length: > 0 } && !ReferenceEquals(thumbnail, _smtcAlbumArtBuffer);
        if (isThumbnailChanged)
        {
            _smtcAlbumArtBuffer = thumbnail;
        }

        _appUIThreadProvider.Execute(() =>
        {
            if (!IsCurrentDesiredSession(mediaSession)) return;

            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                CurrentSongInfo.DurationMs = mediaSession.EndTime.TotalMilliseconds;
                UpdateTargetScrobbledDuration();
            }

            CurrentPosition = mediaSession.CurrentTime;

            if (isThumbnailChanged)
            {
                UpdateAlbumArt();
            }
            else
            {
                // 图片字节没有变化时属性不会触发变更通知，这里强制通知一次，让 UI 重新加载封面
                NotifyAlbumArtChanged();
            }

            _logger.LogDebug("Refreshed media presentation after the display was turned on: {Title}.",
                CurrentSongInfo.Title);
        });
    }

    partial void OnIsScrobbledChanged(bool value) =>
        SendMessage(new SongScrobbledMessage(value));

    partial void OnCurrentSongInfoChanged(SongInfo value) =>
        SendMessage(new CurrentSongInfoChangedMessage(value));

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop the current media session.");
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

    protected bool IsMediaSourceEnabled(string id)
    {
        var found = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(s => s.Provider == id);
        return found?.IsEnabled ?? true;
    }

    protected bool IsMediaSourceTimelineSyncEnabled(string? id)
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
            _logger.LogError(ex, "Failed to initialize the Windows media manager.");
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            return;
        }
    }

    protected void OnDesiredSessionChanged(bool firstTime = false)
    {
        var desiredSession = GetCurrentDesiredSession();
        //Debug.WriteLine("OnDesiredSessionChanged: " + desiredSession?.SessionId);
        if (firstTime || desiredSession != _currentDesiredSession)
        {
            _currentDesiredSession = desiredSession;
            _logger.LogDebug("Active media session changed to {SessionId}.", desiredSession?.SessionId);
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

        OnPlaybackStateApplied(isPlaying);

        _appUIThreadProvider.Execute(async () =>
        {
            CurrentIsPlaying = isPlaying;
            OnPresenceUpdateRequested();
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

        _appliedMediaPropsSnapshot = mediaSession == null ? null : GetMediaPropsSnapshot(mediaSession);

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

        // 外部上报相关的资源（如全局内存读取器）由子类按需处理
        OnMediaPropertiesRefreshing(currentMediaSourceProviderInfo);

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
            OnPresenceUpdateRequested();
            OnSongInfoApplied();

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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh the focused media session; clearing the current song.");
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
        _logger.LogDebug("LX Music SSE disconnected. Reconnecting in {Delay}.", e.ReconnectDelay);
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
                        OnPresenceUpdateRequested();
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

    protected void UniversalMemoryReader_OnProgressChanged(double time, double total)
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
        OnCurrentIsPlayingChangedCore(value);
        SendMessage(new CurrentIsPlayingChangedMessage(value));
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        OnCurrentPositionChangedCore(value);
        SendMessage(new CurrentPositionChangedMessage(value));
    }

    partial void OnCurrentMediaSourceProviderInfoChanged(MediaSourceProviderInfo? value)
    {
        OnCurrentMediaSourceProviderInfoChangedCore(value);
        SendMessage(new CurrentMediaSourceProviderChangedMessage(value));
    }
}