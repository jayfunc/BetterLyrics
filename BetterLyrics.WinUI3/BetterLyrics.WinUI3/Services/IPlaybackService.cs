// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services
{
    public interface IPlaybackService
    {
        event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;

        event EventHandler<PositionChangedEventArgs>? PositionChanged;

        event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;

        bool IsPlaying { get; }

        TimeSpan Position { get; }

        SongInfo? SongInfo { get; }
    }
}
