// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="LyricsLine" />
    /// </summary>
    public class LyricsLine
    {
        #region Properties

        public CanvasTextLayout? CanvasTextLayout { get; set; }

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

        public ValueTransition<float> BlurAmountTransition { get; set; } =
            new(initialValue: 0f, durationSeconds: 0.3f);

        #endregion
    }
}
