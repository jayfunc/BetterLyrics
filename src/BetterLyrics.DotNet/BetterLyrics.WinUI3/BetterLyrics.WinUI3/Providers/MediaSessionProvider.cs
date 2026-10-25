using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class MediaSessionProvider : IMediaSessionProvider
{
    private readonly MediaManager.MediaSession _session;
    private readonly ILogger<MediaSessionProvider> _logger;

    public string SessionId { get; }

    public string? Title { get; private set; }

    public string? Artist { get; private set; }

    public string? Album { get; private set; }

    public List<string>? Genres { get; private set; }

    public byte[]? Thumbnail { get; private set; }

    public SessionPlaybackStatus PlaybackStatus { get; private set; }

    public TimeSpan CurrentTime { get; private set; }

    public TimeSpan EndTime { get; private set; }

    public MediaSessionProvider(MediaManager.MediaSession session, ILogger<MediaSessionProvider> logger)
    {
        _session = session;
        _logger = logger;
        SessionId = session.Id;

        // 初始化播放状态：MediaManager.Start() 期间的播放状态事件在 BetterLyrics 完成订阅前就已发出，
        // 若不在此读取，所有已存在的会话都会停留在默认的 Stopped，导致“正在播放的播放源优先”在选择时失效。
        RefreshPlaybackStatus();
    }

    public async Task TryRefreshMediaPropsAsync()
    {
        try
        {
            var mediaProperties = await _session.ControlSession.TryGetMediaPropertiesAsync();
            Title = mediaProperties.Title;
            Artist = mediaProperties.Artist;
            Album = mediaProperties.AlbumTitle;

            Genres = mediaProperties.Genres.ToList();
            Thumbnail = await mediaProperties.Thumbnail.ToByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh media properties for session {SessionId}.", SessionId);
        }
    }

    private void RefreshPlaybackStatus()
    {
        try
        {
            var playbackInfo = _session.ControlSession?.GetPlaybackInfo();
            if (playbackInfo != null)
            {
                PlaybackStatus = playbackInfo.PlaybackStatus switch
                {
                    Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => SessionPlaybackStatus.Playing,
                    Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => SessionPlaybackStatus.Paused,
                    _ => SessionPlaybackStatus.Stopped
                };
            }
            else
            {
                PlaybackStatus = SessionPlaybackStatus.Stopped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh playback state for session {SessionId}.", SessionId);
            PlaybackStatus = SessionPlaybackStatus.Stopped;
        }
    }

    public Task TryRefreshPlaybackStateAsync()
    {
        RefreshPlaybackStatus();
        return Task.CompletedTask;
    }

    public async Task TryRefreshTimelinePropsAsync()
    {
        try
        {
            var timelineProperties = _session.ControlSession.GetTimelineProperties();
            CurrentTime = timelineProperties.Position;
            EndTime = timelineProperties.EndTime;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh timeline properties for session {SessionId}.", SessionId);
        }
    }

    public async Task TryChangePlaybackPositionAsync(TimeSpan timeSpan)
    {
        await _session.ControlSession.TryChangePlaybackPositionAsync(timeSpan.Ticks);
    }

    public async Task TryPauseAsync()
    {
        await _session.ControlSession.TryPauseAsync();
    }

    public async Task TryPlayAsync()
    {
        await _session.ControlSession.TryPlayAsync();
    }

    public async Task TrySkipNextAsync()
    {
        await _session.ControlSession.TrySkipNextAsync();
    }

    public async Task TrySkipPreviousAsync()
    {
        await _session.ControlSession.TrySkipPreviousAsync();
    }

    public async Task TryStopAsync()
    {
        await _session.ControlSession.TryStopAsync();
    }
}
