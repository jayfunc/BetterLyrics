// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Helpers;

public static class MetadataHelper
{
    private static readonly IPlatformProvider _platformProvider = Ioc.Default.GetRequiredService<IPlatformProvider>();

    public static string AppVersion => _platformProvider.AppVersion;
}