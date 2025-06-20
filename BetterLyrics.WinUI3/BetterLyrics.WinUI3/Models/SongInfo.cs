using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject
    {
        [ObservableProperty]
        public partial string? Title { get; set; }

        [ObservableProperty]
        public partial string? Artist { get; set; }

        [ObservableProperty]
        public partial string? Album { get; set; }

        /// <summary>
        /// In milliseconds
        /// </summary>
        [ObservableProperty]
        public partial double? DurationMs { get; set; }

        [ObservableProperty]
        public partial string? SourceAppUserModelId { get; set; } = null;

        public byte[]? AlbumArt { get; set; } = null;

        public SongInfo() { }
    }
}
