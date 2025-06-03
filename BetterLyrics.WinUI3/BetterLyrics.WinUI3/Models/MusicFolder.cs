using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models {
    public partial class MusicFolder : ObservableObject {
        [ObservableProperty]
        private string _path;

        public bool IsValid => Directory.Exists(Path);

        public MusicFolder(string path) {
            Path = path;
        }
    }
}
