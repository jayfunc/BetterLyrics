// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject
    {
        [ObservableProperty]
        public partial string Album { get; set; }

        [ObservableProperty]
        public partial string Artist { get; set; }

        [ObservableProperty]
        public partial int? Duration { get; set; }

        [ObservableProperty]
        public partial double? DurationMs { get; set; }

        [ObservableProperty]
        public partial string? PlayerId { get; set; } = null;

        [ObservableProperty]
        public partial string Title { get; set; }

        [ObservableProperty]
        public partial string? SongId { get; set; } = null;

        public SongInfo() { }
    }
}
