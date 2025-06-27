// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Numerics;
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

        /// <summary>
        /// Gets or sets the BlurAmountTransition
        /// </summary>
        public ValueTransition<float> BlurAmountTransition { get; set; } =
            new(initialValue: 0f, durationSeconds: 0.3f);

        /// <summary>
        /// Gets or sets the CanvasTextLayout
        /// </summary>
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

        public ValueTransition<float> HighlightOpacityTransition { get; set; } =
            new(initialValue: 0f, durationSeconds: 0.3f);

        /// <summary>
        /// Gets or sets the Position
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets or sets the ScaleTransition
        /// </summary>
        public ValueTransition<float> ScaleTransition { get; set; } =
            new(initialValue: 0.95f, durationSeconds: 0.3f);

        /// <summary>
        /// Gets or sets the StartMs
        /// </summary>
        public int StartMs { get; set; }

        /// <summary>
        /// Gets or sets the Text
        /// </summary>
        public string Text { get; set; } = "";

        #endregion
    }
}
