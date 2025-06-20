using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    using System;
    using System.IO;
    using Windows.ApplicationModel;
    using Windows.Storage;

    public static class AppInfo
    {
        // App Metadata
        public const string AppName = "BetterLyrics";
        public const string AppDisplayName = "Better Lyrics";
        public const string AppAuthor = "Zhe Fang";
        public const string GithubUrl = "https://github.com/jayfunc/BetterLyrics";
        public static string AppVersion
        {
            get
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        // Environment Info
        public static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // Base Folders
        private static string LocalFolder => ApplicationData.Current.LocalFolder.Path;
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        // Data Files

        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string OnlineLyricsCacheDirectory =>
            Path.Combine(CacheFolder, "online-lyrics");

        private static string TestMusicFileName => "AI - 甜度爆表.mp3";
        public static string TestMusicPath => Path.Combine(AssetsFolder, TestMusicFileName);

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(LocalFolder);
            Directory.CreateDirectory(LogDirectory);
            Directory.CreateDirectory(OnlineLyricsCacheDirectory);
        }
    }
}
