// 2025/6/23 by Zhe Fang

using CommunityToolkit.WinUI.Helpers;
using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.Helper
{
    public static class MetadataHelper
    {
        public static string AppVersion => Package.Current.Id.Version.ToFormattedString();
    }
}
