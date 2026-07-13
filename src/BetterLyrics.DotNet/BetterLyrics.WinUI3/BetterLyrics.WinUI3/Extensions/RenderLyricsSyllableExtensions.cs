using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Lyrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions;

public static class RenderLyricsSyllableExtensions
{
    extension(RenderLyricsSyllable syllable)
    {
        public BaseRenderLyricsSyllable ToBaseRenderLyricsSyllable() => new BaseRenderLyricsSyllable(syllable)
        {
            ChildrenRenderLyricsChars = syllable.ChildrenRenderLyricsChars.Select(x => (BaseRenderLyricsChar)x).ToList()
        };
    }
}
