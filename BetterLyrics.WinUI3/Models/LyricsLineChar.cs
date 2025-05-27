using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models {
    public partial class LyricsLineChar : ObservableObject {
        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private int _startTimestampMs;

        [ObservableProperty]
        private int _endTimestampMs;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private int _durationMs;

        [ObservableProperty]
        private Vector2 _position;

        [ObservableProperty]
        private Vector2 _positionBeforeScrolling;

        [ObservableProperty]
        private Vector2 _centerPosition;

        [ObservableProperty]
        private float _scale;

        [ObservableProperty]
        private float _opacity;
    }
}
