using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class AlbumArtFadeOutDirectionChangedMessage(AlbumArtAreaEffectSettings sender)
{
    public AlbumArtAreaEffectSettings Sender { get; } = sender;
}

