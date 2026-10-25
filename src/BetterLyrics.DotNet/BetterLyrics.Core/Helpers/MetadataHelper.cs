// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Helpers;

public static class MetadataHelper
{
    private static IPlatformProvider? PlatformProvider
    {
        get
        {
            try
            {
                return Ioc.Default.GetService<IPlatformProvider>();
            }
            catch
            {
                return null;
            }
        }
    }

    public static string AppVersion
    {
        get
        {
            try
            {
                return PlatformProvider?.AppVersion ?? "Unknown";
            }
            catch
            {
                // 未打包或平台信息尚不可用时退化为 Unknown，避免影响启动流程。
                return "Unknown";
            }
        }
    }
}