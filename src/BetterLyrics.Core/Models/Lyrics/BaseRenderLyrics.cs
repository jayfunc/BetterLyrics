namespace BetterLyrics.Core.Models.Lyrics;

public class BaseRenderLyrics : BaseLyrics
{
    public BaseRenderLyrics(BaseLyrics baseLyrics)
    {
        Text = baseLyrics.Text;
        StartMs = baseLyrics.StartMs;
        EndMs = baseLyrics.EndMs;
        StartIndex = baseLyrics.StartIndex;
    }

    public bool IsPlayingLastFrame { get; set; } = false;

    public bool GetIsPlaying(double currentMs)
    {
        return StartMs <= currentMs && currentMs < EndMs;
    }

    public double GetPlayProgress(double currentMs)
    {
        return Math.Clamp((currentMs - StartMs) / DurationMs, 0, 1);
    }
}