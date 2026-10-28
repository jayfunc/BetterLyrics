// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Models.Memory;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

/// <summary>
/// Singleton Service
/// </summary>
public class GsmtcService : GsmtcServiceBase,
    IRecipient<MediaSourceProviderEnabledChangedMessage>,
    IRecipient<MediaSourceDiscordPresenceChangedMessage>,
    IRecipient<LyricsWindowStateChangedMessage>,
    IRecipient<DiscordAlbumArtSourceChangedMessage>
{
    private readonly IDiscordService _discordService;
    private readonly ILastFmService _lastFmService;
    private readonly IPlayHistoryService _playHistoryService;
    private readonly Timer _scrobbleTimer;

    private readonly IUniversalMemoryReaderProvider _memoryReader =
        Ioc.Default.GetRequiredService<IUniversalMemoryReaderProvider>();

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
        : base(settingsService, albumArtSearchService, lyricsSearchService, libreTranslateService,
            transliterationService, appUiThreadProvider, logger, globalToastProvider, windowManagerProvider)
    {
        _discordService = discordService;
        _playHistoryService = playHistoryService;
        _lastFmService = lastFmService;

        _scrobbleTimer = new Timer(ScrobbleTimerCallback, null, Timeout.Infinite, 1000);

        Initialize();
    }

    public void Receive(MediaSourceProviderEnabledChangedMessage message)
    {
        OnDesiredSessionChanged();
    }

    public void Receive(MediaSourceDiscordPresenceChangedMessage message)
    {
        _ = UpdateDiscordPresenceAsync();
    }

    public void Receive(LyricsWindowStateChangedMessage message)
    {
        OnDesiredSessionChanged();
    }

    public void Receive(DiscordAlbumArtSourceChangedMessage message)
    {
        CurrentSongInfo.AlbumArtUrl = null;
        _ = UpdateDiscordPresenceAsync();
    }

    protected override void OnPlaybackStateApplied(bool isPlaying)
    {
        if (isPlaying)
        {
            _scrobbleTimer.Change(0, 1000);
        }
        else
        {
            _scrobbleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    protected override void OnPresenceUpdateRequested()
    {
        _ = UpdateDiscordPresenceAsync();
    }

    protected override void OnSongInfoApplied()
    {
        var isLastFMEnabled = CurrentMediaSourceProviderInfo?.IsLastFMTrackEnabled ?? false;
        if (isLastFMEnabled)
        {
            _ = Task.Run(() => _lastFmService.UpdateNowPlayingAsync(CurrentSongInfo));
            _logger.LogInformation("MediaManager_OnAnyMediaPropertyChanged: {Title} update now playing to last.fm", CurrentSongInfo.Title);
        }
    }

    protected override void OnMediaPropertiesRefreshing(MediaSourceProviderInfo? providerInfo)
    {
        // 总是先停止 _memoryReader
        _memoryReader.Stop();

        // 注册
        if (providerInfo?.IsMemoryReaderEnabled == true)
        {
            if (providerInfo.MemoryReaderConfig is MemoryReaderConfig config)
            {
                _memoryReader.Config = config;
                _memoryReader.Start();
                _memoryReader.OnProgressChanged += UniversalMemoryReader_OnProgressChanged;
            }
        }
    }

    protected override void OnCurrentIsPlayingChangedCore(bool value)
    {
        _windowManagerProvider.SetTaskbarProgressState(WindowType.NowPlayingWindow, value);
    }

    protected override void OnCurrentPositionChangedCore(TimeSpan value)
    {
        _windowManagerProvider.SetTaskbarProgressValue(WindowType.NowPlayingWindow, value.TotalSeconds / CurrentSongInfo.Duration);
    }

    protected override void OnCurrentMediaSourceProviderInfoChangedCore(MediaSourceProviderInfo? value)
    {
        foreach (var item in _settingsService.AppSettings.MediaSourceProvidersInfo)
        {
            item.IsFocused = item.Provider == value?.Provider;
        }
    }

    protected override IMediaSessionProvider? GetCurrentDesiredSession()
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
}
