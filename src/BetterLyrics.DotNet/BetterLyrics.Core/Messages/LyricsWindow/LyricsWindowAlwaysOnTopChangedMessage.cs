using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowAlwaysOnTopChangedMessage(LyricsWindowStatus sender, bool value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public bool Value { get; } = value;
}

