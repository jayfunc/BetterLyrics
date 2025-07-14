// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        private const float _animationDuration = 0.3f;
        public ValueTransition<float> AngleTransition { get; set; } = new(initialValue: 0f, durationSeconds: _animationDuration);
        public ValueTransition<float> BlurAmountTransition { get; set; } = new(initialValue: 0f, durationSeconds: _animationDuration);
        public ValueTransition<float> HighlightOpacityTransition { get; set; } = new(initialValue: 0f, durationSeconds: _animationDuration);
        public ValueTransition<float> OpacityTransition { get; set; } = new(initialValue: 0f, durationSeconds: _animationDuration);
        public ValueTransition<float> ScaleTransition { get; set; } = new(initialValue: 0.95f, durationSeconds: _animationDuration);

        public CanvasTextLayout? CanvasTextLayout { get; set; }

        public Vector2 CenterPosition { get; set; }
        public Vector2 Position { get; set; }

        public List<LyricsChar> LyricsChars { get; set; } = [];

        public int DurationMs => EndMs - StartMs;
        public int EndMs { get; set; }
        public int StartMs { get; set; }

        public string DisplayedText { get; set; } = "";
        public string OriginalText { get; set; } = "";
    }
}
