// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject
    {
        [ObservableProperty]
        public partial string? Album { get; set; }

        public SoftwareBitmap? AlbumArtSwBitmap { get; set; } = null;

        public Color? AlbumArtAccentColor { get; set; } = null;

        [ObservableProperty]
        public partial string Artist { get; set; }

        [ObservableProperty]
        public partial double? DurationMs { get; set; }

        [ObservableProperty]
        public partial string? SourceAppUserModelId { get; set; } = null;

        [ObservableProperty]
        public partial string Title { get; set; }

        public SongInfo() { }
    }
}
