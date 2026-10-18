using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BetterLyrics.Core.Models.Entities;

public partial class PlaybackState : ObservableObject
{
    public int Id { get; set; } = 1;
    [ObservableProperty] public partial int PlayQueueIndex { get; set; } = -1;
    [ObservableProperty] public partial TimeSpan PlaybackPosition { get; set; } = TimeSpan.Zero;
    [ObservableProperty] public partial ObservableCollection<string> PlayQueuePaths { get; set; } = [];
}
