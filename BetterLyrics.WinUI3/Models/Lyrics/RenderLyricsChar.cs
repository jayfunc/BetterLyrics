using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using System;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsChar : BaseRenderLyrics
    {
        public Rect LayoutRect { get; private set; }

        public ValueTransition<double> ScaleTransition { get; set; }
        public ValueTransition<double> GlowTransition { get; set; }
        public ValueTransition<double> FloatTransition { get; set; }

        public double ProgressPlayed { get; set; } = 0; // 0~1

        public RenderLyricsChar(BaseLyrics lyricsChars, Rect layoutRect) : base(lyricsChars)
        {
            ScaleTransition = new(
                initialValue: 1.0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: Time.AnimationDuration.TotalSeconds
            );
            GlowTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: Time.AnimationDuration.TotalSeconds
            );
            FloatTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: Time.LongAnimationDuration.TotalSeconds
            );
            LayoutRect = layoutRect;
        }

        public void Update(TimeSpan elapsedTime)
        {
            ScaleTransition.Update(elapsedTime);
            GlowTransition.Update(elapsedTime);
            FloatTransition.Update(elapsedTime);
        }

    }
}
