using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models;

public partial class LyricsCardStyleItem : ObservableObject
{
    public string DisplayText { get; set; }
    public string StyleKey { get; set; }
    public LyricsCardData CardData => LyricsCardDataExtensions.DemoLyricsCardData;

    [ObservableProperty] public partial bool IsChecked { get; set; } = false;
    [ObservableProperty] public partial bool IsExpanded { get; set; } = true;
}