// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AlbumArtSearchProviderInfo : ObservableRecipient
{
    public AlbumArtSearchProviderInfo()
    {
    }

    public AlbumArtSearchProviderInfo(AlbumArtSearchProvider provider, bool isEnabled)
    {
        Provider = provider;
        IsEnabled = isEnabled;
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AlbumArtSearchProvider Provider { get; set; }
}