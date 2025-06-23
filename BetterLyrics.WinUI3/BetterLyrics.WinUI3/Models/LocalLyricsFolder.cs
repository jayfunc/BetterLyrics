using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LocalLyricsFolder : ObservableObject
    {
        [ObservableProperty]
        public partial string Path { get; set; }

        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        public LocalLyricsFolder() { }

        public LocalLyricsFolder(string path, bool isEnabled)
        {
            Path = path;
            IsEnabled = isEnabled;
        }
    }
}
