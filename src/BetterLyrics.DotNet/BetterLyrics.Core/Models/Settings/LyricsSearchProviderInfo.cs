// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsSearchProviderInfo : ObservableObject
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
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial LyricsProvider Provider { get; set; }

    [ObservableProperty]
    public partial bool IsMatchingThresholdOverwritten { get; set; } = false;

    [ObservableProperty]
    public partial int MatchingThreshold { get; set; } = 60;

    [ObservableProperty]
    public partial bool IgnoreCacheWhenSearching { get; set; } = false;

    [JsonIgnore] public bool IsPlugin => Provider.IsPlugin();
}