// 2025/6/23 by Zhe Fang

using System;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services
{
    public interface IPlaybackService
    {
        event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        event EventHandler<PositionChangedEventArgs>? PositionChanged;
        event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        event EventHandler<AlbumArtChangedEventArgs>? AlbumArtChangedChanged;
        event EventHandler<MediaSourceProvidersInfoEventArgs>? MediaSourceProvidersInfoChanged;

        Task PlayAsync();
        Task PauseAsync();
        Task PreviousAsync();
        Task NextAsync();
        Task ChangePosition(double seconds);

        bool IsPlaying { get; }
        SongInfo? SongInfo { get; }
    }
}
