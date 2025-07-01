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
    }

    public static class LyricsSearchProviderExtensions
    {
        public static string GetCacheDirectory(this LyricsSearchProvider provider)
        {
            return provider switch
            {
                LyricsSearchProvider.LrcLib => AppInfo.LrcLibLyricsCacheDirectory,
                LyricsSearchProvider.QQ => AppInfo.QQLyricsCacheDirectory,
                LyricsSearchProvider.Netease => AppInfo.NeteaseLyricsCacheDirectory,
                LyricsSearchProvider.Kugou => AppInfo.KugouLyricsCacheDirectory,
                LyricsSearchProvider.AmllTtmlDb => AppInfo.AmllTtmlDbLyricsCacheDirectory,
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
    }
}
