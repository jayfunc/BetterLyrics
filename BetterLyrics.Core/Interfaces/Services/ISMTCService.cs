using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BetterLyrics.Core.Interfaces.Services
{
    /// <summary>
    /// Interface for SystemMediaTransportControlsSession Service
    /// </summary>
    public interface ISMTCService : INotifyPropertyChanged
    {
        public ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; }
        public ExtendedTrack? PlayingTrack { get; set; }

        Task UpdatePlaybackListAsync(IEnumerable<PlayQueueItem> source, bool recoverPlaybackPosition = false, bool allowAutoPlay = false);
        void ApplyPlaybackOrder(PlaybackOrder order);

        void PlayNextTrack();
        void PlayTrack(PlayQueueItem? playQueueItem);
        void PlayTrackAt(int index, bool recoverPlaybackPosition = false);
    }
}
