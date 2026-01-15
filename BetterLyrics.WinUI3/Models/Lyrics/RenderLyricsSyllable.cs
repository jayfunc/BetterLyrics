using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsSyllable : BaseRenderLyrics
    {
        public List<RenderLyricsChar> ChildrenRenderLyricsChars { get; set; } = [];

        public RenderLyricsSyllable(BaseLyrics lyricsSyllable) : base(lyricsSyllable) { }
    }
}
