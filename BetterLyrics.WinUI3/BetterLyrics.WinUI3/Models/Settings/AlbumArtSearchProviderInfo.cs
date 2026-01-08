// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtSearchProviderInfo : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtSearchProvider Provider { get; set; }

        public AlbumArtSearchProviderInfo() { }

        public AlbumArtSearchProviderInfo(AlbumArtSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }
    }
}
