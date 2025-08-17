using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class DockModeSettings : BaseModeSettings
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DockPlacement DockPlacement { get; set; } = DockPlacement.Top;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int DockWindowHeight { get; set; } = 64;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string DockMonitorDeviceName { get; set; } = MonitorHelper.GetPrimaryMonitorDeviceName();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> ToggleShortcut { get; set; } = new List<string> { "Ctrl", "Shift", "D" };

        public DockModeSettings()
        {
            LyricsDisplayType = LyricsDisplayType.LyricsOnly;
        }
    }
}
