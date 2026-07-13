using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WindowsMediaController;

namespace BetterLyrics.WinUI3.Providers;

public class MediaSessionProvider : IMediaSessionProvider
{
    private readonly MediaManager.MediaSession _session;

    public string SessionId { get; }

    public string? Title { get; private set; }

    public string? Artist { get; private set; }

    public string? Album { get; private set; }

    public List<string>? Genres { get; private set; }

    public byte[]? Thumbnail { get; private set; }

    public SessionPlaybackStatus PlaybackStatus { get; private set; }

    public TimeSpan CurrentTime { get; private set; }

    public TimeSpan EndTime { get; private set; }

    public MediaSessionProvider(MediaManager.MediaSession session)
    {
        _session = session;
        SessionId = session.Id;
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
        catch (Exception)
        {
        }
    }

    public async Task TryRefreshPlaybackStateAsync()
    {
        try
        {
            var playbackStatus = _session.ControlSession.GetPlaybackInfo().PlaybackStatus;
            PlaybackStatus = playbackStatus switch
            {
                Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => SessionPlaybackStatus.Playing,
                Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => SessionPlaybackStatus.Paused,
                _ => SessionPlaybackStatus.Stopped
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing playback state: {ex.Message}");
        }
    }

    public async Task TryRefreshTimelinePropsAsync()
    {
        try
        {
            var timelineProperties = _session.ControlSession.GetTimelineProperties();
            CurrentTime = timelineProperties.Position;
            EndTime = timelineProperties.EndTime;
        }
        catch (Exception)
        {
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
