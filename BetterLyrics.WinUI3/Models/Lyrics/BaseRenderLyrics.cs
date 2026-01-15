using System;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class BaseRenderLyrics : BaseLyrics
    {
        public bool IsPlayingLastFrame { get; set; } = false;

        public BaseRenderLyrics(BaseLyrics baseLyrics)
        {
            this.Text = baseLyrics.Text;
            this.StartMs = baseLyrics.StartMs;
            this.EndMs = baseLyrics.EndMs;
            this.StartIndex = baseLyrics.StartIndex;
        }

        public bool GetIsPlaying(double currentMs) => this.StartMs <= currentMs && currentMs < this.EndMs;
        public double GetPlayProgress(double currentMs) => Math.Clamp((currentMs - this.StartMs) / this.DurationMs, 0, 1);
    }
}
