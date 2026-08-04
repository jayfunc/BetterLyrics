using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Extensions;

public static class LyricsProviderExtensions
{
    extension(LyricsProvider provider)
    {
        public LyricsFormat GetLyricsFormat()
        {
            return provider switch
            {
                LyricsProvider.LrcLib => LyricsFormat.Lrc,
                LyricsProvider.QQ => LyricsFormat.Qrc,
                LyricsProvider.Kugou => LyricsFormat.Krc,
                LyricsProvider.Netease => LyricsFormat.Lrc,
                LyricsProvider.AmllTtmlDb => LyricsFormat.Ttml,
                LyricsProvider.AppleMusic => LyricsFormat.Ttml,
                LyricsProvider.LocalLrcFile => LyricsFormat.Lrc,
                LyricsProvider.LocalEslrcFile => LyricsFormat.Eslrc,
                LyricsProvider.LocalTtmlFile => LyricsFormat.Ttml,
                _ => LyricsFormat.NotSpecified
            };
        }

        public bool IsLocal()
        {
            return provider
                is LyricsProvider.LocalMusicFile
                or LyricsProvider.LocalLrcFile
                or LyricsProvider.LocalEslrcFile
                or LyricsProvider.LocalTtmlFile;
        }

        public bool IsCacheable()
        {
            return !provider.IsLocal();
        }

        public bool IsPlugin()
        {
            return (int)provider >= 1000;
        }

        public bool IsInternal()
        {
            return provider is LyricsProvider.BetterLyrics or LyricsProvider.LibreTranslate;
        }
    }
}