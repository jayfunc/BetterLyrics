namespace BetterLyrics.Core.Models.Lyrics;

public class BaseRenderLyricsSyllable : BaseRenderLyrics
{
    public BaseRenderLyricsSyllable(BaseLyrics lyricsSyllable) : base(lyricsSyllable)
    {
    }

    public List<BaseRenderLyricsChar> ChildrenRenderLyricsChars { get; set; } = [];
}