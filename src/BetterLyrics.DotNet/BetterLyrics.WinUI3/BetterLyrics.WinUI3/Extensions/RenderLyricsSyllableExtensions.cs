using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Lyrics;
using System.Linq;

namespace BetterLyrics.WinUI3.Extensions;

public static class RenderLyricsSyllableExtensions
{
    extension(RenderLyricsSyllable syllable)
    {
        public BaseRenderLyricsSyllable ToBaseRenderLyricsSyllable()
        {
            var ret = (BaseRenderLyricsSyllable)syllable;

            ret.ChildrenRenderLyricsChars = syllable.ChildrenRenderLyricsChars.Select(x => (BaseRenderLyricsChar)x).ToList();

            return ret;
        }
    }
}
