using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsEffectReloadChangedMessage(LyricsEffectSettings sender)
{
    public LyricsEffectSettings Sender { get; } = sender;
}

