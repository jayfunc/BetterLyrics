using Windows.ApplicationModel;
using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.WinUI.Helpers;

namespace BetterLyrics.WinUI3.Providers;

public class PlatformProvider : IPlatformProvider
{
    public string AppVersion => Package.Current.Id.Version.ToFormattedString();
}