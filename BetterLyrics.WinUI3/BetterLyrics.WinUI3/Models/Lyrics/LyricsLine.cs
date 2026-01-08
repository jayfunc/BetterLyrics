// 2025/6/23 by Zhe Fang

using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class LyricsLine
    {
        public List<LyricsSyllable> LyricsSyllables { get; set; } = [];

        public int? DurationMs => EndMs - StartMs;
        public int? EndMs { get; set; }
        public int StartMs { get; set; }

        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText { get; set; } = "";
        /// <summary>
        /// 译文
        /// </summary>
        public string TranslatedText { get; set; } = "";
        /// <summary>
        /// 注音
        /// </summary>
        public string PhoneticText { get; set; } = "";

    }
}
