using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Extensions;

public static class LyricsSearchProviderExtensions
{
    extension(LyricsSearchProvider provider)
    {
        public LyricsFormat GetLyricsFormat()
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
                _ => LyricsFormat.NotSpecified
            };
        }

        public bool IsLocal()
        {
            return provider
                is LyricsSearchProvider.LocalMusicFile
                or LyricsSearchProvider.LocalLrcFile
                or LyricsSearchProvider.LocalEslrcFile
                or LyricsSearchProvider.LocalTtmlFile;
        }

        public bool IsCacheable()
        {
            return !provider.IsLocal();
        }

        public bool IsPlugin()
        {
            return (int)provider >= 1000;
        }

        public TranslationSearchProvider? ToTranslationSearchProvider()
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
                _ => null
            };
        }

        public TransliterationSearchProvider? ToTransliterationSearchProvider()
        {
            return provider switch
            {
                LyricsSearchProvider.LrcLib => TransliterationSearchProvider.LrcLib,
                LyricsSearchProvider.QQ => TransliterationSearchProvider.QQ,
                LyricsSearchProvider.Kugou => TransliterationSearchProvider.Kugou,
                LyricsSearchProvider.Netease => TransliterationSearchProvider.Netease,
                LyricsSearchProvider.AmllTtmlDb => TransliterationSearchProvider.AmllTtmlDb,
                LyricsSearchProvider.AppleMusic => TransliterationSearchProvider.AppleMusic,
                LyricsSearchProvider.LocalMusicFile => TransliterationSearchProvider.LocalMusicFile,
                LyricsSearchProvider.LocalLrcFile => TransliterationSearchProvider.LocalLrcFile,
                LyricsSearchProvider.LocalEslrcFile => TransliterationSearchProvider.LocalEslrcFile,
                LyricsSearchProvider.LocalTtmlFile => TransliterationSearchProvider.LocalTtmlFile,
                _ => null
            };
        }
    }
}