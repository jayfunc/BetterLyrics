// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsSearchProviderInfo : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider Provider { get; set; }

        public LyricsSearchProviderInfo() { }

        public LyricsSearchProviderInfo(LyricsSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }

    }
}
