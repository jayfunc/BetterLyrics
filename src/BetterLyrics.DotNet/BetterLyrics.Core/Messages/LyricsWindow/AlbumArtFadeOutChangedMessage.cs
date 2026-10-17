using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class AlbumArtFadeOutChangedMessage(AlbumArtAreaEffectSettings sender, bool value)
{
    public AlbumArtAreaEffectSettings Sender { get; } = sender;
    public bool Value { get; } = value;
}

