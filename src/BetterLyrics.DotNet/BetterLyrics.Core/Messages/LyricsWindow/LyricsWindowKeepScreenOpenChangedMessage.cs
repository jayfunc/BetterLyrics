using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowKeepScreenOpenChangedMessage(LyricsWindowStatus sender, bool value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public bool Value { get; } = value;
}

