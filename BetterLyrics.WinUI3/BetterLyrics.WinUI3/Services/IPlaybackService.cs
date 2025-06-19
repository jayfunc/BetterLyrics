using System;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services
{
    public interface IPlaybackService
    {
        event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;
        event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;
        event EventHandler<PositionChangedEventArgs>? PositionChanged;

        void ReSendingMessages();
        SongInfo? SongInfo { get; }
        bool IsPlaying { get; }
        TimeSpan Position { get; }
    }
}
