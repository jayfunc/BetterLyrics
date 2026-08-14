using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;

namespace BetterLyrics.Core.Extensions;

public static class AlbumArtSearchProviderExtensions
{
    extension(AlbumArtProvider provider)
    {
        public bool IsLocal()
        {
            return provider
                is AlbumArtProvider.Local
                or AlbumArtProvider.SMTC;
        }

        public bool IsRemote()
        {
            return !provider.IsLocal();
        }

        public string GetCacheDirectory()
        {
            return provider switch
            {
                AlbumArtProvider.iTunes => PathHelper.iTunesAlbumArtCacheDirectory,
                AlbumArtProvider.Kugou => PathHelper.KugouAlbumArtCacheDirectory,
                AlbumArtProvider.LastFm => PathHelper.LastFmAlbumArtCacheDirectory,
                //AlbumArtSearchProvider.Netease => PathHelper.NeteaseAlbumArtCacheDirectory,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}