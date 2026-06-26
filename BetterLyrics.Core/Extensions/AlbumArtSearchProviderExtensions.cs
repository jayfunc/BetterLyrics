using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;

namespace BetterLyrics.Core.Extensions
{
    public static class AlbumArtSearchProviderExtensions
    {
        extension(AlbumArtSearchProvider provider)
        {
            public bool IsLocal() => provider
                is AlbumArtSearchProvider.Local
                or AlbumArtSearchProvider.SMTC;

            public bool IsRemote() => !IsLocal(provider);

            public string GetCacheDirectory() => provider switch
            {
                AlbumArtSearchProvider.iTunes => PathHelper.iTunesAlbumArtCacheDirectory,
                AlbumArtSearchProvider.Kugou => PathHelper.KugouAlbumArtCacheDirectory,
                //AlbumArtSearchProvider.Netease => PathHelper.NeteaseAlbumArtCacheDirectory,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
