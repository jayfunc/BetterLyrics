// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Numerics;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        public ValueTransition<float> AngleTransition { get; set; } = new(initialValue: 0f, durationSeconds: 0.3f);
        public ValueTransition<float> BlurAmountTransition { get; set; } = new(initialValue: 0f, durationSeconds: 0.3f);
        public ValueTransition<float> HighlightOpacityTransition { get; set; } = new(initialValue: 0f, durationSeconds: 0.3f);
        public ValueTransition<float> OpacityTransition { get; set; } = new(initialValue: 0f, durationSeconds: 0.3f);
        public ValueTransition<float> ScaleTransition { get; set; } = new(initialValue: 0.95f, durationSeconds: 0.3f);

        public CanvasTextLayout? CanvasTextLayout { get; set; }

        public Vector2 CenterPosition { get; set; }
        public Vector2 Position { get; set; }

        public List<CharTiming> CharTimings { get; set; } = [];

        public int DurationMs => EndMs - StartMs;

        public int EndMs { get; set; }

        public int StartMs { get; set; }

        public string Text { get; set; } = "";
    }
}
