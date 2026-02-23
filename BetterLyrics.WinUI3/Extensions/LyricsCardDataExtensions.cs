using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public class LyricsCardDataExtensions
    {
        public readonly static LyricsCardData DemoLyricsCardData = new()
        {
            SelectedLyrics =
            {
                new LyricsLine { PrimaryText = "曲拨心弦，词落云笺。" },
                new LyricsLine { PrimaryText = "Strums the Heartstrings, Graces the Wordscapes." },
            },
            Title = "BetterLyrics",
            Artist = "BetterLyrics",
            Config = new Models.Settings.LyricsCardConfig { FontFamily = "Segoe UI" },
        };
    }
}
