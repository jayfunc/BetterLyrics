using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsCardSettings : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string SelectedStyleKey { get; set; } = "LyricsCardMinimalStyle";

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SelectedDisplayTypeIndex { get; set; } = 1;
}
