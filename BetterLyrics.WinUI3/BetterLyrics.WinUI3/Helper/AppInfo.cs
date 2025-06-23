// 2025/6/23 by Zhe Fang

namespace BetterLyrics.WinUI3.Helper
{
    using System.IO;
    using Windows.ApplicationModel;
    using Windows.Storage;

    /// <summary>
    /// Defines the <see cref="AppInfo" />
    /// </summary>
    public static class AppInfo
    {
        #region Constants

        /// <summary>
        /// Defines the AppAuthor
        /// </summary>
        public const string AppAuthor = "Zhe Fang";

        /// <summary>
        /// Defines the AppDisplayName
        /// </summary>
        public const string AppDisplayName = "Better Lyrics";

        // App Metadata

        /// <summary>
        /// Defines the AppName
        /// </summary>
        public const string AppName = "BetterLyrics";

        /// <summary>
        /// Defines the GithubUrl
        /// </summary>
        public const string GithubUrl = "https://github.com/jayfunc/BetterLyrics";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the AppVersion
        /// </summary>
        public static string AppVersion
        {
            get
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        /// <summary>
        /// Gets the AssetsFolder
        /// </summary>
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        /// <summary>
        /// Gets the CacheFolder
        /// </summary>
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;

        // Environment Info

        /// <summary>
        /// Gets a value indicating whether IsDebug
        /// </summary>
        public static bool IsDebug =>
#if DEBUG
            true;

        // Data Files

        /// <summary>
        /// Gets the LogDirectory
        /// </summary>
        public static string LogDirectory => Path.Combine(CacheFolder, "logs");

        /// <summary>
        /// Gets the LogFilePattern
        /// </summary>
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        /// <summary>
        /// Gets the OnlineLyricsCacheDirectory
        /// </summary>
        public static string OnlineLyricsCacheDirectory =>
            Path.Combine(CacheFolder, "online-lyrics");

        /// <summary>
        /// Gets the TestMusicPath
        /// </summary>
        public static string TestMusicPath => Path.Combine(AssetsFolder, TestMusicFileName);

#else
            false;
#endif

        // Base Folders

        /// <summary>
        /// Gets the LocalFolder
        /// </summary>
        private static string LocalFolder => ApplicationData.Current.LocalFolder.Path;

        /// <summary>
        /// Gets the TestMusicFileName
        /// </summary>
        private static string TestMusicFileName => "AI - 甜度爆表.mp3";

        #endregion

        #region Methods

        /// <summary>
        /// The EnsureDirectories
        /// </summary>
        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(LocalFolder);
            Directory.CreateDirectory(LogDirectory);
            Directory.CreateDirectory(OnlineLyricsCacheDirectory);
        }

        #endregion
    }
}
