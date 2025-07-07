// 2025/6/23 by Zhe Fang

namespace BetterLyrics.WinUI3.Helper
{
    using BetterLyrics.WinUI3.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.Storage.FileProperties;

    public static class AppInfo
    {
        public const string AppAuthor = "Zhe Fang";
        public const string AppDisplayName = "Better Lyrics";
        public const string AppName = "BetterLyrics";
        public static string AppVersion
        {
            get
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        public const string GithubUrl = "https://github.com/jayfunc/BetterLyrics";

        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;


        public const string UnlockWindowTag = "UnlockWindow";

        public static string AmllTtmlDbIndexPath => Path.Combine(CacheFolder, "amll-ttml-db-index.json");

        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");
        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string LrcLibLyricsCacheDirectory => Path.Combine(CacheFolder, "lrclib-lyrics");
        public static string NeteaseLyricsCacheDirectory => Path.Combine(CacheFolder, "netease-lyrics");
        public static string QQLyricsCacheDirectory => Path.Combine(CacheFolder, "qq-lyrics");
        public static string KugouLyricsCacheDirectory => Path.Combine(CacheFolder, "kugou-lyrics");
        public static string AmllTtmlDbLyricsCacheDirectory => Path.Combine(CacheFolder, "amll-ttml-db-lyrics");

        public static string iTunesAlbumArtCacheDirectory => Path.Combine(CacheFolder, "itunes-album-art");


        private static string LocalFolder => ApplicationData.Current.LocalFolder.Path;

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

        public static async Task<DateTime> GetBuildDate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var filePath = assembly.Location;
            if (!File.Exists(filePath))
                return DateTime.MinValue;

            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            // 获取文件基本属性
            BasicProperties props = await file.GetBasicPropertiesAsync();
            // 返回修改日期
            return props.DateModified.DateTime;
        }

        public static List<LanguageInfo> GetAllTranslationLanguagesInfo() =>
        [
            new LanguageInfo("ar", "العربية"),
            new LanguageInfo("az", "Azərbaycan dili"),
            new LanguageInfo("zh", "中文"),
            new LanguageInfo("cs", "Čeština"),
            new LanguageInfo("da", "Dansk"),
            new LanguageInfo("nl", "Nederlands"),
            new LanguageInfo("en", "English"),
            new LanguageInfo("eo", "Esperanto"),
            new LanguageInfo("fi", "Suomi"),
            new LanguageInfo("fr", "Français"),
            new LanguageInfo("de", "Deutsch"),
            new LanguageInfo("el", "Ελληνικά"),
            new LanguageInfo("he", "עברית"),
            new LanguageInfo("hi", "हिन्दी"),
            new LanguageInfo("hu", "Magyar"),
            new LanguageInfo("id", "Bahasa Indonesia"),
            new LanguageInfo("ga", "Gaeilge"),
            new LanguageInfo("it", "Italiano"),
            new LanguageInfo("ja", "日本語"),
            new LanguageInfo("ko", "한국어"),
            new LanguageInfo("fa", "فارسی"),
            new LanguageInfo("pl", "Polski"),
            new LanguageInfo("pt", "Português"),
            new LanguageInfo("ru", "Русский"),
            new LanguageInfo("sk", "Slovenčina"),
            new LanguageInfo("es", "Español"),
            new LanguageInfo("sv", "Svenska"),
            new LanguageInfo("tr", "Türkçe"),
            new LanguageInfo("uk", "Українська"),
            new LanguageInfo("vi", "Tiếng Việt"),
        ];
    }
}
