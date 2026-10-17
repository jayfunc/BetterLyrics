// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AlbumArtSearchProviderInfo : ObservableObject
{
    public AlbumArtSearchProviderInfo()
    {
    }

    public AlbumArtSearchProviderInfo(AlbumArtProvider provider, bool isEnabled)
    {
        Provider = provider;
        IsEnabled = isEnabled;
    }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial AlbumArtProvider Provider { get; set; }
}