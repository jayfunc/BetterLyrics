using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsCardConfig : ObservableObject
{
    public string ResourceKey { get; set; } = "";
    [ObservableProperty] public partial string FontFamily { get; set; } = "Segoe UI";
}