using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class LyricsWindowWallpaperEngineBindingChangedMessage(LyricsWindowStatus sender)
{
    public LyricsWindowStatus Sender { get; } = sender;
}
