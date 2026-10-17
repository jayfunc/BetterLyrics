using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowLayoutProfileIdChangedMessage(LyricsWindowStatus sender, Guid value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public Guid Value { get; } = value;
}

