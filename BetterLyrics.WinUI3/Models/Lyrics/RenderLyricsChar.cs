using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Lyrics;
using Microsoft.Graphics.Canvas.Effects;
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

        public CropEffect Crop { get; }
        public GaussianBlurEffect Glow { get; }

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
            Crop = new CropEffect { BorderMode = EffectBorderMode.Hard };
            Glow = new GaussianBlurEffect { Source = Crop, BorderMode = EffectBorderMode.Soft };
        }

        public void Update(TimeSpan elapsedTime)
        {
            ScaleTransition.Update(elapsedTime);
            GlowTransition.Update(elapsedTime);
            FloatTransition.Update(elapsedTime);
        }

        public void DisposeEffetcts()
        {
            Crop?.Dispose();
            Glow?.Dispose();
        }

    }
}
