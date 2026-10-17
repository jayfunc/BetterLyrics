using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowStatusStateChangedMessage(LyricsWindowStatus sender, WindowStatus value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public WindowStatus Value { get; } = value;
}

