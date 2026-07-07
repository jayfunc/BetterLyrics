using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Providers;

public class MediaSessionProvider : IMediaSessionProvider
{
    public string SessionId => throw new NotImplementedException();

    public string? Title => throw new NotImplementedException();

    public string? Artist => throw new NotImplementedException();

    public string? Album => throw new NotImplementedException();

    public List<string>? Genres => throw new NotImplementedException();

    public byte[]? Thumbnail => throw new NotImplementedException();

    public SessionPlaybackStatus PlaybackStatus => throw new NotImplementedException();

    public TimeSpan CurrentTime => throw new NotImplementedException();

    public TimeSpan EndTime => throw new NotImplementedException();

    public Task TryChangePlaybackPositionAsync(TimeSpan timeSpan)
    {
        throw new NotImplementedException();
    }

    public Task TryPauseAsync()
    {
        throw new NotImplementedException();
    }

    public Task TryPlayAsync()
    {
        throw new NotImplementedException();
    }

    public Task TryRefreshMediaPropsAsync()
    {
        throw new NotImplementedException();
    }

    public Task TryRefreshPlaybackStateAsync()
    {
        throw new NotImplementedException();
    }

    public Task TryRefreshTimelinePropsAsync()
    {
        throw new NotImplementedException();
    }

    public Task TrySkipNextAsync()
    {
        throw new NotImplementedException();
    }

    public Task TrySkipPreviousAsync()
    {
        throw new NotImplementedException();
    }

    public Task TryStopAsync()
    {
        throw new NotImplementedException();
    }
}
