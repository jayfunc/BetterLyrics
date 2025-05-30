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
    public class LyricsLineChild {
        public string Text { get; set; }

        public int StartTimestampMs { get; set; }

        public int EndTimestampMs { get; set; }

        public LyricsPlayingState PlayingState { get; set; }

        public float PlayingProgress { get; set; }

        public int DurationMs => EndTimestampMs - StartTimestampMs;

        public Vector2 Position { get; set; }

        public Vector2 PositionBeforeScrolling { get; set; }

        public Vector2 CenterPosition { get; set; }

        public Rect LayoutBounds { get; set; }

        public float Scale { get; set; }

        public float Opacity { get; set; }
    }
}
