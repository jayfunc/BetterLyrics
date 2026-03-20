using BetterLyrics.WinUI3.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SMTCService
{
    /// <summary>
    /// Interface for SystemMediaTransportControlsSession Service
    /// </summary>
    public interface ISMTCService
    {
        public ObservableCollection<PlayQueueItem> TrackPlayingQueue { get; set; }
        public ExtendedTrack? PlayingTrack { get; set; }

        public void PlayNextTrack();

        Task PlayTrackAsync(PlayQueueItem? playQueueItem);
        Task PlayTrackAtAsync(int index);
    }
}
