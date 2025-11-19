using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsSearchProviderExtensions
    {
        extension(LyricsSearchProvider provider)
        {
            public string GetCacheDirectory() => provider switch
            {
                LyricsSearchProvider.LrcLib => PathHelper.LrcLibLyricsCacheDirectory,
                LyricsSearchProvider.QQ => PathHelper.QQLyricsCacheDirectory,
                LyricsSearchProvider.Netease => PathHelper.NeteaseLyricsCacheDirectory,
                LyricsSearchProvider.Kugou => PathHelper.KugouLyricsCacheDirectory,
                LyricsSearchProvider.AmllTtmlDb => PathHelper.AmllTtmlDbLyricsCacheDirectory,
                LyricsSearchProvider.AppleMusic => PathHelper.AppleMusicCacheDirectory,
                _ => throw new ArgumentOutOfRangeException(nameof(provider)),
            };

            public LyricsFormat GetLyricsFormat() => provider switch
            {
                LyricsSearchProvider.LrcLib => LyricsFormat.Lrc,
                LyricsSearchProvider.QQ => LyricsFormat.Qrc,
                LyricsSearchProvider.Kugou => LyricsFormat.Krc,
                LyricsSearchProvider.Netease => LyricsFormat.Lrc,
                LyricsSearchProvider.AmllTtmlDb => LyricsFormat.Ttml,
                LyricsSearchProvider.AppleMusic => LyricsFormat.Ttml,
                LyricsSearchProvider.LocalLrcFile => LyricsFormat.Lrc,
                LyricsSearchProvider.LocalEslrcFile => LyricsFormat.Eslrc,
                LyricsSearchProvider.LocalTtmlFile => LyricsFormat.Ttml,
                _ => LyricsFormat.NotSpecified,
            };

            public bool IsLocal() => provider
                is LyricsSearchProvider.LocalMusicFile
                or LyricsSearchProvider.LocalLrcFile
                or LyricsSearchProvider.LocalEslrcFile
                or LyricsSearchProvider.LocalTtmlFile;

            public bool IsRemote() => !provider.IsLocal();

            public TranslationSearchProvider? ToTranslationSearchProvider() => provider switch
            {
                LyricsSearchProvider.LrcLib => TranslationSearchProvider.LrcLib,
                LyricsSearchProvider.QQ => TranslationSearchProvider.QQ,
                LyricsSearchProvider.Kugou => TranslationSearchProvider.Kugou,
                LyricsSearchProvider.Netease => TranslationSearchProvider.Netease,
                LyricsSearchProvider.AmllTtmlDb => TranslationSearchProvider.AmllTtmlDb,
                LyricsSearchProvider.AppleMusic => TranslationSearchProvider.AppleMusic,
                LyricsSearchProvider.LocalMusicFile => TranslationSearchProvider.LocalMusicFile,
                LyricsSearchProvider.LocalLrcFile => TranslationSearchProvider.LocalLrcFile,
                LyricsSearchProvider.LocalEslrcFile => TranslationSearchProvider.LocalEslrcFile,
                LyricsSearchProvider.LocalTtmlFile => TranslationSearchProvider.LocalTtmlFile,
                _ => null,
            };
        }
    }
}
