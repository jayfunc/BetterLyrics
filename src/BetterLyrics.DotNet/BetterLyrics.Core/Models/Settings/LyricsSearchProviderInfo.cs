// 2025/6/23 by Zhe Fang

using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsSearchProviderInfo : ObservableRecipient
{
    public LyricsSearchProviderInfo()
    {
    }

    public LyricsSearchProviderInfo(LyricsProvider provider, bool isEnabled)
    {
        Provider = provider;
        IsEnabled = isEnabled;
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsProvider Provider { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsMatchingThresholdOverwritten { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int MatchingThreshold { get; set; } = 60;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IgnoreCacheWhenSearching { get; set; } = false;

    [JsonIgnore] public bool IsPlugin => Provider.IsPlugin();
}