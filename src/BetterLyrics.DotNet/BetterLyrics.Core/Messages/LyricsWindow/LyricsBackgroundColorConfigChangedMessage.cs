using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsBackgroundColorConfigChangedMessage(LyricsBackgroundSettings sender)
{
    public LyricsBackgroundSettings Sender { get; } = sender;
}

