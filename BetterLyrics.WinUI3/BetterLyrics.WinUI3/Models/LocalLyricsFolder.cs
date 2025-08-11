// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LocalMediaFolder : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Path { get; set; }

        public LocalMediaFolder() { }

        public LocalMediaFolder(string path, bool isEnabled)
        {
            Path = path;
            IsEnabled = isEnabled;
        }
    }
}
