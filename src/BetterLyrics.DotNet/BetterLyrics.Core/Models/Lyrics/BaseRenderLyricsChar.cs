using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Models.Lyrics;

public class BaseRenderLyricsChar : BaseRenderLyrics
{
    public BaseRenderLyricsChar(BaseLyrics lyricsChars, AppRect layoutRect) : base(lyricsChars)
    {
        ScaleTransition = new ValueTransition<double>(
            1.0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            Time.AnimationDuration.TotalSeconds
        );
        GlowTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            Time.AnimationDuration.TotalSeconds
        );
        FloatTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            Time.LongAnimationDuration.TotalSeconds
        );
        LayoutRect = layoutRect;
    }

    public AppRect LayoutRect { get; private set; }

    public ValueTransition<double> ScaleTransition { get; set; }
    public ValueTransition<double> GlowTransition { get; set; }
    public ValueTransition<double> FloatTransition { get; set; }

    public double ProgressPlayed { get; set; } = 0; // 0~1

    public void Update(TimeSpan elapsedTime)
    {
        ScaleTransition.Update(elapsedTime);
        GlowTransition.Update(elapsedTime);
        FloatTransition.Update(elapsedTime);
    }
}