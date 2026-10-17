using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.LyricsWindow;

public sealed class AlbumArtSongInfoAutoScrollChangedMessage(AlbumArtAreaEffectSettings sender, bool value)
{
    public AlbumArtAreaEffectSettings Sender { get; } = sender;
    public bool Value { get; } = value;
}

