using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.WinUI.Helpers;
using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class PlatformProvider : IPlatformProvider
{
    public string AppVersion => Package.Current.Id.Version.ToFormattedString();
}