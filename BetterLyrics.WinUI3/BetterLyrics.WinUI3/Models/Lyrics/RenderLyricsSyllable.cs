using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsSyllable : BaseRenderLyrics
    {
        public List<RenderLyricsChar> ChildrenRenderLyricsChars { get; set; } = [];

        public RenderLyricsSyllable(BaseLyrics lyricsSyllable) : base(lyricsSyllable) { }
    }
}
