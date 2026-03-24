using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class SystemTraySettings : ObservableObject
    {
        [ObservableProperty] public partial SystemTrayClickCallback SystemTrayClickCallback { get; set; } = SystemTrayClickCallback.LyricsWindowSwitchWindow;
        [ObservableProperty] public partial SystemTrayClickCallback SystemTrayDoubleClickCallback { get; set; } = SystemTrayClickCallback.None;
        [ObservableProperty] public partial SystemTrayClickCallback SystemTrayMiddleClickCallback { get; set; } = SystemTrayClickCallback.None;
    }
}
