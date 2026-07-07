using BetterLyrics.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IMediaSessionProvider
    {
        string SessionId { get; }

        string? Title { get; }
        string? Artist { get; }
        string? Album { get; }
        List<string>? Genres { get; }
        byte[]? Thumbnail { get; }

        SessionPlaybackStatus PlaybackStatus { get; }
        TimeSpan CurrentTime { get; }
        TimeSpan EndTime { get; }

        Task TryRefreshMediaPropsAsync();
        Task TryRefreshPlaybackStateAsync();
        Task TryRefreshTimelinePropsAsync();

        Task TryPlayAsync();
        Task TryPauseAsync();
        Task TryStopAsync();
        Task TrySkipPreviousAsync();
        Task TrySkipNextAsync();
        Task TryChangePlaybackPositionAsync(TimeSpan timeSpan);
    }
}
