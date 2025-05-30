using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Models {
    public class LyricsLine {
        public string Text { get; set; }

        /// <summary>
        /// A list of every single char in this lyrics line
        /// </summary>
        public List<LyricsLineChild> LyricsLineChars { get; set; } = [];

        /// <summary>
        /// A list of sub lines in this lyrics line
        /// </summary>
        public List<LyricsLineChild> LyricsSubLines { get; set; } = [];

        public int StartTimestampMs { get; set; }

        public int EndTimestampMs { get; set; }

        public LyricsPlayingState PlayingState { get; set; }

        public int DurationMs { get; set; }

        public int AverageDurationPerCharMs { get; set; }

        public float EnteringProgress { get; set; }

        public float ExitingProgress { get; set; }
    }
}
