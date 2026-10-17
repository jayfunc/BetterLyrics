using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsCardSettings : ObservableObject
{
    [ObservableProperty]
    public partial string SelectedStyleKey { get; set; } = "LyricsCardMinimalStyle";

    [ObservableProperty]
    public partial int SelectedDisplayTypeIndex { get; set; } = 1;
}
