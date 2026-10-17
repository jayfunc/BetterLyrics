using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowLockConfigChangedMessage(LyricsWindowStatus sender)
{
    public LyricsWindowStatus Sender { get; } = sender;
}

