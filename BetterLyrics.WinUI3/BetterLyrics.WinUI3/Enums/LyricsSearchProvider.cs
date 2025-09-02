// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;

namespace BetterLyrics.WinUI3.Enums
{
    public enum LyricsSearchProvider
    {
        QQ,
        Kugou,
        Netease,
        LrcLib,
        AmllTtmlDb,
        LocalMusicFile,
        LocalLrcFile,
        LocalEslrcFile,
        LocalTtmlFile,
        AppleMusic,
    }

    public static class LyricsSearchProviderExtensions
    {
        public static string GetCacheDirectory(this LyricsSearchProvider provider)
        {
            return provider switch
            {
                LyricsSearchProvider.LrcLib => PathHelper.LrcLibLyricsCacheDirectory,
                LyricsSearchProvider.QQ => PathHelper.QQLyricsCacheDirectory,
                LyricsSearchProvider.Netease => PathHelper.NeteaseLyricsCacheDirectory,
                LyricsSearchProvider.Kugou => PathHelper.KugouLyricsCacheDirectory,
                LyricsSearchProvider.AmllTtmlDb => PathHelper.AmllTtmlDbLyricsCacheDirectory,
                LyricsSearchProvider.AppleMusic => PathHelper.AppleMusicCacheDirectory,
                _ => throw new System.ArgumentOutOfRangeException(nameof(provider)),
            };
        }

        public static LyricsFormat GetLyricsFormat(this LyricsSearchProvider provider)
        {
            return provider switch
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
        }

        public static bool IsLocal(this LyricsSearchProvider provider)
        {
            return provider
                is LyricsSearchProvider.LocalMusicFile
                    or LyricsSearchProvider.LocalLrcFile
                    or LyricsSearchProvider.LocalEslrcFile
                    or LyricsSearchProvider.LocalTtmlFile;
        }

        public static bool IsRemote(this LyricsSearchProvider provider)
        {
            return !provider.IsLocal();
        }

        public static TranslationSearchProvider? ToTranslationSearchProvider(this LyricsSearchProvider? provider)
        {
            return provider switch
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
