using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Helper
{
    public class PathHelper
    {
        private static string LocalFolder => ApplicationData.Current.LocalFolder.Path;
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        public static string LanguageProfilePath => Path.Combine(AssetsFolder, "Core14.profile.xml");

        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string LrcLibLyricsCacheDirectory => Path.Combine(CacheFolder, "lrclib-lyrics");
        public static string NeteaseLyricsCacheDirectory => Path.Combine(CacheFolder, "netease-lyrics");
        public static string QQLyricsCacheDirectory => Path.Combine(CacheFolder, "qq-lyrics");
        public static string KugouLyricsCacheDirectory => Path.Combine(CacheFolder, "kugou-lyrics");
        public static string AmllTtmlDbLyricsCacheDirectory => Path.Combine(CacheFolder, "amll-ttml-db-lyrics");
        public static string AmllTtmlDbIndexPath => Path.Combine(CacheFolder, "amll-ttml-db-index.json");

        public static string iTunesAlbumArtCacheDirectory => Path.Combine(CacheFolder, "itunes-album-art");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(LocalFolder);
            Directory.CreateDirectory(LogDirectory);

            Directory.CreateDirectory(LrcLibLyricsCacheDirectory);
            Directory.CreateDirectory(QQLyricsCacheDirectory);
            Directory.CreateDirectory(KugouLyricsCacheDirectory);
            Directory.CreateDirectory(NeteaseLyricsCacheDirectory);
            Directory.CreateDirectory(AmllTtmlDbLyricsCacheDirectory);

            Directory.CreateDirectory(iTunesAlbumArtCacheDirectory);
        }
    }
}
