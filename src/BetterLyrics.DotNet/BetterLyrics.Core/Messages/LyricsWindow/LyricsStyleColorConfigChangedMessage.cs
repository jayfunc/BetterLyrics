using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsStyleColorConfigChangedMessage(LyricsStyleSettings sender)
{
    public LyricsStyleSettings Sender { get; } = sender;
}

