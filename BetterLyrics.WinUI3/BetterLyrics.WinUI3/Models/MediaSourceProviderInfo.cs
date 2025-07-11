// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MediaSourceProviderInfo : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        [ObservableProperty]
        public partial string Provider { get; set; }

        public MediaSourceProviderInfo() { }

        public MediaSourceProviderInfo(string provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }

    }
}
