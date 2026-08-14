// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AlbumArtSearchProviderInfo : ObservableRecipient
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
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AlbumArtProvider Provider { get; set; }
}