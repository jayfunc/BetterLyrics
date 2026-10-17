using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class SpectrumConfigChangedMessage(LyricsBackgroundSettings sender)
{
    public LyricsBackgroundSettings Sender { get; } = sender;
}

