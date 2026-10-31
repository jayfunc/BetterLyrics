using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowDisplayStateChangedMessage(LyricsWindowStatus sender, bool isMinimized)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public bool IsMinimized { get; } = isMinimized;
}
