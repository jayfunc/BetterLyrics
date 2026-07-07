using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Extensions;

public class LyricsCardDataExtensions
{
    public static readonly LyricsCardData DemoLyricsCardData = new()
    {
        Lyrics =
        {
            new LyricsLine { PrimaryText = "曲拨心弦，词落云笺。" },
            new LyricsLine { PrimaryText = "Strums the Heartstrings, Graces the Wordscapes." }
        },
        Title = "BetterLyrics",
        Artist = "BetterLyrics"
    };
}