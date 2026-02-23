using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsCardConfig : ObservableObject
    {
        public string ResourceKey { get; set; } = "";
        [ObservableProperty] public partial string FontFamily { get; set; } = "Segoe UI";
    }


}
