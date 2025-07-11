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

    public static class MetadataHelper
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
    }
}
