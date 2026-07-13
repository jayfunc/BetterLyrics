using System.Linq;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Lyrics;

namespace BetterLyrics.WinUI3.Extensions;

public static class RenderLyricsLineExtensions
{
    extension(RenderLyricsLine line)
    {
        public BaseRenderLyricsLine ToBaseRenderLyricsLine()
        {
            var ret = (BaseRenderLyricsLine)line;

            ret.PrimaryRenderChars =
                line.PrimaryRenderChars.Select(x => (BaseRenderLyricsChar)x).ToList();
            ret.PrimaryRenderSyllables =
                line.PrimaryRenderSyllables.Select(x => x.ToBaseRenderLyricsSyllable()).ToList();

            ret.PrimaryTextLayoutBounds = line.PrimaryTextLayout?.LayoutBounds.ToAppRect();
            ret.SecondaryTextLayoutBounds = line.SecondaryTextLayout?.LayoutBounds.ToAppRect();
            ret.TertiaryTextLayoutBounds = line.TertiaryTextLayout?.LayoutBounds.ToAppRect();

            return ret;
        }
    }
}