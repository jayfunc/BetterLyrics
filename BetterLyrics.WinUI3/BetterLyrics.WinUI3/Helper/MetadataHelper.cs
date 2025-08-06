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
        public static string AppVersion
        {
            get
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }
    }
}
