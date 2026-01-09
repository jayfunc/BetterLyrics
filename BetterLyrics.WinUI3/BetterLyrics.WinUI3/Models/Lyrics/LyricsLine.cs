// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class LyricsLine : BaseLyrics
    {
        public List<BaseLyrics> PrimarySyllables { get; set; } = [];
        public List<BaseLyrics> SecondarySyllables { get; set; } = [];
        public List<BaseLyrics> TertiarySyllables { get; set; } = [];

        public List<BaseLyrics> PrimaryChars { get; private set; } = [];
        public List<BaseLyrics> SecondaryChars { get; private set; } = [];
        public List<BaseLyrics> TertiaryChars { get; private set; } = [];

        public string PrimaryText { get; set; } = "";
        public string SecondaryText { get; set; } = "";
        public string TertiaryText { get; set; } = "";

        public new string Text => PrimaryText;
        public new int StartIndex = 0;

        public LyricsLine()
        {
            for (int charStartIndex = 0; charStartIndex < PrimaryText.Length; charStartIndex++)
            {
                var syllable = PrimarySyllables.FirstOrDefault(x => x.StartIndex <= charStartIndex && charStartIndex <= x.EndIndex);
                if (syllable == null) continue;

                var avgCharDuration = syllable.DurationMs / syllable.Length;
                if (avgCharDuration == 0) continue;

                var charStartMs = syllable.StartMs + (charStartIndex - syllable.StartIndex) * avgCharDuration;
                var charEndMs = charStartMs + avgCharDuration;

                PrimaryChars.Add(new BaseLyrics
                {
                    StartIndex = charStartIndex,
                    StartMs = charStartMs,
                    EndMs = charEndMs,
                    Text = PrimaryText[charStartIndex].ToString()
                });
            }
        }

    }
}
