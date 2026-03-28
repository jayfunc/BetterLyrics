using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SMTCService
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
