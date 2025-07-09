// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class AlbumArtSearchProviderInfo : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        [ObservableProperty]
        public partial AlbumArtSearchProvider Provider { get; set; }

        public AlbumArtSearchProviderInfo() { }

        public AlbumArtSearchProviderInfo(AlbumArtSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }
    }
}
