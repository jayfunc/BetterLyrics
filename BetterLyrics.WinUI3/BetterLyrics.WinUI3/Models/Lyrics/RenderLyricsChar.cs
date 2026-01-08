using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsChar : LyricsChar
    {
        public Rect LayoutRect { get; set; }

        public double AnimationDuration { get; set; } = 0.3;

        public ValueTransition<double> ScaleTransition { get; set; }
        public ValueTransition<double> GlowTransition { get; set; }
        public ValueTransition<double> FloatTransition { get; set; }

        public double ProgressPlayed { get; set; } = 0; // 0~1

        public bool IsPlayingLastFrame { get; set; } = false;

        public RenderLyricsChar()
        {
            ScaleTransition = new(
                initialValue: 1.0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            GlowTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            FloatTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
        }
    }
}
