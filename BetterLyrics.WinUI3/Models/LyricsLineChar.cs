using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models {
    public partial class LyricsLineChar {
        public string Text;

        public int StartTimestampMs;

        public int EndTimestampMs;

        public LyricsPlayingState PlayingState;

        public float PlayingProgress;

        public int DurationMs;

        public Vector2 Position;

        public Vector2 PositionBeforeScrolling;

        public Vector2 CenterPosition;

        public Rect LayoutBounds;

        public float Scale;

        public float Opacity;
    }
}
