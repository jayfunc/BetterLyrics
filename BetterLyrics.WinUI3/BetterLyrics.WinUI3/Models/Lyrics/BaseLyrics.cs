using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class BaseLyrics
    {
        public int StartMs { get; set; }
        public int EndMs { get; set; }
        public int DurationMs => EndMs - StartMs;

        public string Text { get; set; } = "";
        public int Length => Text.Length;

        public int StartIndex { get; set; }
        public int EndIndex => StartIndex + Length - 1;

    }
}
