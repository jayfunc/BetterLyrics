using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
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

        public LyricsLine Clone()
        {
            return new LyricsLine
            {
                Texts = new List<string>(this.Texts),
                LanguageIndex = this.LanguageIndex,
                StartPlayingTimestampMs = this.StartPlayingTimestampMs,
                EndPlayingTimestampMs = this.EndPlayingTimestampMs,
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
