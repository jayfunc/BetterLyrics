// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using TagLib.Riff;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public interface IMediaSessionsService
    {
        event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        event EventHandler<TimelineChangedEventArgs>? TimelineChanged;
        event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        event EventHandler<AlbumArtChangedEventArgs>? AlbumArtChanged;
        event EventHandler<LyricsChangedEventArgs>? LyricsChanged;
        event EventHandler<MediaSourceProvidersInfoEventArgs>? MediaSourceProvidersInfoChanged;

        Task PlayAsync();
        Task PauseAsync();
        Task PreviousAsync();
        Task NextAsync();
        Task ChangePosition(double seconds);

        MediaSourceProviderInfo? GetCurrentMediaSourceProviderInfo();

        bool IsPlaying { get; }
        SongInfo? SongInfo { get; }
        TimeSpan Position { get; }
        LyricsData? CurrentLyricsData { get; }
    }
}
