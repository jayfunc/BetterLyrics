using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;

namespace BetterLyrics.WinUI3.Extensions
{
    public class LyricsCardDataExtensions
    {
        public readonly static LyricsCardData DemoLyricsCardData = new()
        {
            Lyrics =
            {
                new LyricsLine { PrimaryText = "曲拨心弦，词落云笺。" },
                new LyricsLine { PrimaryText = "Strums the Heartstrings, Graces the Wordscapes." },
            },
            Title = "BetterLyrics",
            Artist = "BetterLyrics",
        };
    }
}
