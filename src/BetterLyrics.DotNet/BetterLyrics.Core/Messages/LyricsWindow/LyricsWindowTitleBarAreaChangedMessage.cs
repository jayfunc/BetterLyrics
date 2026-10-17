using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowTitleBarAreaChangedMessage(LyricsWindowStatus sender, TitleBarArea value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public TitleBarArea Value { get; } = value;
}

