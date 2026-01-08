// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsSearchProviderInfo : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider Provider { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsMatchingThresholdOverwritten { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int MatchingThreshold { get; set; } = 40;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IgnoreCacheWhenSearching { get; set; } = false;

        public LyricsSearchProviderInfo() { }

        public LyricsSearchProviderInfo(LyricsSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }

    }
}
