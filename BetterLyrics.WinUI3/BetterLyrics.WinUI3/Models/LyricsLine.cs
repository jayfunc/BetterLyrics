// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using System.Collections.Generic;
using System.Numerics;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="LyricsLine" />
    /// </summary>
    public class LyricsLine
    {
        #region Properties

        /// <summary>
        /// Gets or sets the CenterPosition
        /// </summary>
        public Vector2 CenterPosition { get; set; }

        /// <summary>
        /// Gets or sets the CharTimings
        /// </summary>
        public List<CharTiming> CharTimings { get; set; } = [];

        /// <summary>
        /// Gets the DurationMs
        /// </summary>
        public int DurationMs => EndMs - StartMs;

        /// <summary>
        /// Gets or sets the EndMs
        /// </summary>
        public int EndMs { get; set; }

        /// <summary>
        /// Gets or sets the EnteringProgress
        /// </summary>
        public float EnteringProgress { get; set; }

        /// <summary>
        /// Gets or sets the ExitingProgress
        /// </summary>
        public float ExitingProgress { get; set; }

        /// <summary>
        /// Gets or sets the Opacity
        /// </summary>
        public float Opacity { get; set; }

        /// <summary>
        /// Gets or sets the PlayingProgress
        /// </summary>
        public float PlayingProgress { get; set; }

        /// <summary>
        /// Gets or sets the PlayingState
        /// </summary>
        public LyricsPlayingState PlayingState { get; set; }

        /// <summary>
        /// Gets or sets the Position
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets or sets the Scale
        /// </summary>
        public float Scale { get; set; }

        /// <summary>
        /// Gets or sets the StartMs
        /// </summary>
        public int StartMs { get; set; }

        /// <summary>
        /// Gets or sets the Text
        /// </summary>
        public string Text { get; set; } = "";

        #endregion

        #region Methods

        /// <summary>
        /// The Clone
        /// </summary>
        /// <returns>The <see cref="LyricsLine"/></returns>
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

        #endregion
    }
}
