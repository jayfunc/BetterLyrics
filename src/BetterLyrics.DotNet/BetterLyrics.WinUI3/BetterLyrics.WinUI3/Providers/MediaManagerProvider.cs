using BetterLyrics.Core.Interfaces.Providers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class MediaManagerProvider : IMediaManagerProvider
{
    private readonly MediaManager _mediaManager = new();
    private readonly ConcurrentDictionary<string, IMediaSessionProvider> _mediaSessions = new();
    private readonly ILogger<MediaManagerProvider> _logger;
    private readonly ILogger<MediaSessionProvider> _sessionLogger;

    public MediaManagerProvider(ILogger<MediaManagerProvider> logger,
        ILogger<MediaSessionProvider> sessionLogger)
    {
        _logger = logger;
        _sessionLogger = sessionLogger;
    }

    public IMediaSessionProvider? FocusedSession
    {
        get
        {
            var focusedSession = _mediaManager.GetFocusedSession();
            if (focusedSession != null)
            {
                return TryGetIMediaSessionProvider(focusedSession.Id);
            }
            return null;
        }
    }

    public IEnumerable<IMediaSessionProvider> CurrentMediaSessions
    {
        get
        {
            SyncMediaSessions();
            return _mediaSessions.Values;
        }
    }

    public event IMediaManagerProvider.SessionChangeDelegate? OnAnySessionOpened;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnySessionClosed;
    public event IMediaManagerProvider.SessionChangeDelegate? OnFocusedSessionChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyMediaPropertyChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyPlaybackStateChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyTimelinePropertyChanged;

    private bool _isInitialized;

    public void Init()
    {
        // 多个 GSMTC 服务实例共享同一个提供者，只需启动一次
        if (_isInitialized) return;
        _isInitialized = true;

        _logger.LogDebug("Starting Windows media manager.");
        _mediaManager.Start();

        _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
        _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
        _mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;

        _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
        _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
        _mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;
    }

    public bool IsMediaSessionExisting(string sessionId)
    {
        return _mediaManager.CurrentMediaSessions.ContainsKey(sessionId);
    }

    private IMediaSessionProvider? TryGetIMediaSessionProvider(string? sessionId)
    {
        if (sessionId == null) return null;

        SyncMediaSessions();

        if (_mediaSessions.TryGetValue(sessionId, out var mediaSessionProvider))
        {
            return mediaSessionProvider;
        }

        return null;
    }

    private void MediaManager_OnAnySessionOpened(MediaManager.MediaSession? mediaSession)
    {
        OnAnySessionOpened?.Invoke(TryGetIMediaSessionProvider(mediaSession.Id));
    }

    private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession? mediaSession)
    {
        OnAnySessionClosed?.Invoke(TryGetIMediaSessionProvider(mediaSession.Id));
    }

    private void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession? mediaSession)
    {
        OnFocusedSessionChanged?.Invoke(TryGetIMediaSessionProvider(mediaSession?.Id));
    }

    private void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession? mediaSession, Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
    {
        OnAnyMediaPropertyChanged?.Invoke(TryGetIMediaSessionProvider(mediaSession.Id));
    }

    private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession? mediaSession, Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        OnAnyPlaybackStateChanged?.Invoke(TryGetIMediaSessionProvider(mediaSession.Id));
    }

    private void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession? mediaSession, Windows.Media.Control.GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties)
    {
        OnAnyTimelinePropertyChanged?.Invoke(TryGetIMediaSessionProvider(mediaSession.Id));
    }

    private void SyncMediaSessions()
    {
        var currentSessionIds = _mediaManager.CurrentMediaSessions.Keys;
        var existingSessionIds = _mediaSessions.Keys;
        // Remove closed sessions
        foreach (var sessionId in existingSessionIds.Except(currentSessionIds))
        {
            _mediaSessions.TryRemove(sessionId, out _);
            _logger.LogDebug("Media session closed: {SessionId}.", sessionId);
        }
        // Add new sessions
        foreach (var sessionId in currentSessionIds.Except(existingSessionIds))
        {
            var session = _mediaManager.CurrentMediaSessions[sessionId];
            var mediaSessionProvider = new MediaSessionProvider(session, _sessionLogger);
            _mediaSessions.TryAdd(sessionId, mediaSessionProvider);
            _logger.LogDebug("Media session opened: {SessionId}.", sessionId);
        }
    }
}
