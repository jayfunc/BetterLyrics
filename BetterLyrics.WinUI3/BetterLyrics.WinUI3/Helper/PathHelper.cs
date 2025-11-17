using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Helper
{
    public class PathHelper
    {
        public static string LocalFolder => ApplicationData.Current.LocalFolder.Path;
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        public static string SettingsDirectory => Path.Combine(LocalFolder, "settings");
        public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

        public static string LanguageProfilePath => Path.Combine(AssetsFolder, "Wiki82.profile.xml");

        public static string AlbumArtPlaceholderPath => Path.Combine(AssetsFolder, "AlbumArtPlaceholder.png");
        public static string LogoPath => Path.Combine(AssetsFolder, "Logo.ico");

        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string LyricsCacheDirectory => Path.Combine(CacheFolder, "lyrics");
        public static string LrcLibLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "lrclib");
        public static string NeteaseLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "netease");
        public static string QQLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "qq");
        public static string KugouLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "kugou");
        public static string AmllTtmlDbLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "amll-ttml-db");
        public static string AppleMusicCacheDirectory => Path.Combine(LyricsCacheDirectory, "apple-music");
        public static string AmllTtmlDbIndexPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-index.json");
        public static string AmllTtmlDbLastUpdatedPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-last-updated.txt");

        public static string TranslationCacheDirectory => Path.Combine(CacheFolder, "translations");
        public static string QQTranslationCacheDirectory => Path.Combine(TranslationCacheDirectory, "qq");
        public static string NeteaseTranslationCacheDirectory => Path.Combine(TranslationCacheDirectory, "netease");
        public static string KugouTranslationCacheDirectory => Path.Combine(TranslationCacheDirectory, "kugou");

        public static string AlbumArtCacheDirectory => Path.Combine(CacheFolder, "album-art");
        public static string iTunesAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "itunes");

        public static string PlayQueuePath => Path.Combine(CacheFolder, "play-queue.m3u");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(SettingsDirectory);

            Directory.CreateDirectory(LogDirectory);

            Directory.CreateDirectory(LrcLibLyricsCacheDirectory);
            Directory.CreateDirectory(QQLyricsCacheDirectory);
            Directory.CreateDirectory(KugouLyricsCacheDirectory);
            Directory.CreateDirectory(NeteaseLyricsCacheDirectory);
            Directory.CreateDirectory(AmllTtmlDbLyricsCacheDirectory);
            Directory.CreateDirectory(AppleMusicCacheDirectory);

            Directory.CreateDirectory(QQTranslationCacheDirectory);
            Directory.CreateDirectory(NeteaseTranslationCacheDirectory);
            Directory.CreateDirectory(KugouTranslationCacheDirectory);

            Directory.CreateDirectory(iTunesAlbumArtCacheDirectory);
        }
    }
}
