// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        LyricsSearchProvider? LyricsSearchProvider { get; }
        TranslationSearchProvider? TranslationSearchProvider { get; }
    }
}
