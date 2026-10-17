using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowTaskbarPlacementChangedMessage(LyricsWindowStatus sender, TaskbarPlacement value)
{
    public LyricsWindowStatus Sender { get; } = sender;
    public TaskbarPlacement Value { get; } = value;
}

