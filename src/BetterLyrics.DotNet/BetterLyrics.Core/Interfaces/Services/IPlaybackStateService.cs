using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IPlaybackStateService
{
    PlaybackState CurrentState { get; }
    void TryWriteImmediately();
}