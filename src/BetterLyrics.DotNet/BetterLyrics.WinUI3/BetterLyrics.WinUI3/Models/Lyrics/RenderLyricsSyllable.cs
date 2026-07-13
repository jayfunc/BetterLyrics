using System.Collections.Generic;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.WinUI3.Models.Lyrics;

public class RenderLyricsSyllable : BaseRenderLyricsSyllable
{
    public RenderLyricsSyllable(BaseLyrics lyricsSyllable) : base(lyricsSyllable)
    {
    }

    public new List<RenderLyricsChar> ChildrenRenderLyricsChars { get; set; } = [];
}