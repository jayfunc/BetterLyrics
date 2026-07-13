using BetterLyrics.Core.Interfaces.Providers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Providers;

public class MediaManagerProvider : IMediaManagerProvider
{
    private readonly MediaManager _mediaManager = new();
    private readonly ConcurrentDictionary<string, IMediaSessionProvider> _mediaSessions = new();

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

    public void Init()
    {
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
        }
        // Add new sessions
        foreach (var sessionId in currentSessionIds.Except(existingSessionIds))
        {
            var session = _mediaManager.CurrentMediaSessions[sessionId];
            var mediaSessionProvider = new MediaSessionProvider(session);
            _mediaSessions.TryAdd(sessionId, mediaSessionProvider);
        }
    }
}
