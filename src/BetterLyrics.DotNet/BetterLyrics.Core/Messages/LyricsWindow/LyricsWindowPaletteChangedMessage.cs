using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowPaletteChangedMessage(LyricsWindowStatus sender, NowPlayingPalette value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public NowPlayingPalette Value { get; } = value;
}

