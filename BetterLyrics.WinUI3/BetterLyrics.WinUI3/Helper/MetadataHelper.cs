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
        public const string QQGroupUrl = "https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info";
        public const string DiscordUrl = "https://discord.gg/5yAQPnyCKv";

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
