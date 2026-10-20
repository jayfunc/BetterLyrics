using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowStateChangedMessage(LyricsWindowStatus sender, LyricsWindowState value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public LyricsWindowState Value { get; } = value;
}
