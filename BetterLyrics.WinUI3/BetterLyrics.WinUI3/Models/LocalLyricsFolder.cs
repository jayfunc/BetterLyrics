// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LocalLyricsFolder : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        [ObservableProperty]
        public partial string Path { get; set; }

        public LocalLyricsFolder() { }

        public LocalLyricsFolder(string path, bool isEnabled)
        {
            Path = path;
            IsEnabled = isEnabled;
        }
    }
}
