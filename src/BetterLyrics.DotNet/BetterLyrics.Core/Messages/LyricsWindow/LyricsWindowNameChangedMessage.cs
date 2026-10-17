using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowNameChangedMessage(LyricsWindowStatus sender, string value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public string Value { get; } = value;
}

