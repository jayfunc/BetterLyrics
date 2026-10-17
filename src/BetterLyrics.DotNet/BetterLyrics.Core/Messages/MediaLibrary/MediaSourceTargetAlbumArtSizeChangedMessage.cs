using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaSourceTargetAlbumArtSizeChangedMessage(
    MediaSourceProviderInfo provider,
    int targetAlbumArtSize)
{
    public MediaSourceProviderInfo Provider { get; } = provider;
    public int TargetAlbumArtSize { get; } = targetAlbumArtSize;
}

