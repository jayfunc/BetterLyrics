using System.Linq;
using BetterLyrics.Avalonia.Models.Lyrics;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Avalonia.Extensions;

public static class RenderLyricsLineExtensions
{
    public static BaseRenderLyricsLine ToBaseRenderLyricsLine(this RenderLyricsLine line)
    {
        var ret = (BaseRenderLyricsLine)line;

        ret.PrimaryRenderChars =
            line.PrimaryRenderChars.Select(x => (BaseRenderLyricsChar)x).ToList();
        ret.PrimaryRenderSyllables =
            line.PrimaryRenderSyllables.Select(x => (BaseRenderLyricsSyllable)x).ToList();

        // Avalonia's TextLayout uses Width and Height directly instead of a LayoutBounds property
        ret.PrimaryTextLayoutBounds = line.PrimaryTextLayout != null
            ? new AppRect(0, 0, line.PrimaryTextLayout.Width, line.PrimaryTextLayout.Height)
            : null;

        ret.SecondaryTextLayoutBounds = line.SecondaryTextLayout != null
            ? new AppRect(0, 0, line.SecondaryTextLayout.Width, line.SecondaryTextLayout.Height)
            : null;

        ret.TertiaryTextLayoutBounds = line.TertiaryTextLayout != null
            ? new AppRect(0, 0, line.TertiaryTextLayout.Width, line.TertiaryTextLayout.Height)
            : null;

        return ret;
    }
}