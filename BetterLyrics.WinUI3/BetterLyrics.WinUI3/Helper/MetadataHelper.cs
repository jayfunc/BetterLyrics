// 2025/6/23 by Zhe Fang

using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.Helper
{
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
