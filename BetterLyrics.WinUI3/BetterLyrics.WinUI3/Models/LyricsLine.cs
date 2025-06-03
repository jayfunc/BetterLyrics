using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models {
    public class LyricsLine {

        public List<string> Texts { get; set; } = [];

        public int LanguageIndex { get; set; } = 0;

        public string Text => Texts[LanguageIndex];

        public int StartPlayingTimestampMs { get; set; }
        public int EndPlayingTimestampMs { get; set; }

        public LyricsPlayingState PlayingState { get; set; }

        public int DurationMs => EndPlayingTimestampMs - StartPlayingTimestampMs;

        public float EnteringProgress { get; set; }

        public float ExitingProgress { get; set; }

        public float PlayingProgress { get; set; }

        public Vector2 Position { get; set; }

        public Vector2 CenterPosition { get; set; }

        public float Scale { get; set; }

        public float Opacity { get; set; }

        public CanvasTextLayout TextLayout { get; set; }

    }
}
