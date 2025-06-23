using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        public string Text { get; set; } = "";

        public List<CharTiming> CharTimings { get; set; } = [];

        public int StartMs { get; set; }
        public int EndMs { get; set; }

        public LyricsPlayingState PlayingState { get; set; }

        public int DurationMs => EndMs - StartMs;

        public float EnteringProgress { get; set; }

        public float ExitingProgress { get; set; }

        public float PlayingProgress { get; set; }

        public Vector2 Position { get; set; }

        public Vector2 CenterPosition { get; set; }

        public float Scale { get; set; }

        public float Opacity { get; set; }

        public LyricsLine Clone()
        {
            return new LyricsLine
            {
                Text = this.Text,
                CharTimings = this.CharTimings,
                StartMs = this.StartMs,
                EndMs = this.EndMs,
                PlayingState = this.PlayingState,
                EnteringProgress = this.EnteringProgress,
                ExitingProgress = this.ExitingProgress,
                PlayingProgress = this.PlayingProgress,
                Position = this.Position,
                CenterPosition = this.CenterPosition,
                Scale = this.Scale,
                Opacity = this.Opacity,
            };
        }
    }
}
