using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class DesktopModeSettings : BaseModeSettings
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect WindowBounds { get; set; } = new Rect(100, 100, 400, 200);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoLockOnDesktopMode { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LockHotKeyIndex { get; set; } = 'U' - 'A'; // Default to 'U' key

        public DesktopModeSettings()
        {
            LyricsDisplayType = Enums.LyricsDisplayType.LyricsOnly;
        }
    }
}
