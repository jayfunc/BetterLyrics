using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Models {
    public partial class LyricsLine : ObservableObject {
        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private ObservableCollection<LyricsLineChar> _lyricsLineChars;

        [ObservableProperty]
        private int _startTimestampMs;

        [ObservableProperty]
        private int _endTimestampMs;

        [ObservableProperty]
        private LyricsPlayingState _playingState;

        [ObservableProperty]
        private int _durationMs;

        [ObservableProperty]
        private int _averageDurationPerCharMs;

        [ObservableProperty]
        private float _enteringProgress;

        [ObservableProperty]
        private float _exitingProgress;
    }
}
