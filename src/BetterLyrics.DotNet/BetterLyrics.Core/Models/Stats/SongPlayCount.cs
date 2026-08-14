using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Stats;

public partial class SongPlayCount : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial string Artist { get; set; } = string.Empty;
    [ObservableProperty] public partial int PlayCount { get; set; }
    [ObservableProperty] public partial string? AlbumArtUrl { get; set; }
}