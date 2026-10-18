using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BetterLyrics.Core.Interfaces.Services;

/// <summary>
/// Interface for SystemMediaTransportControlsSession Service
/// </summary>
public interface ISmtcService : INotifyPropertyChanged
{
    bool IsVolumeRestored { get; set; }
    bool IsPlaying { get; }

    public ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; }
    public ExtendedTrack? PlayingTrack { get; set; }
    public PlaybackState PlaybackState { get; }

    Task UpdatePlaybackListAsync(IEnumerable<PlayQueueItem> source, bool recoverPlaybackPosition = false, bool allowAutoPlay = false);

    void ApplyPlaybackOrder(PlaybackOrder order);

    void Play();
    void Pause();
    void PlayNextTrack();
    void PlayPreviousTrack();
    void PlayTrack(PlayQueueItem? playQueueItem);
    void PlayTrackAt(int index, bool recoverPlaybackPosition = false);
    void SaveCurrentPlaybackPosition();
}