// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;

namespace BetterLyrics.WinUI3.Enums
{
    #region Enums

    /// <summary>
    /// Defines the LyricsSearchProvider
    /// </summary>
    public enum LyricsSearchProvider
    {
        /// <summary>
        /// Defines the LrcLib
        /// </summary>
        LrcLib,
        QQ,
        Netease,
        Kugou,

        AmllTtmlDb,

        /// <summary>
        /// Defines the LocalMusicFile
        /// </summary>
        LocalMusicFile,

        /// <summary>
        /// Defines the LocalLrcFile
        /// </summary>
        LocalLrcFile,

        /// <summary>
        /// Defines the LocalEslrcFile
        /// </summary>
        LocalEslrcFile,

        /// <summary>
        /// Defines the LocalTtmlFile
        /// </summary>
        LocalTtmlFile,
    }

    public static class LyricsSearchProviderExtensions
    {
        /// <summary>
        /// The IsLocal
        /// </summary>
        /// <param name="provider">The provider<see cref="LyricsSearchProvider"/></param>
        /// <returns>The <see cref="bool"/></returns>
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
    }

    #endregion
}
