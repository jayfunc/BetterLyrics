using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsLine : ObservableObject
    {
        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private int _startTimestampMs;

        [ObservableProperty]
        private int _endTimestampMs;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private double _playedProgress = 0;

        public int DurationMs { get => EndTimestampMs - StartTimestampMs; }
    }
}
